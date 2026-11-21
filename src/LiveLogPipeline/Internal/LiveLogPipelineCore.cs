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

    private readonly TimeSpan _flushInterval;
    private readonly int _maxBatchSize;

    private readonly LogEntry[] _pendingLogEntries;
    private int _pendingLogEntriesCount;
    private bool _isTimerRunning;
    private readonly ITimer _flushTimer;
    private bool _isDisposed;

    public LiveLogPipelineCore(IOptions<LiveLogPipelineOptions> options, TimeProvider timerProvider)
    {
        _lock = new Lock();

        _capacity = RoundUpToPowerOfTwo(options.Value.MinimumRetainedEntryCount);
        _mask = _capacity - 1;
        _top = 0;
        _buffer = new LogEntry[_capacity];
        _count = 0;

        _flushInterval = options.Value.FlushInterval;
        _maxBatchSize = options.Value.MaxBatchSize;

        _pendingLogEntries = new LogEntry[_maxBatchSize];
        _pendingLogEntriesCount = 0;
        _isTimerRunning = false;
        _flushTimer = timerProvider.CreateTimer(
            FlushCallback,
            this,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);
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
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;

            if (_isTimerRunning)
            {
                _ = _flushTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _isTimerRunning = false;
            }
            _flushTimer.Dispose();
        }
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
            if (_isDisposed)
            {
                return;
            }

            _buffer[_top] = logEntry;
            if (_count < _capacity)
            {
                _count++;
            }
            _top++;
            _top &= _mask;

            if (!_isTimerRunning)
            {
                _ = _flushTimer.Change(_flushInterval, Timeout.InfiniteTimeSpan);
                _isTimerRunning = true;
            }
            _pendingLogEntries[_pendingLogEntriesCount] = logEntry;
            _pendingLogEntriesCount++;
            if (_pendingLogEntriesCount == _maxBatchSize)
            {
                FlushLocked();
            }
        }
    }

    private void FlushCallback(object? _)
    {
        lock (_lock)
        {
            if (_isDisposed)
            {
                return;
            }

            FlushLocked();
        }
    }

    private void FlushLocked()
    {
        if (_isTimerRunning)
        {
            _ = _flushTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _isTimerRunning = false;
        }

        var batch = DrainPendingBatchLocked();
        if (batch is not null)
        {
            InvokeSafeHandlers(batch);
        }
    }

    private void InvokeSafeHandlers(LogEntry[] batch)
    {
        Action<LogBatchEventArgs>? handler = LogBatchReceived;
        if (handler is null)
        {
            return;
        }

        var args = new LogBatchEventArgs(batch);

        // .NET 9++ optimization
        if (handler.HasSingleTarget)
        {
            try
            {
                handler(args);
            }
            catch { }
            return;
        }

        foreach (Delegate subscriber in handler.GetInvocationList())
        {
            try
            {
                ((Action<LogBatchEventArgs>)subscriber).Invoke(args);
            }
            catch { }
        }
    }
    private LogEntry[]? DrainPendingBatchLocked()
    {
        if (_pendingLogEntriesCount == 0)
        {
            return null;
        }

        if (LogBatchReceived is null)
        {
            Array.Clear(_pendingLogEntries, 0, _pendingLogEntriesCount);
            _pendingLogEntriesCount = 0;
            return null;
        }

        var batch = _pendingLogEntries[.._pendingLogEntriesCount].ToArray();
        _pendingLogEntriesCount = 0;
        return batch;
    }
    
    private static int RoundUpToPowerOfTwo(int capacity)
    {
        return 1 << (BitOperations.Log2((uint)(capacity - 1)) + 1);
    }
}