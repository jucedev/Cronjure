using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Cronjure.Triggers;

public static class EventHub
{
    private static readonly ConcurrentDictionary<string, List<Action<string, object?>>> Handlers = new();
    
    public static void Subscribe(string eventPattern, Action<string, object?> handler)
    {
        var pattern = new Regex(WildcardToRegex(eventPattern));

        foreach (var key in Handlers.Keys.Where(key => pattern.IsMatch(key)))
        {
            Handlers.AddOrUpdate(
                key,
                [handler],
                (_, list) =>
                {
                    list.Add(handler);
                    return list;
                });
        }
    }

    public static void Unsubscribe(string eventPattern, Action<string, object?> handler)
    {
        var pattern = new Regex(WildcardToRegex(eventPattern));

        foreach (var key in Handlers.Keys.Where(key => pattern.IsMatch(key)))
        {
            if (Handlers.TryGetValue(key, out var handlers))
            {
                handlers.Remove(handler);
            }
        }
    }

    public static void Raise(string eventName, object? eventData = null)
    {
        if (!Handlers.TryGetValue(eventName, out var handlers)) return;
        
        foreach (var handler in handlers)
        {
            handler(eventName, eventData);
        }
    }

    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".");
    }
}