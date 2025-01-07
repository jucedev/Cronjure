namespace Cronjure;

public interface IScheduler
{
    Task ScheduleJob<T>(Action<JobScheduleBuilder> scheduleBuilder, Dictionary<string, object>? jobData = null!) where T : IJob;
    Task StartAsync();
    Task StopAsync();
}