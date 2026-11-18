using Microsoft.Extensions.Logging;

namespace LiveLogPipeline;

public class LiveLogPipelineOptions
{
    public int MaxCapacity { get; set; } = 1024;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    public int MaxBatchSize { get; set; } = 100;
    public BatchMode Mode { get; set; } = BatchMode.Periodic;
    public LogLevel MinLogLevel { get; set; } = LogLevel.Information;
    public HashSet<int>? IncludedEventIds { get; set; }
}

public enum BatchMode
{
    Periodic,
    Debounce
}