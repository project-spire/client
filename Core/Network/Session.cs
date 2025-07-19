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
    public readonly byte[] Data = data;

    ~EgressProtocol()
    {
        if (pooled) ArrayPool<byte>.Shared.Return(Data);
    }

    public static EgressProtocol New(ProtocolCategory category, IMessage protocol, bool pooled = true)
    {
        var size = protocol.CalculateSize() + ProtocolHeader.Size;
        var buffer = pooled ? ArrayPool<byte>.Shared.Rent(size) : new byte[size];
        ProtocolHeader.Write(category, size - ProtocolHeader.Size, buffer.AsSpan()[..ProtocolHeader.Size]);
        protocol.WriteTo(buffer.AsSpan()[ProtocolHeader.Size..size]);

        return new EgressProtocol(buffer, pooled);
    }

    public static EgressProtocol Auth(IMessage protocol, bool pooled = true)
    {
        return New(ProtocolCategory.Auth, protocol, pooled);
    }
    
    public static EgressProtocol Game(IMessage protocol, bool pooled = true)
    {
        return New(ProtocolCategory.Game, protocol, pooled);
    }
    
    public static EgressProtocol Net(IMessage protocol, bool pooled = true)
    {
        return New(ProtocolCategory.Net, protocol, pooled);
    }
}

public sealed class Session : IDisposable
{
    private readonly TcpClient _client = new();
    private readonly byte[] _headerBuffer = new byte[ProtocolHeader.Size];
    private readonly Channel<EgressProtocol> _egressProtocols = Channel.CreateUnbounded<EgressProtocol>();
    private readonly Func<Session, ISessionContext> _ctxFactory;

    private readonly CancellationTokenSource _cancellation = new();
    private readonly ILogger _logger;
    
    private Socket Socket => _client.Client;
    private bool IsRunning => !_cancellation.IsCancellationRequested;
    
    public Task CompletionTask { get; private set; } = Task.CompletedTask;
    
    public Session(Func<Session, ISessionContext> ctxFactory, ILogger logger)
    {
        _ctxFactory = ctxFactory;
        _logger = logger;
        
        Socket.NoDelay = true;
    }

    public void Connect(string host, ushort port)
    {
        var address = Dns.GetHostAddresses(host)[0];
        _client.Connect(address, port);
    }

    public async ValueTask ConnectAsync(string host, ushort port)
    {
        var address = (await Dns.GetHostAddressesAsync(host))[0];
        await _client.ConnectAsync(address, port);
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
        
        CompletionTask = Task.WhenAll(
            Task.Run(StartReceive, _cancellation.Token),
            Task.Run(StartSend, _cancellation.Token));
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
        {
            try
            {
                var (category, data) = await Receive();
                ProtocolDispatcher.Dispatch(_ctxFactory(this), new IngressProtocol(category, data));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error receiving: {message}", e.Message);
                Stop();
            }
        }
    }

    private async ValueTask<(ProtocolCategory, byte[])> Receive()
    {
        var n = await Socket.ReceiveAsync(_headerBuffer, SocketFlags.None, _cancellation.Token);
        if (n == 0) throw new IOException("End of file received");

        var (category, length) = ProtocolHeader.Read(_headerBuffer);
        if (length == 0) return (ProtocolCategory.None, []);

        var bodyBuffer = ArrayPool<byte>.Shared.Rent(length);
        n = await Socket.ReceiveAsync(bodyBuffer, SocketFlags.None, _cancellation.Token);
        if (n == 0) throw new IOException("End of file received");

        return (category, bodyBuffer);
    }

    private async Task StartSend()
    {
        while (IsRunning)
        {
            try
            {
                await foreach (var protocol in _egressProtocols.Reader.ReadAllAsync(_cancellation.Token))
                {
                    await Socket.SendAsync(protocol.Data, SocketFlags.None, _cancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending: {message}", e.Message);
                Stop();
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

        await _egressProtocols.Writer.WriteAsync(protocol, _cancellation.Token);
    }
}