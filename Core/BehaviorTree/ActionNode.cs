namespace Spire.Core.BehaviorTree;

public class ActionNode(ValueTask<NodeState> task) : Node
{
    public override async ValueTask<NodeState> Run(IContext ctx)
    {
        return await task;
    }
}
