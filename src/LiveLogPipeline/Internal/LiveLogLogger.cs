using Microsoft.Extensions.Logging;

namespace LiveLogPipeline.Internal;

internal class LiveLogLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LiveLogRouter _router;
    private readonly LiveLogPipelineOptions _options;

    public LiveLogLogger(string categoryName, LiveLogRouter router, LiveLogPipelineOptions options)
    {
        _categoryName = categoryName;
        _router = router;
        _options = options;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        // return no-op disposable
        return default;
    }
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None
            && logLevel >= _options.MinLogLevel;
    }

    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        // early return if the log level is not enabled
        if (!IsEnabled(logLevel))
        {
            return;
        }
        // early return if options does not include event id
        if (_options.IncludedEventIds is not null && !_options.IncludedEventIds.Contains(eventId.Id))
        {
            return;
        }

        // early return if the formatted message is empty
        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        // create the log envelope and push it to the router
        var envelope = new LogEnvelope(
            DateTimeOffset.UtcNow,
            logLevel,
            eventId,
            _categoryName,
            message,
            exception,
            Environment.CurrentManagedThreadId);
        _router.Push(envelope);
    }
}