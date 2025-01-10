using Cronjure.Triggers;

namespace Cronjure;

public class JobScheduleBuilder
{
    private DateTime? _startTime;
    private string _cronExpression = null!;
    private TimeSpan _interval;
    private int _repeatCount = -1; // -1 by default to repeat forever.
    private readonly List<ITrigger> _triggers = [];
    private readonly JobMetadata _metadata = new();

    internal (JobSchedule schedule, JobMetadata metadata) Build()
    {
        return (new JobSchedule
        {
            StartTime = _startTime,
            CronExpression = _cronExpression,
            Interval = _interval,
            RepeatCount = _repeatCount,
            Triggers = _triggers,
        }, _metadata);
    }
    
    public JobScheduleBuilder StartAt(DateTime startTime)
    {
        _startTime = startTime;
        return this;
    }

    public JobScheduleBuilder WithCronSchedule(string cronExpression)
    {
        _cronExpression = cronExpression;
        return this;
    }

    public JobScheduleBuilder WithInterval(TimeSpan interval)
    {
        _interval = interval;
        return this;
    }

    public JobScheduleBuilder WithRepeatCount(int repeatCount)
    {
        _repeatCount = repeatCount;
        return this;
    }

    public JobScheduleBuilder InGroup(string group)
    {
        _metadata.Group = group ?? throw new ArgumentNullException(nameof(group));
        return this;
    }
    
    public JobScheduleBuilder WithTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            _metadata.Tags.Add(tag);
        }
        return this;
    }
    
    public JobScheduleBuilder TriggerOnFile(
        string path,
        WatcherChangeTypes changeTypes = WatcherChangeTypes.Created,
        string filter = "*.*",
        TimeSpan? debounceInterval = null,
        ILogger? logger = null)
    {
        var trigger = new FileSystemTrigger(
            path, 
            filter, 
            changeTypes,
            debounceInterval ?? TimeSpan.FromSeconds(1),
            logger
        );
        
        _triggers.Add(trigger);
        return this;
    }
    
    public JobScheduleBuilder TriggerOnEvent(
        string eventPattern,
        Func<string, object?, bool>? filter = null,
        TimeSpan? debounceInterval = null,
        ILogger? logger = null)
    {
        var trigger = new EventTrigger(
            eventPattern,
            filter,
            debounceInterval ?? TimeSpan.Zero,
            logger
        );
        
        _triggers.Add(trigger);
        return this;
    }
}