using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spire.Bot;
using Spire.Bot.Network;
using Spire.Bot.Node;
using Spire.Core.BehaviorTree;
using Spire.Protocol.Game;
using Spire.Protocol.Game.Auth;

var services = new ServiceCollection()
    .AddLogging(configure =>
    {
        configure.AddConsole();
        configure.SetMinimumLevel(LogLevel.Debug);
    })
    .BuildServiceProvider();

var logger = services.GetRequiredService<ILogger<Program>>();
var loggerFactory = services.GetRequiredService<ILoggerFactory>();

BotProtocolDispatcher.Initialize(Assembly.GetExecutingAssembly());

logger.LogInformation("Starting {NumBots} bots...", Config.BotCount);

List<Task> botTasks = [];
for (ushort botId = 1; botId <= Config.BotCount; botId++)
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

        await ctx.GameSession.ConnectAsync(Config.GameHost, Config.GamePort);

        var login = new LoginProtocol(new Login
        {
            Kind = Login.Types.Kind.Enter,
            Token = ctx.Token,
            CharacterId = ctx.Character!.Id
        });
        await ctx.GameSession.LoginAsync(login);
        
        await ctx.GameSession.StartAsync();

        await Task.WhenAny(ctx.Stopped, ctx.GameSession.CompletionTask);
    }
    catch (Exception e)
    {
        ctx.Logger.LogError(e, "Error");
    }
}
