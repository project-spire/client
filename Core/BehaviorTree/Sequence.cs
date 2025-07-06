namespace Spire.Core.BehaviorTree;

public class Sequence(List<Node> children) : Node
{
    public override async ValueTask<NodeState> Run()
    {
        foreach (var node in children)
        {
            switch (await node.Run())
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
