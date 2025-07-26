using System.Buffers.Binary;
using Spire.Protocol;

namespace Spire.Core;

public static class ProtocolConvert
{
    public static Uuid ToUuid(Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        if (!guid.TryWriteBytes(bytes, bigEndian: true, out _))
            throw new FormatException("Invalid guid");
        
        var high = BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]);
        var low = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
        
        return new Uuid { High = high, Low = low };
    }
}