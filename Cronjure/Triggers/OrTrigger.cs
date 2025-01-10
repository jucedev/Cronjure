namespace Cronjure.Triggers;

public class OrTrigger : ITrigger
{
    private readonly ITrigger[] _triggers;
    private Func<Task> _callback = null!;

    public OrTrigger(params ITrigger[] triggers)
    {
        _triggers = triggers;
    }


    public async Task Start(Func<Task> callback)
    {
        _callback = callback;
        foreach (var trigger in _triggers)
        {
            await trigger.Start(async () => await _callback());
        }
    }

    public async Task Stop()
    {
        foreach (var trigger in _triggers)
        {
            await trigger.Stop();
        }
    }
}