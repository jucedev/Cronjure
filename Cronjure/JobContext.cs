namespace Cronjure;

public class JobContext : IJobContext
{
    public CancellationToken CancellationToken { get; set; }
    public IDictionary<string, object>? Data { get; set; }
}