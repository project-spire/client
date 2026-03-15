using Spire.Core.Network;
using Spire.Core.State;
using Spire.Message.Game;
using Spire.Message.Game.Play;

namespace Spire.Core.Handler.Play;

public static class EntityDespawnHandler
{
    [MessageHandler(typeof(EntityDespawnMessage))]
    public static void Handle(EntityDespawn data, GameState state)
    {
        state.RemoveEntity(data.Entity);
    }
}
