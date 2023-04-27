using System.Collections.Concurrent;
using System.Threading;

namespace Workers;

internal static class Throttler
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaphores =
        new()
        {
            ["None"] = new SemaphoreSlim(int.MaxValue)
        };

    internal static SemaphoreSlim Get(string key) => Semaphores.GetOrAdd(key, k => new SemaphoreSlim(1));
}
