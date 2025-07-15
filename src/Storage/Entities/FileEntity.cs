using System;
using System.Collections.Generic;

namespace Andy.CodeAnalyzer.Storage.Entities;

/// <summary>
/// Entity representing an indexed file.
/// </summary>
public class FileEntity
{
    /// <summary>
    /// Gets or sets the file ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content hash.
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the last modified time.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Gets or sets the file size.
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets when the file was indexed.
    /// </summary>
    public DateTime IndexedAt { get; set; }

    /// <summary>
    /// Gets or sets the symbols in this file.
    /// </summary>
    public List<SymbolEntity> Symbols { get; set; } = new();

    /// <summary>
    /// Gets or sets the outgoing dependencies.
    /// </summary>
    public List<DependencyEntity> OutgoingDependencies { get; set; } = new();

    /// <summary>
    /// Gets or sets the incoming dependencies.
    /// </summary>
    public List<DependencyEntity> IncomingDependencies { get; set; } = new();
}