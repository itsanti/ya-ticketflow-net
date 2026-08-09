using System.Collections.Concurrent;

namespace TicketFlow.Application.Concurrency
{
    public sealed class KeyedAsyncLock
    {
        private sealed class Entry
        {
            public readonly SemaphoreSlim Semaphore = new(1, 1);
            public int RefCount;
        }

        private readonly object _sync = new();
        private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

        public async Task<IAsyncDisposable> AcquireAsync(Guid key)
        {
            Entry entry;

            lock (_sync)
            {
                entry = _entries.AddOrUpdate(
                    key,
                    static _ => new Entry { RefCount = 1 },
                    static (_, existing) =>
                    {
                        existing.RefCount++;
                        return existing;
                    });
            }

            await entry.Semaphore.WaitAsync();

            return new Releaser(() => Release(key, entry));
        }

        private void Release(Guid key, Entry entry)
        {
            entry.Semaphore.Release();

            lock (_sync)
            {
                entry.RefCount--;

                if (entry.RefCount == 0)
                {
                    _entries.TryRemove(key, out _);
                }
            }
        }

        private sealed class Releaser(Action release) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                release();
                return ValueTask.CompletedTask;
            }
        }
    }
}
