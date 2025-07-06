namespace Spire.Core.BehaviorTree;

public enum NodeState
{
    Success,
    Failure
}

public abstract class Node
{
    public abstract ValueTask<NodeState> Run();
}
