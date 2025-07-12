using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spire.Core;
using Spire.Core.BehaviorTree;

namespace Spire.Bot.Node;

public class CharacterActionNode() : ActionNode(ctx => RequestCharacter((BotContext)ctx))
{
    private static async ValueTask<NodeState> RequestCharacter(BotContext ctx)
    {
        var characters = await ListCharacter(ctx);
        if (characters.Count == 0)
        {
            ctx.Character = await CreateCharacter(ctx);
        }
        else
        {
            ctx.Character = characters[0];
        }

        return NodeState.Success;
    }
    
    private static async ValueTask<List<Character>> ListCharacter(BotContext ctx)
    {
        var url = Settings.LobbyUrl + "/character/list";
        
        var resp = await ctx.Request(url, string.Empty);
        var rawCharacters = resp.GetProperty("characters");
        var characters = new List<Character>();
        
        for (var i = 0; i < rawCharacters.GetArrayLength(); i++)
        {
            var rawCharacter = rawCharacters[i];
            characters.Add(new Character
            (
                rawCharacter.GetProperty("id").GetInt64(),
                rawCharacter.GetProperty("name").GetString()!,
                rawCharacter.GetProperty("race").GetString()!
            ));
        }
        
        return characters;
    }
    
    private static async ValueTask<Character> CreateCharacter(BotContext ctx)
    {
        var url = Settings.LobbyUrl + "/character/create";
        var data = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["character_name"] = ctx.DevId + "_c",
            ["character_race"] = "Human"
        });
        
        var resp = await ctx.Request(url, data);
        var rawCharacter = resp.GetProperty("character");
        var character = new Character
        (
            rawCharacter.GetProperty("id").GetInt64(),
            rawCharacter.GetProperty("name").GetString()!,
            rawCharacter.GetProperty("race").GetString()!
        );
        
        ctx.Logger.LogInformation("Created character {}", character.Id);
        return character;
    }
}