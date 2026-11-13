using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LiveLogPipeline.Internal;

internal sealed class LiveLogLoggerProvider : ILoggerProvider
{
    private readonly LiveLogRouter _router;
    private readonly ConcurrentDictionary<string, ILogger> _loggers;
    public LiveLogLoggerProvider(LiveLogRouter router)
    {
        _router = router;
        _loggers = new ConcurrentDictionary<string, ILogger>();
    }
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new LiveLogLogger(name, _router));
    }

    public void Dispose()
    {
    }
}