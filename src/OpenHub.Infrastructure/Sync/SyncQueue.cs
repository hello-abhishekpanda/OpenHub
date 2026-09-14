using System.Threading.Channels;
using OpenHub.Application.Abstractions;

namespace OpenHub.Infrastructure.Sync;

public sealed class SyncQueue : ISyncQueue
{
    private readonly Channel<string> _queue = Channel.CreateBounded<string>(
        new BoundedChannelOptions(500) { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = false, SingleWriter = false });

    public async ValueTask QueueAsync(string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100)
            throw new ArgumentException("A valid GitHub username is required.", nameof(username));
        if (!await _queue.Writer.WaitToWriteAsync(cancellationToken) || !_queue.Writer.TryWrite(username.Trim()))
            throw new InvalidOperationException("The sync queue is at capacity. Try again shortly.");
    }

    public ValueTask<string> DequeueAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAsync(cancellationToken);
}
