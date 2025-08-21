using Godot;
using Microsoft.Extensions.Logging;

namespace Spire.Core.Log;

public class GodotLogger(string categoryName, LogLevel minLogLevel = LogLevel.Information)
    : ILogger
{
    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= minLogLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;
        
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ssZ");
        var message = formatter(state, exception);
        var logMessage = $"[{timestamp}] [{logLevel}] {categoryName}: {message}";
        
        switch (logLevel)
        {
            case LogLevel.Critical:
                GD.PrintRich($"[color=rd][b] CRIT[b][/color] {logMessage}");
                break;
            case LogLevel.Error:
                GD.PrintRich($"[color=rd]ERROR[/color] {logMessage}");
                break;
            case LogLevel.Warning:
                GD.PrintRich($"[color=rd] WARN[/color] {logMessage}");
                break;
            case LogLevel.Debug:
                GD.PrintRich($"[color=rd]DEBUG[/color] {logMessage}");
                break;
            case LogLevel.Trace:
                GD.PrintRich($"[color=rd]TRACE[/color] {logMessage}");
                break;
            case LogLevel.Information:
            case LogLevel.None:
            default:
                GD.Print(logMessage);
                break;
        }

        if (exception == null) return;
        
        GD.PrintRich($"[color=red]Exception: {exception.GetType().Name}: {exception.Message}[/color]");
        if (OS.IsDebugBuild())
        {
            GD.PrintRich($"[color=gray]{exception.StackTrace}[/color]");
        }
    }
}