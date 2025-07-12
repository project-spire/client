using System.Text.Json;
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
        
        return NodeState.Success;
    }

    private static async ValueTask<(bool, long)> FindAccount(BotContext ctx)
    {
        var url = Settings.LobbyUrl + "/account/dev/me";
        var data = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["dev_id"] = ctx.DevId
        });
        
        var resp = await ctx.Request(url, data, false);
        var found = resp.GetProperty("found").GetBoolean();
        var accountId = resp.GetProperty("account_id").GetInt64();
        
        return (found, accountId);
    }
    
    private static async ValueTask<long> CreateAccount(BotContext ctx)
    {
        var url = Settings.LobbyUrl + "/account/dev/create";
        var data = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["dev_id"] = ctx.DevId
        });
        
        var resp = await ctx.Request(url, data, false);
        var accountId = resp.GetProperty("account_id").GetInt64();
        
        return accountId;
    }

    private static async ValueTask<string> RequestToken(BotContext ctx, long accountId)
    {
        var url = Settings.LobbyUrl + "/account/dev/token";
        var data = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["account_id"] = accountId
        });
        
        var resp = await ctx.Request(url, data, false);
        var token = resp.GetProperty("token").GetString();
        
        if (token == null)
            throw new Exception("Token not found");
        
        return token;
    }
}