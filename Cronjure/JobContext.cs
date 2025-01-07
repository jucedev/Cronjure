namespace Cronjure;

public interface IJobContext
{
    CancellationToken CancellationToken { get; set; }
    IDictionary<string, object>? Data { get; set; }
}

public class JobContext : IJobContext
{
    public CancellationToken CancellationToken { get; set; }
    public IDictionary<string, object>? Data { get; set; }
}