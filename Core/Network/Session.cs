using System.Buffers;
using System.Net;
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
    private readonly TcpClient _client = new ();
    private readonly byte[] _headerBuffer = new byte[ProtocolHeader.Size];
    private readonly Func<Session, ISessionContext> _ctxFactory;
    private readonly Channel<EgressProtocol> _egressProtocols = Channel.CreateUnbounded<EgressProtocol>();
    
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ILogger _logger;

    private Socket Socket => _client.Client;
    public bool IsRunning => !_cancellation.IsCancellationRequested;
    
    public Session(Func<Session, ISessionContext> ctxFactory, ILogger logger)
    {
        _ctxFactory = ctxFactory;
        _logger = logger;
        
        Socket.NoDelay = true;
    }

    public bool Connect(string host, ushort port)
    {
        try
        {
            var address = Dns.GetHostAddresses(host)[0];
            _client.Connect(address, port);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error connecting to {Host}:{Port}", host, port);
            return false;
        }
    }

    public async ValueTask<bool> ConnectAsync(string host, ushort port)
    {
        try
        {
            var address = (await Dns.GetHostAddressesAsync(host))[0];
            await _client.ConnectAsync(address, port);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error connecting to {Host}:{Port}", host, port);
            return false;
        }
    }

    public void Dispose()
    {
        _cancellation.Dispose();
        _client.Dispose();
    }

    public void Start()
    {
        if (!_client.Connected)
        {
            _logger.LogWarning("Session is not connected yet");
            return;
        }
        
        Task.Run(StartReceive, _cancellation.Token);
        Task.Run(StartSend, _cancellation.Token);
    }

    public void Stop()
    {
        if (!IsRunning) return;

        try
        {
            _cancellation.Cancel();
            _egressProtocols.Writer.TryComplete();
            _client.Close();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error stopping session");
        }
    }

    private async Task StartReceive()
    {
        while (IsRunning)
            try
            {
                var (category, data) = await Receive();
                ProtocolDispatcher.Dispatch(_ctxFactory(this), new IngressProtocol(category, data));
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
        await Socket.ReceiveAsync(_headerBuffer, SocketFlags.None, _cancellation.Token);

        var (category, length) = ProtocolHeader.Read(_headerBuffer);
        if (length == 0) return (category, []);

        var bodyBuffer = ArrayPool<byte>.Shared.Rent(length);
        await Socket.ReceiveAsync(bodyBuffer, SocketFlags.None, _cancellation.Token);

        return (category, bodyBuffer);
    }

    private async Task StartSend()
    {
        await foreach (var protocol in _egressProtocols.Reader.ReadAllAsync(_cancellation.Token))
        {
            try
            {
                await Socket.SendAsync(protocol.Data, SocketFlags.None, _cancellation.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending message");
                Stop();
                break;
            }
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

        try
        {
            await _egressProtocols.Writer.WriteAsync(protocol, _cancellation.Token);
        }
        catch (OperationCanceledException) { }
    }
}