using Cronjure.Triggers;

namespace Cronjure;

public class JobSchedule
{
    public DateTime? StartTime { get; init; }
    public string? CronExpression { get; init; }
    public TimeSpan? Interval { get; init; }
    public int RepeatCount { get; init; }
    
    public List<ITrigger> Triggers { get; init; } = [];
    public bool HasTriggers => Triggers.Any();
}