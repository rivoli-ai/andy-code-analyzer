namespace Andy.CodeAnalyzer.Storage.Entities;

/// <summary>
/// Entity representing a file dependency.
/// </summary>
public class DependencyEntity
{
    /// <summary>
    /// Gets or sets the dependency ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the source file ID.
    /// </summary>
    public int FromFileId { get; set; }

    /// <summary>
    /// Gets or sets the source file.
    /// </summary>
    public FileEntity FromFile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the target file ID.
    /// </summary>
    public int ToFileId { get; set; }

    /// <summary>
    /// Gets or sets the target file.
    /// </summary>
    public FileEntity ToFile { get; set; } = null!;

    /// <summary>
    /// Gets or sets the dependency type (import, include, etc.).
    /// </summary>
    public string DependencyType { get; set; } = string.Empty;
}