using Spire.Core.BehaviorTree;

namespace Spire.Bot.Node;

public class AccountActionNode : ActionNode
{
    private bool _initialized = false;

    private async ValueTask<NodeState> RequestAccount(BotContext ctx)
    {
        if (_initialized)
            return NodeState.Success;
        
       
        
        
        
        _initialized = true;
        return NodeState.Success;
    }
}