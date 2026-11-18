using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using LiveLogPipeline;
namespace LiveLogPipeline.Internal;

[ProviderAlias("LiveLog")]
internal sealed class LiveLogLoggerProvider : ILoggerProvider
{
    private readonly LiveLogRouter _router;
    private readonly LiveLogOptions _options;
    private readonly ConcurrentDictionary<string, ILogger> _loggers;
    public LiveLogLoggerProvider(LiveLogRouter router, IOptions<LiveLogOptions> options)
    {
        _router = router;
        _options = options.Value;
        _loggers = new ConcurrentDictionary<string, ILogger>();
    }
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new LiveLogLogger(name, _router, _options));
    }

    public void Dispose()
    {
    }
}