using System.Collections.Concurrent;

namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// How often who sent a request asked for something of a kind in the last hour – a window that slides with the
/// clock, so that no moment of the clock opens it again. A window that was emptied at the full hour let somebody
/// ask twice as often around it, and a test that ran across the full hour fail.
/// <para>
/// It lives in the memory of the process and holds the moments of the requests of the last hour, no more of
/// them than are allowed, and nothing else: who sent them is the key, and a key whose hour is over is forgotten.
/// Nothing of it is written anywhere.
/// </para>
/// </summary>
/// <param name="time">The clock.</param>
public sealed class RequestWindow(TimeProvider time)
{
    /// <summary>The length of the window.</summary>
    public static readonly TimeSpan Length = TimeSpan.FromHours(1);

    private readonly TimeProvider _time = time ?? throw new ArgumentNullException(nameof(time));
    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _asked = new(StringComparer.Ordinal);
    private long _sweptAt;

    /// <summary>
    /// Counts a request under a key and returns whether it is still within what the last hour allows. A request
    /// that is refused is not counted: who keeps asking is let in again as soon as the oldest request of the
    /// hour is an hour old.
    /// </summary>
    /// <param name="key">What is counted and who asked, for example the kind of request and the sender.</param>
    /// <param name="most">How many requests the hour allows.</param>
    public bool Allows(string key, int most)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNegative(most);
        var now = _time.GetUtcNow();
        var since = now - Length;
        Sweep(now, since);
        while (true)
        {
            var asked = _asked.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
            lock (asked)
            {
                // A queue the sweep took out of the dictionary meanwhile counts for nobody: ask for the one that is there.
                if (!_asked.TryGetValue(key, out var current) || !ReferenceEquals(current, asked))
                {
                    continue;
                }

                while (asked.Count > 0 && asked.Peek() <= since)
                {
                    asked.Dequeue();
                }

                if (asked.Count >= most)
                {
                    return false;
                }

                asked.Enqueue(now);
                return true;
            }
        }
    }

    /// <summary>Gets how many keys are remembered – for the tests, which see that what is over is forgotten.</summary>
    internal int Remembered => _asked.Count;

    // Forgets the keys whose last request is an hour old, at most once in a window.
    private void Sweep(DateTimeOffset now, DateTimeOffset since)
    {
        var last = Interlocked.Read(ref _sweptAt);
        if (last > since.UtcTicks || Interlocked.CompareExchange(ref _sweptAt, now.UtcTicks, last) != last)
        {
            return;
        }

        foreach (var (key, asked) in _asked)
        {
            lock (asked)
            {
                if (asked.Count == 0 || asked.Last() <= since)
                {
                    _asked.TryRemove(KeyValuePair.Create(key, asked));
                }
            }
        }
    }
}
