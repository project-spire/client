namespace Spire.Lobby;

public abstract record Account
{
    public required Guid AccountId { get; init; }
}

public record DevAccount : Account
{
    public required string DevId { get; init; }
}
