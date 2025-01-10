namespace Cronjure.Triggers;

public interface ITrigger
{
    Task Start(Func<Task> callback);
    Task Stop();
}