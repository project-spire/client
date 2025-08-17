using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Spire.Core.Protocol;
using Spire.Protocol;

namespace Spire.Core.Network;

public class EgressProtocol(byte[] data, int length, bool pooled)
{
    public ReadOnlyMemory<byte> Data => data.AsMemory(0, length);

    ~EgressProtocol()
    {
        if (pooled) ArrayPool<byte>.Shared.Return(data);
    }

    private static EgressProtocol New(IProtocol protocol, bool pooled = true)
    {
        var length = protocol.Size + ProtocolHeader.Size;
        var buffer = pooled ? ArrayPool<byte>.Shared.Rent(length) : new byte[length];
        ProtocolHeader header = new(length, protocol.ProtocolId);
        
        header.Encode(buffer.AsSpan()[..ProtocolHeader.Size]);
        protocol.Encode(buffer.AsSpan()[ProtocolHeader.Size..length]);

        return new EgressProtocol(buffer, length, pooled);
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
                var protocol = await Receive();
                ProtocolDispatcher.Dispatch(_ctxFactory(this), protocol);
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

    private async ValueTask<IProtocol> Receive()
    {
        var n = await Socket.ReceiveAsync(_headerBuffer, SocketFlags.None, _cancellation.Token);
        if (n == 0) throw new IOException("End of file received");

        var header = ProtocolHeader.Decode(_headerBuffer);

        var bodyBuffer = ArrayPool<byte>.Shared.Rent(header.Length);
        n = await Socket.ReceiveAsync(bodyBuffer, SocketFlags.None, _cancellation.Token);
        if (n == 0) throw new IOException("End of file received");
        
        var protocol = IProtocol.Decode(header.Id, bodyBuffer);
        return protocol;
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