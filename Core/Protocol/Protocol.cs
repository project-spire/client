using System;

namespace Spire.Core.Protocol;

public enum ProtocolCategory : byte
{
    None = 0,
    Auth = 1,
    Net = 2,
    Game = 3
}

public static class ProtocolHeader
{
    public const int Size = 4;
    private const byte Reserved = 0;

    public static void Write(ProtocolCategory category, int length, Span<byte> buffer)
    {
        if (buffer.Length < 4)
            throw new ArgumentOutOfRangeException("Buffer must be at least 4 bytes long");
        if (length > ushort.MaxValue)
            throw new ArgumentOutOfRangeException("Length must be less than 2 bytes");

        buffer[0] = (byte)category;
        buffer[1] = Reserved;
        buffer[2] = (byte)(length >> 8);
        buffer[3] = (byte)length;
    }

    public static (ProtocolCategory, ushort) Read(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 4)
            throw new ArgumentOutOfRangeException("Buffer must be at least 4 bytes long");

        var category = buffer[0] switch
        {
            1 => ProtocolCategory.Auth,
            2 => ProtocolCategory.Net,
            3 => ProtocolCategory.Game,
            _ => ProtocolCategory.None
        };
        var length = (ushort)((buffer[2] << 8) | buffer[3]);

        return (category, length);
    }
}