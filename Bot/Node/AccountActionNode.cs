using Spire.Core.BehaviorTree;
using Spire.Protocol.Lobby;

namespace Spire.Bot.Node;

public class AccountActionNode() : ActionNode(ctx => RequestAccount((BotContext)ctx))
{
    private static async ValueTask<NodeState> RequestAccount(BotContext ctx)
    {
        var client = new DevAuth.DevAuthClient(ctx.LobbyChannel);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        
        var devAccountRequest = new GetDevAccountRequest
        {
            DevId = ctx.DevId
        };
        var devAccountResponse = await client.GetDevAccountAsync(devAccountRequest, deadline: deadline);

        var tokenRequest = new GetDevTokenRequest
        {
            AccountId = devAccountResponse.AccountId
        };
        var tokenResponse = await client.GetDevTokenAsync(tokenRequest, deadline: deadline);
        
        ctx.OnDevAccountAcquired(devAccountResponse.AccountId, tokenResponse.Token);
        return NodeState.Success;
    }
}