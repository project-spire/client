using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Spire.Core.Log;

public abstract partial class LoggableNode : Node
{
    protected ILogger Logger { get; private set; } = null!;

    public override void _Ready()
    {
        var loggerFactory = GetNode<Main>("/root/Main").ServiceProvider!.GetRequiredService<ILoggerFactory>();
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }
}