using System.Collections.Concurrent;

namespace Cronjure.Triggers;

public class AndTrigger : ITrigger
{
    private readonly ITrigger[] _triggers;
    private readonly ConcurrentDictionary<ITrigger, bool> _triggerStates = new();
    private Func<Task> _callback = null!;

    public AndTrigger(params ITrigger[] triggers)
    {
        _triggers = triggers;
    }

    public async Task Start(Func<Task> callback)
    {
        _callback = callback;

        foreach (var trigger in _triggers)
        {
            _triggerStates[trigger] = false;
            await trigger.Start(async () => await OnTriggerActivated(trigger));
        }
    }

    private async Task OnTriggerActivated(ITrigger trigger)
    {
        _triggerStates[trigger] = true;
        if (_triggerStates.All(x => x.Value))
        {
            await _callback();

            // Reset states after all triggers have fired
            foreach (var key in _triggerStates.Keys)
            {
                _triggerStates[key] = false;
            }
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