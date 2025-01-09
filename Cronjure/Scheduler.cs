using System.Collections.Concurrent;

namespace Cronjure;

public class Scheduler : IScheduler
{
    public class ScheduledJob
    {
        public required Type JobType { get; init; }
        public required JobSchedule Schedule { get; init; }
        public IDictionary<string, object>? JobData { get; init; }
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
        public int RemainingRuns { get; set; }
        public JobMetadata Metadata { get; init; } = new();
    }
    
    private readonly ILogger<Scheduler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Guid, ScheduledJob> _jobs;
    private readonly CancellationTokenSource _shutdownToken;
    private Task? _schedulerTask;
    
    // Group and Tag indexes for quick lookups
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _groupIndex = new();
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _tagIndex = new();

    public Scheduler(ILogger<Scheduler> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _jobs = new ConcurrentDictionary<Guid, ScheduledJob>();
        _shutdownToken = new CancellationTokenSource();
    }
    
    public Task ScheduleJob<T>(Action<JobScheduleBuilder> scheduleBuilder, Dictionary<string, object>? jobData = null) 
        where T : IJob
    {
        var builder = new JobScheduleBuilder();
        scheduleBuilder(builder);
        
        var (schedule, metadata) = builder.Build();
        
        var jobKey = Guid.NewGuid();
        var scheduledJob = new ScheduledJob
        {
            JobType = typeof(T),
            Schedule = schedule,
            JobData = jobData ?? new Dictionary<string, object>(),
            NextRunTime = schedule.StartTime,
            RemainingRuns = schedule.RepeatCount,
            Metadata = metadata,
        };
        
        _jobs.TryAdd(jobKey, scheduledJob);
        
        UpdateJobIndexes(jobKey, scheduledJob);
        
        _logger.LogInformation($"Cronjure: Scheduled job of type {typeof(T).Name} in group {metadata.Group} with tags {string.Join(", ", metadata.Tags)}");

        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        _schedulerTask = RunSchedulerLoop();
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        await _shutdownToken.CancelAsync();
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
                _ = ExecuteJobAsync(job.Value);
            }
        }

        // Wait a short delay before checking for more jobs.
        await Task.Delay(TimeSpan.FromSeconds(1), _shutdownToken.Token);
    }

    private async Task ExecuteJobAsync(ScheduledJob scheduledJob)
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
            
            scheduledJob.LastRunTime = DateTime.UtcNow;

            UpdateNextRunTime(scheduledJob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cronjure: Failed to execute job of type {}", scheduledJob.JobType.Name);
        }
    }

    private static void UpdateNextRunTime(ScheduledJob job)
    {
        var lastRunTime = job.LastRunTime ?? DateTime.UtcNow;
        
        if (job.Schedule.CronExpression != null)
        {
            var expression = CronExpression.Parse(job.Schedule.CronExpression);
            job.NextRunTime = expression.GetNextOccurrence(lastRunTime);
        }
        else if (job.Schedule.Interval.HasValue)
        {
            job.NextRunTime = lastRunTime.Add(job.Schedule.Interval.Value);
            if (job.RemainingRuns <= 0) return;

            job.RemainingRuns--;
            
            if (job.RemainingRuns == 0)
            {
                // Job has finished executing.
                job.NextRunTime = null;
            }
        }
    }
    
    private void UpdateJobIndexes(Guid key, ScheduledJob job)
    {
        _groupIndex.AddOrUpdate(
            job.Metadata.Group,
            [key],
            (_, existing) =>
            {
                existing.Add(key);
                return existing;
            });

        foreach (var tag in job.Metadata.Tags)
        {
            _tagIndex.AddOrUpdate(
                tag,
                [key],
                (_, existing) =>
                {
                    existing.Add(key);
                    return existing;
                });
        }
    }

    public Task PauseGroup(string group)
    {
        if (!_groupIndex.TryGetValue(group, out var groupJobs))
        {
            return Task.CompletedTask;
        }
        
        foreach (var key in groupJobs)
        {
            if (_jobs.TryGetValue(key, out var job))
            {
                job.NextRunTime = null;
            }
        }

        return Task.CompletedTask;
    }

    public Task ResumeGroup(string group)
    {
        if (!_groupIndex.TryGetValue(group, out var groupJobs))
        {
            return Task.CompletedTask;
        }
        
        foreach (var key in groupJobs)
        {
            if (_jobs.TryGetValue(key, out var job))
            {
                UpdateNextRunTime(job);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<ScheduledJob>> GetJobsByGroup(string group)
    {
        return Task.FromResult(_jobs.Values.Where(j => j.Metadata.Group == group));
    }

    public Task<IEnumerable<ScheduledJob>> GetJobsByTag(string tag)
    {
        if (!_tagIndex.TryGetValue(tag, out var taggedJobs))
        {
            return Task.FromResult(Enumerable.Empty<ScheduledJob>());
        }

        return Task.FromResult(taggedJobs
            .Select(id => _jobs.GetValueOrDefault(id))
            .Where(job => job != null))!;
    }
}