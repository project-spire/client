namespace Spire.Core.BehaviorTree;

public class Condition(ValueTask<bool> task) : Node
{
    public override async ValueTask<NodeState> Run()
    {
        return await task ? NodeState.Success : NodeState.Failure;
    }
}
