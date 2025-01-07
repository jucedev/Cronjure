namespace Cronjure;

public interface IJob
{
    Task Execute(IJobContext context);
}