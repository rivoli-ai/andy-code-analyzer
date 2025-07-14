using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Models;
using Microsoft.Extensions.Logging;

namespace Andy.CodeAnalyzer.Services;

/// <summary>
/// Service for watching file system changes with debouncing.
/// </summary>
public class FileWatcherService : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Channel<FileChangeEvent> _changeChannel;
    private readonly ILogger<FileWatcherService> _logger;
    private readonly SemaphoreSlim _debounceSemaphore;
    private readonly Dictionary<string, DateTime> _lastChangeTime;
    private readonly Dictionary<string, Timer> _debounceTimers;
    private readonly FileWatcherOptions _options;
    private readonly HashSet<string> _ignoredExtensions;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileWatcherService"/> class.
    /// </summary>
    /// <param name="path">The path to watch.</param>
    /// <param name="options">The watcher options.</param>
    /// <param name="logger">The logger.</param>
    public FileWatcherService(string path, FileWatcherOptions options, ILogger<FileWatcherService> logger)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _options = options;
        _logger = logger;
        _changeChannel = Channel.CreateUnbounded<FileChangeEvent>();
        _debounceSemaphore = new SemaphoreSlim(1, 1);
        _lastChangeTime = new Dictionary<string, DateTime>();
        _debounceTimers = new Dictionary<string, Timer>();
        _ignoredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".tmp", ".temp", ".cache", ".lock", ".log"
        };

        _watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.DirectoryName,
            IncludeSubdirectories = options.IncludeSubdirectories,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnError;

        _logger.LogInformation("File watcher initialized for path: {Path}", path);
    }

    /// <summary>
    /// Gets changes as they occur.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of file changes.</returns>
    public async IAsyncEnumerable<FileChangeEvent> GetChangesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var change in _changeChannel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return change;
        }
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnoreFile(e.FullPath))
            return;

        await DebounceFileChangeAsync(e.FullPath, FileChangeType.Modified);
    }

    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnoreFile(e.FullPath))
            return;

        await DebounceFileChangeAsync(e.FullPath, FileChangeType.Created);
    }

    private async void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (ShouldIgnoreFile(e.FullPath))
            return;

        await DebounceFileChangeAsync(e.FullPath, FileChangeType.Deleted);
    }

    private async void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (ShouldIgnoreFile(e.FullPath))
            return;

        var change = new FileChangeEvent
        {
            Path = e.FullPath,
            OldPath = e.OldFullPath,
            ChangeType = FileChangeType.Renamed,
            Timestamp = DateTime.UtcNow
        };

        await _changeChannel.Writer.WriteAsync(change);
        _logger.LogDebug("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        _logger.LogError(e.GetException(), "File watcher error");
    }

    private async Task DebounceFileChangeAsync(string path, FileChangeType changeType)
    {
        await _debounceSemaphore.WaitAsync();
        try
        {
            // Cancel any existing timer for this file
            if (_debounceTimers.TryGetValue(path, out var existingTimer))
            {
                await existingTimer.DisposeAsync();
                _debounceTimers.Remove(path);
            }

            // Create a new timer that will fire after the debounce delay
            var timer = new Timer(async _ =>
            {
                var change = new FileChangeEvent
                {
                    Path = path,
                    ChangeType = changeType,
                    Timestamp = DateTime.UtcNow
                };

                await _changeChannel.Writer.WriteAsync(change);
                _logger.LogDebug("File {ChangeType}: {Path}", changeType, path);

                // Clean up the timer
                await _debounceSemaphore.WaitAsync();
                try
                {
                    if (_debounceTimers.TryGetValue(path, out var t))
                    {
                        _debounceTimers.Remove(path);
                        await t.DisposeAsync();
                    }
                }
                finally
                {
                    _debounceSemaphore.Release();
                }
            }, null, _options.DebounceDelay, Timeout.InfiniteTimeSpan);

            _debounceTimers[path] = timer;
        }
        finally
        {
            _debounceSemaphore.Release();
        }
    }

    private bool ShouldIgnoreFile(string path)
    {
        // Check file extension
        var extension = Path.GetExtension(path);
        if (_ignoredExtensions.Contains(extension))
            return true;

        // Check ignore patterns
        return _options.IgnorePatterns.Any(pattern =>
            MatchesPattern(path, pattern));
    }

    private static bool MatchesPattern(string path, string pattern)
    {
        // Simple glob pattern matching
        // Convert pattern to regex
        var regexPattern = pattern
            .Replace("\\", "/")
            .Replace(".", "\\.")
            .Replace("**/", ".*")
            .Replace("*", "[^/]*")
            .Replace("?", ".");

        return System.Text.RegularExpressions.Regex.IsMatch(
            path.Replace("\\", "/"),
            $"^{regexPattern}$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Disposes of the file watcher.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();

        _changeChannel.Writer.TryComplete();

        // Dispose all timers
        foreach (var timer in _debounceTimers.Values)
        {
            timer.Dispose();
        }
        _debounceTimers.Clear();

        _debounceSemaphore.Dispose();

        _logger.LogInformation("File watcher disposed");
    }
}