namespace Spire.Core.BehaviorTree;

public class Selector(List<Node> children) : Node
{
    public override async ValueTask<NodeState> Run()
    {
        foreach (var node in children)
        {
            switch (await node.Run())
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
