using System;

namespace Andy.CodeAnalyzer.Models;

/// <summary>
/// Represents a file system change event.
/// </summary>
public class FileChangeEvent
{
    /// <summary>
    /// Gets or sets the file path that changed.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of change.
    /// </summary>
    public FileChangeType ChangeType { get; set; }

    /// <summary>
    /// Gets or sets when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the old path (for rename operations).
    /// </summary>
    public string? OldPath { get; set; }
}

/// <summary>
/// Types of file changes.
/// </summary>
public enum FileChangeType
{
    /// <summary>
    /// File was created.
    /// </summary>
    Created,

    /// <summary>
    /// File was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// File was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// File was renamed.
    /// </summary>
    Renamed
}