using LiveLogPipeline;

namespace LiveLogPipeline.Internal;

internal sealed class LiveLogPipelineCore : ILiveLogPipeline, IDisposable
{
    private readonly LogEntry[] _buffer;
    private int _count;
    private int _capFlag;
    private readonly int _capacity;
    private readonly int _mask;
    private int _top;
    public LiveLogPipelineCore()
    {
        //TODO: Make the buffer size configurable in the future
        int capacity = 1024;
        _capacity = capacity;
        _mask = capacity - 1;
        _top = _mask;
        _buffer = new LogEntry[capacity];
        _count = 0;
    }
    public int Count
    {
        get
        {
            if (Volatile.Read(ref _capFlag) == 0)
            {
                return Volatile.Read(ref _count) & _mask;
            }
            else
            {
                return _capacity;
            }
        }
    }

    public int Capacity => _capacity;

    public event Action<LogBatchEventArgs>? LogBatchReceived;
    public event LogBatchSpanAction? LogBatchReceivedFast;

    public void Dispose()
    {
        // Dispose resources if any
    }

    public LogEntry[] GetSnapshot() => throw new NotImplementedException();
    public int CopyTo(Span<LogEntry> destination) => throw new NotImplementedException();

    public void Push(LogEntry logEntry)
    {
        int top = Interlocked.Increment(ref _top);
        int index = top & _mask;
        _buffer[index] = logEntry;
        int count = Interlocked.Increment(ref _count);
        int capFlag = Volatile.Read(ref _capFlag);
        // TODO: This is extremely unlikely, but _capFlag may not work correctly if roughly half of Int.MaxValue logs are added in a single run.
        if (capFlag == 0 && count >= _capacity)
        {
            _ = Interlocked.Exchange(ref _capFlag, 1);
        }
    }
}