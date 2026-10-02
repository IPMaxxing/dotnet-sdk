using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace IPMax;

internal sealed class LruCache(int capacity, TimeSpan ttl)
{
    private readonly Dictionary<(string Product, string Ip), LinkedListNode<Entry>> _entries = [];
    private readonly LinkedList<Entry> _order = new();
    private readonly object _gate = new();

    public bool TryGet<T>(string product, string ip, [NotNullWhen(true)] out T? value)
        where T : class
    {
        var key = (product, ip);
        lock (_gate)
        {
            if (_entries.TryGetValue(key, out var node))
            {
                if (Stopwatch.GetElapsedTime(node.Value.StoredAt) < ttl)
                {
                    _order.Remove(node);
                    _order.AddFirst(node);
                    value = (T)node.Value.Value;
                    return true;
                }

                _order.Remove(node);
                _entries.Remove(key);
            }
        }

        value = null;
        return false;
    }

    public void Set(string product, string ip, object value)
    {
        var key = (product, ip);
        var entry = new Entry(key, value, Stopwatch.GetTimestamp());
        lock (_gate)
        {
            if (_entries.Remove(key, out var existing))
            {
                _order.Remove(existing);
            }

            _entries[key] = _order.AddFirst(entry);
            if (_entries.Count > capacity)
            {
                _entries.Remove(_order.Last!.Value.Key);
                _order.RemoveLast();
            }
        }
    }

    private readonly record struct Entry((string Product, string Ip) Key, object Value, long StoredAt);
}
