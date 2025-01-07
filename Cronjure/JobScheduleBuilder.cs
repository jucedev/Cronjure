namespace Cronjure;

public class JobScheduleBuilder
{
    private DateTime? _startTime;
    private string _cronExpression = null!;
    private TimeSpan _interval;
    private int _repeatCount = -1; // -1 by default to repeat forever.

    internal JobSchedule Build()
    {
        return new JobSchedule
        {
            StartTime = _startTime,
            CronExpression = _cronExpression,
            Interval = _interval,
            RepeatCount = _repeatCount,
        };
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
}