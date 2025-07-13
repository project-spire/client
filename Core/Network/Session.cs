using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Spire.Protocol;

namespace Spire.Core.Network;

public class IngressMessage(ProtocolCategory category, byte[] data)
{
    public ProtocolCategory Category { get; } = category;
    public byte[] Data { get; } = data;
    
    ~IngressMessage()
    {
        ArrayPool<byte>.Shared.Return(Data);
    }
}

public class EgressMessage(byte[] data, bool pooled)
{
    public byte[] Data { get; } = data;
    public bool Pooled { get; } = pooled;

    ~EgressMessage()
    {
        if (pooled)
            ArrayPool<byte>.Shared.Return(Data);
    }
}

public sealed class Session : IDisposable
{
    private readonly Socket _socket;
    private readonly byte[] _headerBuffer = new byte[ProtocolHeader.Size];
    private readonly Channel<EgressMessage> _egressMessages = Channel.CreateUnbounded<EgressMessage>();
    private readonly CancellationTokenSource _cancellation = new();
    private readonly MessageHandler _handler;
    private readonly ILogger _logger;
    
    public bool IsRunning => !_cancellation.IsCancellationRequested;
    
    public Session(Socket socket, MessageHandler handler, ILogger logger)
    {
        _socket = socket;
        _socket.NoDelay = true;
        
        _handler = handler;
        _logger = logger;
    }

    public void Start()
    {
        Task.Run(StartReceive, _cancellation.Token);
        Task.Run(StartSend, _cancellation.Token);
    }

    public void Stop()
    {
        if (_cancellation.IsCancellationRequested) return;

        try
        {
            _cancellation.Cancel();
            _socket.Shutdown(SocketShutdown.Both);

            _egressMessages.Writer.TryComplete();

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
        {
            try
            {
                var (category, data) = await Receive();
                _handler.Handle(new IngressMessage(category, data));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error receiving message");
                Stop();
                break;
            }
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
        await foreach (var message in _egressMessages.Reader.ReadAllAsync(_cancellation.Token))
        {
            try
            {
                await _socket.SendAsync(message.Data, SocketFlags.None, _cancellation.Token);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error sending message");
                Stop();
                break;
            }
        }
    }

    public void Send(EgressMessage message)
    {
        if (!IsRunning) return;
        _egressMessages.Writer.TryWrite(message);
    }

    public async ValueTask SendAsync(EgressMessage message)
    {
        if (!IsRunning) return;
        await _egressMessages.Writer.WriteAsync(message, _cancellation.Token);
    }

    public void Dispose()
    {
        _cancellation.Dispose();
        _socket.Dispose();
    }
}
