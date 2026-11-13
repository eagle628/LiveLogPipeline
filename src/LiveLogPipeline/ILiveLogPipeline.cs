namespace LiveLogPipeline;

public interface ILiveLogPipeline
{
    public int Count { get; }
    public int Capacity { get; }
    public event Action<LogBatchEventArgs>? LogBatchReceived;
    public event LogBatchSpanAction? LogBatchReceivedFast;
    public LogEntry[] GetSnapshot();
    public int CopyTo(Span<LogEntry> destination);
}
public delegate void LogBatchSpanAction(ReadOnlySpan<LogEntry> entries);
public readonly record struct LogBatchEventArgs(
    ReadOnlyMemory<LogEntry> Entries
);

public interface IScopedLiveLogPipeline : ILiveLogPipeline
{
    
}