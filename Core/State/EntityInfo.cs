namespace Spire.Core.State;

public enum EntityKind
{
    Unknown,
    Player,
    Character,
    Item
}

public record EntityInfo
{
    public required ulong Id { get; init; }
    public required EntityKind Kind { get; init; }
    public string? Name { get; init; }
}
