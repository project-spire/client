namespace Spire.Core.BehaviorTree;

public abstract class ModeNode<T>(T mode, Dictionary<T, Node> modes) : Node
    where T : Enum
{
    protected T mode = mode;
    protected Dictionary<T, Node> modes = modes;

    public override async ValueTask<NodeState> Run(INodeContext ctx)
    {
        var result = await modes[mode].Run(ctx);
        Transit(result);
        return result;
    }

    protected abstract void Transit(NodeState state);
}