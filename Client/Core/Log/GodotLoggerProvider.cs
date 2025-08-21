using Microsoft.Extensions.Logging;

namespace Spire.Core.Log;

public class GodotLoggerProvider(LogLevel minLogLevel = LogLevel.Information) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new GodotLogger(categoryName, minLogLevel);
    }

    public void Dispose() { }
}