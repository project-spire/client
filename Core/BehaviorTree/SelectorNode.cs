namespace Spire.Core.BehaviorTree;

public class SelectorNode(List<Node> children) : Node
{
    public override async ValueTask<NodeState> Run(IContext ctx)
    {
        foreach (var node in children)
        {
            switch (await node.Run(ctx))
            {
                case NodeState.Success:
                    return NodeState.Success;
                case NodeState.Failure:
                    continue;
            }
        }
        return NodeState.Failure;
    }
}
