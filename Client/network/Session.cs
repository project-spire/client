using System.Buffers;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using spire.protocol;
using spire.protocol.game;
using spire.protocol.net;

namespace spire.network;

public sealed class Session(NetworkStream stream)
{
    public readonly Channel<byte[]> Sender = Channel.CreateUnbounded<byte[]>();

    private readonly ArrayPool<byte> _receiveBufferPool = ArrayPool<byte>.Create();

    public void Run()
    {
        _ = Receive();
        _ = Send();
    }

    private async Task Receive()
    {
        var headerBuffer = new byte[ProtocolHeader.HeaderSize];
        
        while (true)
        {
            try
            {
                await stream.ReadExactlyAsync(headerBuffer);
                
                var (category, bodyLength) = ProtocolHeader.Read(headerBuffer);
                if (category == ProtocolCategory.None)
                {
                    //TODO: Log
                    break;
                }
                
                var bodyBuffer = _receiveBufferPool.Rent(bodyLength);
                await stream.ReadExactlyAsync(bodyBuffer);
                
                Dispatch(category, bodyBuffer);
            }
            catch
            {
                break;
            }
        }
    }

    private async Task Send()
    {
        await foreach (var buffer in Sender.Reader.ReadAllAsync())
        {
            try
            {
                await stream.WriteAsync(buffer);
            }
            catch
            {
                break;
            }
            
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void Dispatch(ProtocolCategory category, byte[] buffer)
    {
        switch (category)
        {
            case ProtocolCategory.Game:
            {
                var game = GameProtocol.Parser.ParseFrom(buffer);
                break;
            }
            case ProtocolCategory.Net:
            {
                var net = NetProtocol.Parser.ParseFrom(buffer);
                break;
            }
            case ProtocolCategory.Auth:
            case ProtocolCategory.None:
            default: break;
        }
        
        _receiveBufferPool.Return(buffer);
    }
}