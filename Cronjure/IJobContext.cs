namespace Cronjure;

public interface IJobContext
{
    CancellationToken CancellationToken { get; set; }
    IDictionary<string, object>? Data { get; set; }
}