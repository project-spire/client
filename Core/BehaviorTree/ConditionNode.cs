namespace Spire.Core.BehaviorTree;

public class ConditionNode(ValueTask<bool> task) : Node
{
    public override async ValueTask<NodeState> Run(IContext ctx)
    {
        return await task ? NodeState.Success : NodeState.Failure;
    }
}
