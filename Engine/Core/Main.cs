using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spire.Core.Log;

namespace Spire.Core;

public partial class Main : Node
{
    public ServiceProvider? ServiceProvider { get; private set; }

    public override void _EnterTree()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new GodotLoggerProvider(Config.LogLevel));
            builder.SetMinimumLevel(Config.LogLevel);
        });
        
        ServiceProvider = services.BuildServiceProvider();
    }

    public override void _ExitTree()
    {
        ServiceProvider?.Dispose();
    }
}