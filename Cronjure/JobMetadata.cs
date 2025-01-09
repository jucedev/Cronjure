namespace Cronjure;

public class JobMetadata
{
    public string Group { get; set; } = "default";
    public HashSet<string> Tags { get; set; } = [];
}