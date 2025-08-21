using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spire.Core.Log;

namespace Spire.Core;

public partial class Main : Node
{
    public ServiceProvider? ServiceProvider { get; private set; }

    public override void _Ready()
    {
        var services = new ServiceCollection();
        
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new GodotLoggerProvider(LogLevel.Debug));
        });
        
        ServiceProvider = services.BuildServiceProvider();
    }

    public override void _ExitTree()
    {
        ServiceProvider?.Dispose();
    }
}