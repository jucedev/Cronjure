using System.Collections.Concurrent;

namespace Cronjure.Triggers;

public class FileSystemTrigger : DebouncedTrigger
{
    public string Path { get; }
    public string Filter { get; }
    public WatcherChangeTypes ChangeTypes { get; }
    private FileSystemWatcher? _watcher = null!;
    private readonly ILogger? _logger;
    private readonly ConcurrentDictionary<string, DateTime> _files = new();

    public FileSystemTrigger(
        string path, 
        string filter, 
        WatcherChangeTypes changeTypes, 
        TimeSpan debounceInterval = default, 
        ILogger? logger = null) : base(debounceInterval)
    {
        Path = path;
        Filter = filter;
        ChangeTypes = changeTypes;
        _logger = logger;
    }
    
    public override Task Start(Func<Task> callback)
    {
        Callback = callback;

        try
        {
            _watcher = new FileSystemWatcher
            {
                Path = Path, 
                Filter = Filter,
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName, 
            };

            if (ChangeTypes.HasFlag(WatcherChangeTypes.Changed))
                _watcher.Created += OnChanged;
            if (ChangeTypes.HasFlag(WatcherChangeTypes.Changed))
                _watcher.Changed += OnChanged;
            if (ChangeTypes.HasFlag(WatcherChangeTypes.Deleted))
                _watcher.Deleted += OnChanged;
            if (ChangeTypes.HasFlag(WatcherChangeTypes.Renamed))
                _watcher.Renamed += OnRenamed;

            _watcher.Error += OnError;

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to start file system trigger for path: {Path}");
            throw;
        }
    }
    
    private async void OnChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Prevent duplicate processing
            if (!_files.TryAdd(e.FullPath, DateTime.UtcNow))
                return;

            // Clean up old entries
            CleanupProcessedFiles();

            await DebouncedExecute();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error handling file change for: {e.FullPath}");
        }
    }
    
    private async void OnRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            // Prevent duplicate processing for renamed files
            if (!_files.TryAdd(e.FullPath, DateTime.UtcNow))
                return;
            
            // Remove the old path
            if (_files.ContainsKey(e.OldFullPath))
            {
                _files.TryRemove(e.OldFullPath, out _); 
            }

            // Clean up old entries
            CleanupProcessedFiles();

            await DebouncedExecute();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Error handling file rename for: {e.OldFullPath} -> {e.FullPath}");
        }
    }
    
    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger?.LogError(e.GetException(), "FileSystemWatcher error");
    }

    private void CleanupProcessedFiles()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);

        foreach (var file in _files.Where(x => x.Value < cutoff).ToList())
        {
            _files.TryRemove(file.Key, out _);
        }
    }

    public override Task Stop()
    {
        _watcher?.Dispose();
        _watcher = null;
        DebounceCts.Cancel();
        return Task.CompletedTask;
    }
}