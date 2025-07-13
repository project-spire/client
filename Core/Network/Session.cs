using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Spire.Protocol;

namespace Spire.Core.Network;

public class IngressProtocol(ProtocolCategory category, byte[] data)
{
    public ProtocolCategory Category { get; } = category;
    public byte[] Data { get; } = data;

    ~IngressProtocol()
    {
        ArrayPool<byte>.Shared.Return(Data);
    }
}

public class EgressProtocol(byte[] data, bool pooled)
{
    public byte[] Data { get; } = data;

    ~EgressProtocol()
    {
        if (pooled)
            ArrayPool<byte>.Shared.Return(Data);
    }

    public static EgressProtocol New(ProtocolCategory category, IMessage protocol, bool pooled = true)
    {
        var size = protocol.CalculateSize() + ProtocolHeader.Size;
        var buffer = pooled ? ArrayPool<byte>.Shared.Rent(size) : new byte[size];
        ProtocolHeader.Write(category, (ushort)size, buffer.AsSpan()[..ProtocolHeader.Size]);
        protocol.WriteTo(buffer.AsSpan()[ProtocolHeader.Size..]);

        return new EgressProtocol(buffer, pooled);
    }
}

public sealed class Session : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Func<Session, ISessionContext> _ctxFactory;
    private readonly ProtocolDispatcher _dispatcher;
    private readonly Channel<EgressProtocol> _egressProtocols = Channel.CreateUnbounded<EgressProtocol>();
    private readonly byte[] _headerBuffer = new byte[ProtocolHeader.Size];
    private readonly ILogger _logger;
    private readonly Socket _socket;

    public Session(
        Socket socket,
        ProtocolDispatcher dispatcher,
        Func<Session, ISessionContext> ctxFactory,
        ILogger logger)
    {
        _socket = socket;
        _socket.NoDelay = true;
        _ctxFactory = ctxFactory;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public bool IsRunning => !_cancellation.IsCancellationRequested;

    public void Dispose()
    {
        _cancellation.Dispose();
        _socket.Dispose();
    }

    public void Start()
    {
        Task.Run(StartReceive, _cancellation.Token);
        Task.Run(StartSend, _cancellation.Token);
    }

    public void Stop()
    {
        if (!IsRunning) return;

        try
        {
            _cancellation.Cancel();
            _socket.Shutdown(SocketShutdown.Both);

            _egressProtocols.Writer.TryComplete();

            _socket.Close();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error closing socket");
        }
    }

    private async Task StartReceive()
    {
        while (IsRunning)
            try
            {
                var (category, data) = await Receive();
                _dispatcher.Dispatch(_ctxFactory(this), new IngressProtocol(category, data));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error receiving message");
                Stop();
                break;
            }
    }

    private async ValueTask<(ProtocolCategory, byte[])> Receive()
    {
        await _socket.ReceiveAsync(_headerBuffer, SocketFlags.None, _cancellation.Token);

        var (category, length) = ProtocolHeader.Read(_headerBuffer);
        if (length == 0) return (category, []);

        var bodyBuffer = ArrayPool<byte>.Shared.Rent(length);
        await _socket.ReceiveAsync(bodyBuffer, SocketFlags.None, _cancellation.Token);

        return (category, bodyBuffer);
    }

    private async Task StartSend()
    {
        await foreach (var protocol in _egressProtocols.Reader.ReadAllAsync(_cancellation.Token))
            try
            {
                await _socket.SendAsync(protocol.Data, SocketFlags.None, _cancellation.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending message");
                Stop();
                break;
            }
    }

    public void Send(EgressProtocol protocol)
    {
        if (!IsRunning) return;
        _egressProtocols.Writer.TryWrite(protocol);
    }

    public async ValueTask SendAsync(EgressProtocol protocol)
    {
        if (!IsRunning) return;
        await _egressProtocols.Writer.WriteAsync(protocol, _cancellation.Token);
    }
}