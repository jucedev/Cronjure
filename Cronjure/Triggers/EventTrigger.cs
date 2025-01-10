namespace Cronjure.Triggers;

public class EventTrigger : DebouncedTrigger
{
    private string EventPattern { get; }
    private readonly Func<string, object?, bool>? _filter;
    private readonly ILogger? _logger;
    
    public EventTrigger(
        string eventPattern, 
        Func<string, object?, bool>? filter, 
        TimeSpan debounceInterval = default, 
        ILogger? logger = null) : base(debounceInterval)
    {
        EventPattern = eventPattern;
        _filter = filter;
        _logger = logger;
    }
    
    public override Task Start(Func<Task> callback)
    {
        Callback = callback;
        EventHub.Subscribe(EventPattern, OnEventRaised);
        return Task.CompletedTask;
    }

    private async void OnEventRaised(string eventName, object? eventData)
    {
        try
        {
            if (_filter?.Invoke(eventName, eventData) ?? true)
            {
                await DebouncedExecute();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error handling event: {ex.Message}");
        }
    }

    public override Task Stop()
    {
        EventHub.Unsubscribe(EventPattern, OnEventRaised);
        DebounceCts?.Cancel();
        return Task.CompletedTask;
    }
}