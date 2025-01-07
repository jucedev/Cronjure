# Cronjure

A simple job scheduling library. 

# Usage

1. Register the scheduler
```c#
builder.Services.AddHostedService<Scheduler>();
builder.Services.AddSingleton<IScheduler>(sp => sp.GetRequiredService<IHostedService>() as Scheduler);
```

2. Create your job
```c#
public interface IEmailService
{
    Task SendReminder(string recipient);
}

public class EmailReminderJob(IEmailService emailService) : IJob
{
    private readonly IEmailService _emailService;

    public async Task Execute(IJobContext context)
    {
        var recipient = context.Data["recipient"].ToString();
        await _emailService.SendReminder(recipient);
    }
}
```

3. Schedule the job
```c#
var data = new Dictionary<string, object> { ["recipient"] = "reminder@example.com" };

// Using cron
await scheduler.ScheduleJob<EmailReminderJob>(schedule => 
    schedule.WithCronSchedule("0 0 9 * * ?"), // Run every day at 9 AM
    data
);

// Using TimeSpan - passing data is optional
await scheduler.ScheduleJob<EmailReminderJob>(schedule => 
     schedule.WithInterval(TimeSpan.FromMinutes(5)) // Run every 5 minutes
);
```