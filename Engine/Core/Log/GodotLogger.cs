using Godot;
using Microsoft.Extensions.Logging;

namespace Spire.Core.Log;

public class GodotLogger(string categoryName, LogLevel minLogLevel = LogLevel.Information)
	: ILogger
{
	IDisposable ILogger.BeginScope<TState>(TState state) => null!;

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
		var logTag = logLevel switch
		{
			LogLevel.Trace => "[color=gray]TRACE[/color]",
			LogLevel.Debug => "[color=gray]DEBUG[/color]",
			LogLevel.Information => "[color=green] INFO[/color]",
			LogLevel.Warning => "[color=yellow] WARN[/color]",
			LogLevel.Error => "[color=red]ERROR[/color]",
			LogLevel.Critical => "[color=red] CRIT[/color]",
			_ => ""
		};
		var message = formatter(state, exception);
		
		GD.PrintRich($"{timestamp} [{logTag}] {categoryName}: {message}");
		

		if (exception == null) return;
		
		GD.PrintRich($"[color=red]Exception: {exception.GetType().Name}: {exception.Message}[/color]");
		if (OS.IsDebugBuild())
		{
			GD.PrintRich($"[color=gray]{exception.StackTrace}[/color]");
		}
	}
}
