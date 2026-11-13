using Microsoft.Extensions.Logging;

namespace LiveLogPipeline.Internal;

internal class LiveLogLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LiveLogRouter _router;

    public LiveLogLogger(string categoryName, LiveLogRouter router)
    {
        _categoryName = categoryName;
        _router = router;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        throw new NotImplementedException();
    }
    public bool IsEnabled(LogLevel logLevel)
    {
        throw new NotImplementedException();
    }

    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        throw new NotImplementedException();
    }
}