using Spire.Core.BehaviorTree;
using Spire.Protocol.Lobby;

namespace Spire.Bot.Node;

public class AccountActionNode() : ActionNode(ctx => RequestAccount((BotContext)ctx))
{
    private static async ValueTask<NodeState> RequestAccount(BotContext ctx)
    {
        var devAuthClient = new DevAuth.DevAuthClient(ctx.LobbyChannel);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        
        var devAccountRequest = new GetDevAccountRequest
        {
            DevId = ctx.DevId
        };
        var devAccountResponse = await devAuthClient.GetDevAccountAsync(devAccountRequest, deadline: deadline);

        var tokenRequest = new GetDevTokenRequest();
        var tokenResponse = await devAuthClient.GetDevTokenAsync(tokenRequest, deadline: deadline);
        
        ctx.OnDevAccountAcquired(devAccountResponse.AccountId, tokenResponse.Token);
        return NodeState.Success;
    }
}