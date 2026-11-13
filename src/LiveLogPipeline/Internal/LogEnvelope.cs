using Microsoft.Extensions.Logging;

namespace LiveLogPipeline.Internal;
internal readonly record struct LogEnvelope(
    DateTimeOffset Timestamp,
    LogLevel LogLevel,
    EventId EventId,
    string Category,
    string Message,
    Exception? Exception = null,
    int ManagedThreadId = 0);