using System.Numerics;
using Microsoft.Extensions.Options;

namespace LiveLogPipeline.Internal;

internal sealed class LiveLogPipelineCore : ILiveLogPipeline, IDisposable
{
    internal const int MAX_MINIMUM_RETAINED_ENTRY_COUNT = 1 << 30;
    private readonly Lock _lock;
    private readonly LogEntry[] _buffer;
    private int _count;
    private readonly int _capacity;
    private readonly int _mask;
    private int _top;
    public LiveLogPipelineCore(IOptions<LiveLogPipelineOptions> options)
    {
        _lock = new Lock();
        _capacity = RoundUpToPowerOfTwo(options.Value.MinimumRetainedEntryCount);
        _mask = _capacity - 1;
        _top = 0;
        _buffer = new LogEntry[_capacity];
        _count = 0;
    }
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
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

    public LogEntry[] GetSnapshot()
    {
        lock (_lock)
        {
            var snapshot = new LogEntry[_count];
            _ = CopyToCore(snapshot);
            return snapshot;
        }
    }

    public int CopyTo(Span<LogEntry> destination)
    {
        lock (_lock)
        {
            if (destination.Length < _count)
            {
                throw new ArgumentException(
                    "The destination is too small to contain the log snapshot.",
                    nameof(destination));
            }

            return CopyToCore(destination);
        }
    }

    private int CopyToCore(Span<LogEntry> destination)
    {
        if (_count == 0)
        {
            return 0;
        }

        if (_count < _capacity)
        {
            _buffer.AsSpan(0, _count).CopyTo(destination);
            return _count;
        }

        if (_count == _capacity)
        {
            int firstLength = _capacity - _top;

            _buffer.AsSpan(_top, firstLength).CopyTo(destination);
            _buffer.AsSpan(0, _top).CopyTo(destination[firstLength..]);
            return _count;
        }

        throw new InvalidOperationException("DevError:Unexpected buffer state.");
    }

    public void Push(LogEntry logEntry)
    {
        // OPT: consider SeqLock for single-writer and multiple-reader scenarios to reduce locking overhead
        lock (_lock)
        {
            _buffer[_top] = logEntry;
            if (_count < _capacity)
            {
                _count++;
            }
            _top++;
            _top &= _mask;
        }
    }
    
    private static int RoundUpToPowerOfTwo(int capacity)
    {
        return 1 << (BitOperations.Log2((uint)(capacity - 1)) + 1);
    }
}