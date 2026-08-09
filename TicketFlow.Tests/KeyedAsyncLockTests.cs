using System.Collections;
using System.Reflection;
using TicketFlow.Application.Concurrency;

namespace TicketFlow.Tests
{
    public class KeyedAsyncLockTests
    {
        [Fact]
        public async Task AcquireAsync_ShouldSerializeAccess_ForSameKey()
        {
            var keyedLock = new KeyedAsyncLock();
            var key = Guid.NewGuid();
            var sync = new object();
            var concurrentEntries = 0;
            var maxConcurrentEntries = 0;

            var tasks = Enumerable.Range(0, 20).Select(async _ =>
            {
                await using var handle = await keyedLock.AcquireAsync(key);

                lock (sync)
                {
                    concurrentEntries++;
                    maxConcurrentEntries = Math.Max(maxConcurrentEntries, concurrentEntries);
                }

                await Task.Delay(10);

                lock (sync)
                {
                    concurrentEntries--;
                }
            });

            await Task.WhenAll(tasks);

            Assert.Equal(1, maxConcurrentEntries);
        }

        [Fact]
        public async Task AcquireAsync_ShouldNotBlock_ForDifferentKeys()
        {
            var keyedLock = new KeyedAsyncLock();

            var handleA = await keyedLock.AcquireAsync(Guid.NewGuid());
            try
            {
                var acquireBTask = keyedLock.AcquireAsync(Guid.NewGuid());

                var winner = await Task.WhenAny(acquireBTask, Task.Delay(TimeSpan.FromSeconds(2)));

                Assert.Same(acquireBTask, winner);

                await (await acquireBTask).DisposeAsync();
            }
            finally
            {
                await handleA.DisposeAsync();
            }
        }

        [Fact]
        public async Task AcquireAsync_ShouldRemoveEntryFromDictionary_AfterLastReleaseAsync()
        {
            var keyedLock = new KeyedAsyncLock();
            var key = Guid.NewGuid();

            await using (await keyedLock.AcquireAsync(key))
            {
                // Held and released immediately — should be the only reference to this key.
            }

            var entriesField = typeof(KeyedAsyncLock)
                .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var entries = (ICollection)entriesField.GetValue(keyedLock)!;

            Assert.Empty(entries);
        }
    }
}
