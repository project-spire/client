using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spire.Bot;
using Spire.Bot.Node;
using Spire.Core.BehaviorTree;
using Spire.Core.Network;

var services = new ServiceCollection()
    .AddLogging(configure =>
    {
        configure.AddConsole();
        configure.SetMinimumLevel(LogLevel.Debug);
    })
    .BuildServiceProvider();

var logger = services.GetRequiredService<ILogger<Program>>();
var loggerFactory = services.GetRequiredService<ILoggerFactory>();

ProtocolDispatcher.Register(Assembly.GetExecutingAssembly());

logger.LogInformation("Starting {NumBots} bots...", Settings.BotCount);

List<Task> botTasks = [];
for (ushort botId = 1; botId <= Settings.BotCount; botId++)
{
    var botLogger = loggerFactory.CreateLogger<BotContext>();
    var botContext = new BotContext(botId, botLogger);
    
    botTasks.Add(StartBotAsync(botContext));
}

await Task.WhenAll(botTasks);
logger.LogInformation("End");
return;


async Task StartBotAsync(BotContext ctx)
{
    ctx.Logger.LogInformation("Starting...");

    try
    {
        await new SequenceNode(
        [
            new AccountActionNode(),
            new CharacterActionNode()
        ]).Run(ctx);
    }
    catch (Exception e)
    {
        ctx.Logger.LogError(e, "Error");
    }
}
