namespace Spire.Core.State;

public class GameState
{
    public Dictionary<ulong, EntityInfo> Entities { get; } = [];

    public event Action<EntityInfo>? EntitySpawned;
    public event Action<ulong>? EntityDespawned;

    public void AddEntity(EntityInfo entity)
    {
        Entities[entity.Id] = entity;
        EntitySpawned?.Invoke(entity);
    }

    public void RemoveEntity(ulong id)
    {
        Entities.Remove(id);
        EntityDespawned?.Invoke(id);
    }
}
