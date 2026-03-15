using Spire.Core.Network;
using Spire.Core.State;
using Spire.Message.Game;
using Spire.Message.Game.Play;

namespace Spire.Core.Handler.Play;

public static class EntitySpawnHandler
{
    [MessageHandler(typeof(EntitySpawnMessage))]
    public static void Handle(EntitySpawn data, GameState state)
    {
        var kind = data.DataCase switch
        {
            EntitySpawn.DataOneofCase.Player => EntityKind.Player,
            EntitySpawn.DataOneofCase.Character => EntityKind.Character,
            EntitySpawn.DataOneofCase.Item => EntityKind.Item,
            _ => EntityKind.Unknown
        };

        var name = data.DataCase switch
        {
            EntitySpawn.DataOneofCase.Player => data.Player.Character.Name,
            EntitySpawn.DataOneofCase.Character => data.Character.Name,
            _ => null
        };

        state.AddEntity(new EntityInfo
        {
            Id = data.Entity,
            Kind = kind,
            Name = name
        });
    }
}
