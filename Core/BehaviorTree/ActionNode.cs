namespace Spire.Core.BehaviorTree;

public class ActionNode(Func<INodeContext, ValueTask<NodeState>> task) : Node
{
    public override async ValueTask<NodeState> Run(INodeContext ctx)
    {
        return await task(ctx);
    }
}
