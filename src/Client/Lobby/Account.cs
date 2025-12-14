namespace Spire.Lobby;

public abstract record Account
{
    public required long AccountId { get; init; }
}

public record DevAccount : Account
{
    public required string DevId { get; init; }
}
