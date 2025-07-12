namespace Spire.Core.BehaviorTree;

public class SequenceNode(List<Node> children) : Node
{
    public override async ValueTask<NodeState> Run(INodeContext ctx)
    {
        foreach (var node in children)
        {
            switch (await node.Run(ctx))
            {
                case NodeState.Success:
                    continue;
                case NodeState.Failure:
                    return NodeState.Failure;
            }
        }
        return NodeState.Success;
    }
}
