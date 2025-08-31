using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spire.Core;
using Spire.Core.BehaviorTree;

namespace Spire.Bot.Node;

public class CharacterActionNode() : ActionNode(ctx => RequestCharacter((BotContext)ctx))
{
    private static async ValueTask<NodeState> RequestCharacter(BotContext ctx)
    {
        // var characters = await ListCharacter(ctx);
        // if (characters.Count == 0)
        // {
        //     ctx.Character = await CreateCharacter(ctx);
        // }
        // else
        // {
        //     ctx.Character = characters[0];
        // }

        return NodeState.Success;
    }
    
    // private static async ValueTask<List<Character>> ListCharacter(BotContext ctx)
    // {
    //     var url = Config.LobbyAddress + "/character/list";
    //     
    //     var resp = await ctx.Request(url, string.Empty);
    //     var rawCharacters = resp.GetProperty("characters");
    //     var characters = new List<Character>();
    //     
    //     for (var i = 0; i < rawCharacters.GetArrayLength(); i++)
    //     {
    //         var rawCharacter = rawCharacters[i];
    //         if (!Guid.TryParse(rawCharacter.GetProperty("id").GetString(), out var characterId))
    //             throw new Exception("Invalid UUID from character id");
    //         
    //         characters.Add(new Character
    //         (
    //             Id: characterId,
    //             Name: rawCharacter.GetProperty("name").GetString()!,
    //             Race: rawCharacter.GetProperty("race").GetString()!
    //         ));
    //     }
    //     
    //     return characters;
    // }
    //
    // private static async ValueTask<Character> CreateCharacter(BotContext ctx)
    // {
    //     var url = Config.LobbyAddress + "/character/create";
    //     var data = JsonSerializer.Serialize(new Dictionary<string, object>
    //     {
    //         ["character_name"] = ctx.DevId + "_c",
    //         ["character_race"] = "Human"
    //     });
    //     
    //     var resp = await ctx.Request(url, data);
    //     var rawCharacter = resp.GetProperty("character");
    //     if (!Guid.TryParse(rawCharacter.GetProperty("id").GetString(), out var characterId))
    //         throw new Exception("Invalid UUID from character id");
    //     
    //     var character = new Character
    //     (
    //         Id: characterId,
    //         Name: rawCharacter.GetProperty("name").GetString()!,
    //         Race: rawCharacter.GetProperty("race").GetString()!
    //     );
    //     
    //     ctx.Logger.LogInformation("Created character {}", character.Id);
    //     return character;
    // }
}