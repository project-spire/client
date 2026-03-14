using System.Buffers;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Spire.Message;
using Spire.Message.Game;
using Spire.Message.Game.Net;

namespace Spire.Core.Network;

public class EgressMessage(byte[] data, int length, bool pooled)
{
    public ReadOnlyMemory<byte> Data => data.AsMemory(0, length);

    ~EgressMessage()
    {
        if (pooled) ArrayPool<byte>.Shared.Return(data);
    }

    public static EgressMessage New(IMessage message, bool pooled = true)
    {
        var length = MessageHeader.Size + message.Size;
        var buffer = pooled ? ArrayPool<byte>.Shared.Rent(length) : new byte[length];
        MessageHeader header = new(length - MessageHeader.Size, message.MessageId);

        header.Encode(buffer.AsSpan()[..MessageHeader.Size]);
        message.Encode(buffer.AsSpan()[MessageHeader.Size..length]);

        return new EgressMessage(buffer, length, pooled);
    }
}

public sealed class Session(MessageDispatcher dispatcher, ILogger logger) : IAsyncDisposable, IDisposable
{
    private const string ApplicationProtocol = "spire";

    private readonly byte[] _headerBuffer = new byte[MessageHeader.Size];
    private readonly Channel<EgressMessage> _egressMessages = Channel.CreateUnbounded<EgressMessage>();

    private readonly CancellationTokenSource _cancellation = new();

    private QuicConnection? _connection;
    private QuicStream? _stream;

    private bool IsRunning => !_cancellation.IsCancellationRequested;

    public Task CompletionTask { get; private set; } = Task.CompletedTask;

    public async ValueTask ConnectAsync(string host, ushort port)
    {
        if (!QuicConnection.IsSupported)
            throw new NotSupportedException("QUIC is not supported");

        var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        if (!IPAddress.TryParse(host, out _))
        {
            var addresses = await Dns.GetHostAddressesAsync(host);
            endpoint = new IPEndPoint(addresses[0], port);
        }

        var connectionOptions = new QuicClientConnectionOptions
        {
            DefaultStreamErrorCode = 0,
            DefaultCloseErrorCode = 0,
            RemoteEndPoint = endpoint,
            ClientAuthenticationOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = [new SslApplicationProtocol(ApplicationProtocol)],
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            },

            KeepAliveInterval = TimeSpan.FromSeconds(10)
        };

        _connection = await QuicConnection.ConnectAsync(connectionOptions, _cancellation.Token);
        logger.LogInformation("Connected to {host}:{port}", host, port);
    }

    public async ValueTask LoginAsync(LoginMessage login)
    {
        logger.LogInformation("Logging in with: {CharacterId}, Token: {Token}", login.Value.CharacterId, login.Value.Token);

        if (!QuicConnection.IsSupported)
            throw new NotSupportedException("QUIC is not supported");

        var message = EgressMessage.New(login);

        _stream = await _connection!.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, _cancellation.Token);

        await _stream.WriteAsync(message.Data, _cancellation.Token);
        await _stream.FlushAsync(_cancellation.Token);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _cancellation.CancelAsync();

            if (QuicConnection.IsSupported)
            {
                if (_stream != null)
                    await _stream.DisposeAsync();

                if (_connection != null)
                    await _connection.DisposeAsync();
            }

            _cancellation.Dispose();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during disposal");
        }
    }

    public async ValueTask StartAsync()
    {
        if (!QuicConnection.IsSupported)
            throw new NotSupportedException("QUIC is not supported");

        // _stream = await _connection!.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, _cancellation.Token);
        var ping = new PingMessage(new Ping
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        await SendAsync(EgressMessage.New(ping));

        CompletionTask = Task.WhenAll(
            Task.Run(StartReceive, _cancellation.Token),
            Task.Run(StartSend, _cancellation.Token));
    }

    public async ValueTask StopAsync()
    {
        if (!IsRunning) return;

        try
        {
            await _cancellation.CancelAsync();
            _egressMessages.Writer.TryComplete();

            if (_stream != null)
                await _stream.FlushAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error stopping session");
        }
    }

    public void Stop()
    {
        StopAsync().AsTask().Wait();
    }

    private async Task StartReceive()
    {
        while (IsRunning)
        {
            try
            {
                var message = await Receive();
                dispatcher.Dispatch(message);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to receive: {message}", e.Message);
                await StopAsync();
                break;
            }
        }
    }

    private async ValueTask<IMessage> Receive()
    {
        if (_stream == null)
            throw new InvalidOperationException("Stream is not available");

        await _stream.ReadExactlyAsync(_headerBuffer, _cancellation.Token);

        var header = MessageHeader.Decode(_headerBuffer);

        var bodyBuffer = ArrayPool<byte>.Shared.Rent(header.Length);
        await _stream.ReadExactlyAsync(bodyBuffer.AsMemory()[..header.Length], _cancellation.Token);

        var message = IMessage.Decode(header.Id, bodyBuffer);
        ArrayPool<byte>.Shared.Return(bodyBuffer);
        return message;
    }

    private async Task StartSend()
    {
        if (!QuicConnection.IsSupported)
            throw new NotSupportedException("QUIC is not supported");

        if (_stream == null)
            throw new InvalidOperationException("Stream is not available");

        while (IsRunning)
        {
            try
            {
                await foreach (var message in _egressMessages.Reader.ReadAllAsync(_cancellation.Token))
                {
                    await _stream.WriteAsync(message.Data, _cancellation.Token);
                    await _stream.FlushAsync(_cancellation.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to send: {message}", e.Message);
                await StopAsync();
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
}