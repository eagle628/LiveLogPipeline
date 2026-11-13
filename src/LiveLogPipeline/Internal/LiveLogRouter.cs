using System.Threading.Channels;
using System.Threading;

namespace LiveLogPipeline.Internal;

internal sealed class LiveLogRouter : IDisposable
{
    private readonly LiveLogPipelineCore _pipelineCore;

    private readonly Channel<LogEnvelope> _buffer;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _processingTask;

    public LiveLogRouter(LiveLogPipelineCore pipelineCore)
    {
        _pipelineCore = pipelineCore;
        
        _buffer = Channel.CreateBounded<LogEnvelope>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            singleReader = true,
            SingleWriter = false,
        });
        _processingTask = Task.Run(() => ProcessBufferAsync(_stop.Token));
    }
    public void Push(LogEnvelope entry)
    {
        _ = _buffer.Writer.WriteAsync(entry);
    }
    private async Task ProcessBufferAsync(CancellationToken cancellationToken)
    {
        long sequenceId = 0;
        await foreach (var envelope in
            _buffer.Reader.ReadAllAsync(cancellationToken))
        {
            var logEntry = Convert(envelope, sequenceId);
            _pipelineCore.Push(logEntry);
            sequenceId++;
        }
    }
    public void Dispose()
    {
        _buffer.Writer.TryComplete();
        _stop.Cancel();

        try
        {
            _processingTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
            // 終了時にReaderを止めた場合は想定内
        }
        finally
        {
            _stop.Dispose();
        }
    }
    private LogEntry Convert(LogEnvelope envelope, long sequenceId)
    {
        return new LogEntry(
            sequenceId,
            envelope.Timestamp,
            envelope.LogLevel,
            envelope.EventId,
            envelope.Category,
            envelope.Message,
            envelope.Exception,
            envelope.ManagedThreadId);
    }
}
