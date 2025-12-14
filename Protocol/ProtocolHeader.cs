namespace Spire.Protocol;

public readonly struct ProtocolHeader(int length, ushort id)
{
    public const int Size = 4;
    
    public readonly int Length = length;
    public readonly ushort Id = id;
    
    public void Encode(Span<byte> buffer)
    {
        if (buffer.Length < Size)
            throw new Exception($"Header buffer must be at least {Size} bytes long");
        
        buffer[0] = (byte)(Length >> 8);
        buffer[1] = (byte)Length;
        buffer[2] = (byte)(Id >> 8);
        buffer[3] = (byte)Id;
    }
    
    public static ProtocolHeader Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < Size)
            throw new Exception($"Header buffer must be at least {Size} bytes long");
        
        int length = buffer[0] << 8 | buffer[1];
        ushort id = (ushort)(buffer[2] << 8 | buffer[3]);
        
        return new ProtocolHeader(length, id);
    }
}
