using System.Collections.Concurrent;

namespace Cronjure;

public class Scheduler : IScheduler
{
    private class ScheduledJob
    {
        public required Type JobType { get; set; }
        public required JobSchedule Schedule { get; set; }
        public IDictionary<string, object>? JobData { get; set; }
        public DateTime? NextRunTime { get; set; }
        public int RemainingRuns { get; set; }
    }
    
    private readonly ILogger<Scheduler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Guid, ScheduledJob> _jobs;
    private readonly CancellationTokenSource _shutdownToken;
    private Task? _schedulerTask;

    public Scheduler(ILogger<Scheduler> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _jobs = new ConcurrentDictionary<Guid, ScheduledJob>();
        _shutdownToken = new CancellationTokenSource();
    }
    
    public Task ScheduleJob<T>(Action<JobScheduleBuilder> scheduleBuilder, Dictionary<string, object>? jobData = null) where T : IJob
    {
        var builder = new JobScheduleBuilder();
        scheduleBuilder(builder);
        
        var schedule = builder.Build();
        
        var jobKey = Guid.NewGuid();
        var scheduledJob = new ScheduledJob
        {
            JobType = typeof(T),
            Schedule = schedule,
            JobData = jobData ?? new Dictionary<string, object>(),
            NextRunTime = schedule.StartTime,
            RemainingRuns = schedule.RepeatCount,
        };
        
        _jobs.TryAdd(jobKey, scheduledJob);
        _logger.LogInformation("Cronjure: Scheduled job of type {jobType}", typeof(T).Name);

        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        _schedulerTask = RunSchedulerLoop();
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _shutdownToken.Cancel();
        if (_schedulerTask != null)
        {
            await _schedulerTask;
        }
    }

    private async Task RunSchedulerLoop()
    {
        while (!_shutdownToken.IsCancellationRequested)
        {
            var jobsToRun = _jobs.Where(j => j.Value.NextRunTime <= DateTime.UtcNow).ToList();

            foreach (var job in jobsToRun)
            {
                // Execute job in the background
                _ = ExecuteJobAsync(job.Key, job.Value);
            }
        }

        // Wait a short delay before checking for more jobs.
        await Task.Delay(TimeSpan.FromSeconds(1), _shutdownToken.Token);
    }

    private async Task ExecuteJobAsync(Guid jobKey, ScheduledJob scheduledJob)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var job = (IJob)ActivatorUtilities.CreateInstance(scope.ServiceProvider, scheduledJob.JobType);

            var context = new JobContext
            {
                CancellationToken = _shutdownToken.Token,
                Data = scheduledJob.JobData,
            };

            await job.Execute(context);

            UpdateNextRunTime(scheduledJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cronjure: Failed to execute job of type {}", scheduledJob.JobType.Name);
        }
    }

    private void UpdateNextRunTime(ScheduledJob job)
    {
        if (job.Schedule.CronExpression != null)
        {
            var expression = CronExpression.Parse(job.Schedule.CronExpression);
            job.NextRunTime = expression.GetNextOccurrence(DateTime.UtcNow);
        }
        else if (job.Schedule.Interval.HasValue)
        {
            job.NextRunTime = DateTime.UtcNow.Add(job.Schedule.Interval.Value);
            if (job.RemainingRuns <= 0) return;

            job.RemainingRuns--;
            
            if (job.RemainingRuns == 0)
            {
                // Job has finished executing.
                job.NextRunTime = null;
            }
        }
    }
}