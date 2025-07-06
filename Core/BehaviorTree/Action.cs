namespace Spire.Core.BehaviorTree;

public class Action(ValueTask<NodeState> task) : Node
{
    public override async ValueTask<NodeState> Run()
    {
        return await task;
    }
}
