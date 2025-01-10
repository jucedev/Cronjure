namespace Cronjure.Triggers;

public abstract class DebouncedTrigger : ITrigger
{
    private TimeSpan DebounceInterval { get; }
    protected CancellationTokenSource? DebounceCts;
    protected Func<Task> Callback = null!;

    protected DebouncedTrigger(TimeSpan debounceInterval)
    {
        DebounceInterval = debounceInterval;
    }

    protected async Task DebouncedExecute()
    {
        try
        {
            if (DebounceCts != null)
            {
                await DebounceCts.CancelAsync();
            }
            
            DebounceCts = new CancellationTokenSource();
            var token = DebounceCts.Token;

            await Task.Delay(DebounceInterval, token);
            if (!token.IsCancellationRequested)
            {
                await Callback();
            }
        }
        catch (OperationCanceledException) { }
    }

    public abstract Task Start(Func<Task> callback);
    public abstract Task Stop();
}