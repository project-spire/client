using System;
using System.Buffers;
using spire.protocol.net;

namespace spire.network;

public class NetProtocolHandler
{
    public void Handle(NetProtocol protocol)
    {
        switch (protocol.ProtocolCase)
        {
            case NetProtocol.ProtocolOneofCase.Ping:
                HandlePing();
                break;
            default:
                break;
        }
    }
    
    public void HandlePing()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(size);

        _ = channel.Writer.WriteAsync(buffer.AsMemory());
    }
}