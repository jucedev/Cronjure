namespace Cronjure;

public class JobSchedule
{
    public DateTime? StartTime { get; set; }
    public string CronExpression { get; set; } = null!;
    public TimeSpan Interval { get; set; }
    public int RepeatCount { get; set; }
}