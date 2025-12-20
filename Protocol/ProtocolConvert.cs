using System.Buffers.Binary;

namespace Spire.Protocol;

public static class ProtocolConvert
{
    // public static Guid ToGuid(this Uuid uuid)
    // {
    //     Span<byte> bytes = stackalloc byte[16];
    //     BinaryPrimitives.WriteUInt64BigEndian(bytes, uuid.High);
    //     BinaryPrimitives.WriteUInt64BigEndian(bytes[8..], uuid.Low);
    //     
    //     return new Guid(bytes);
    // }
    //
    // public static Uuid ToUuid(this Guid guid)
    // {
    //     var bytes = guid.ToByteArray();
    //     var high = BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(0, 8));
    //     var low = BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(8, 8));
    //
    //     return new Uuid
    //     {
    //         High = high,
    //         Low = low
    //     };
    // }
}
