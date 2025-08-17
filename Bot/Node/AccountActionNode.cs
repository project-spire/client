using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spire.Core;
using Spire.Core.BehaviorTree;

namespace Spire.Bot.Node;

public class AccountActionNode() : ActionNode(ctx => RequestAccount((BotContext)ctx))
{
    private static async ValueTask<NodeState> RequestAccount(BotContext ctx)
    {
        var (found, accountId) = await FindAccount(ctx);
        if (!found)
        {
            accountId = await CreateAccount(ctx);
        }
        
        var token = await RequestToken(ctx, accountId);
        ctx.Account = new Account(accountId, token);
        
        ctx.Logger.LogDebug("Token: {token}", token);
        
        return NodeState.Success;
    }

    private static async ValueTask<(bool, Guid)> FindAccount(BotContext ctx)
    {
        var url = Config.LobbyUrl + "/account/dev/me";
        var data = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["dev_id"] = ctx.DevId
        });
        
        var resp = await ctx.Request(url, data, false);
        var found = resp.GetProperty("found").GetBoolean();
        if (!Guid.TryParse(resp.GetProperty("account_id").GetString(), out var accountId))
            throw new Exception("Invalid UUID of account id");
        
        return (found, accountId);
    }
    
    private static async ValueTask<Guid> CreateAccount(BotContext ctx)
    {
        var url = Config.LobbyUrl + "/account/dev/create";
        var data = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["dev_id"] = ctx.DevId
        });
        
        var resp = await ctx.Request(url, data, false);
        if (!Guid.TryParse(resp.GetProperty("account_id").GetString(), out var accountId))
            throw new Exception("Invalid UUID of account id");
        
        return accountId;
    }

    private static async ValueTask<string> RequestToken(BotContext ctx, Guid accountId)
    {
        var url = Config.LobbyUrl + "/account/dev/token";
        var data = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["account_id"] = accountId.ToString()
        });
        
        var resp = await ctx.Request(url, data, false);
        var token = resp.GetProperty("token").GetString();
        
        if (token == null)
            throw new Exception("Token not found");
        
        return token;
    }
}