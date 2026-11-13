using Microsoft.Extensions.Logging;

namespace LiveLogPipeline;

public readonly record struct LogEntry(
    long SequenceId,
    DateTimeOffset Timestamp,
    LogLevel LogLevel,
    EventId EventId,
    string Category,
    string Message,
    Exception? Exception = null,
    int ManagedThreadId = 0);