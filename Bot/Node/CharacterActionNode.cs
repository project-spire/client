using Google.Protobuf.WellKnownTypes;
using Spire.Core.BehaviorTree;
using Spire.Protocol;
using Spire.Protocol.Lobby;

namespace Spire.Bot.Node;

public class CharacterActionNode() : ActionNode(ctx => RequestCharacter((BotContext)ctx))
{
    private static async ValueTask<NodeState> RequestCharacter(BotContext ctx)
    {
        var client = new Characters.CharactersClient(ctx.LobbyChannel);
        var deadline = DateTime.UtcNow.AddSeconds(10);

        var listCharactersResponse = await client.ListCharactersAsync(new Empty(), deadline: deadline);
        
        Protocol.Character character;
        if (listCharactersResponse.Characters.Count == 0)
        {
            var createCharacterRequest = new CreateCharacterRequest
            {
                Name = $"{Config.BotPrefix}{ctx.BotId:D4}",
                Race = Race.Human
            };
            var createCharacterResponse = await client.CreateCharacterAsync(createCharacterRequest, deadline: deadline);
            
            character = createCharacterResponse.Character;
        }
        else
        {
            character = listCharactersResponse.Characters[0];
        }
        
        ctx.OnCharacterAcquired(character);
        return NodeState.Success;
    }
}