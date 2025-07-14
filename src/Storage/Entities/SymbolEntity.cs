namespace Andy.CodeAnalyzer.Storage.Entities;

/// <summary>
/// Entity representing a code symbol.
/// </summary>
public class SymbolEntity
{
    /// <summary>
    /// Gets or sets the symbol ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the file ID.
    /// </summary>
    public int FileId { get; set; }

    /// <summary>
    /// Gets or sets the file containing this symbol.
    /// </summary>
    public FileEntity File { get; set; } = null!;

    /// <summary>
    /// Gets or sets the symbol name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start line.
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Gets or sets the start column.
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// Gets or sets the end line.
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Gets or sets the end column.
    /// </summary>
    public int EndColumn { get; set; }

    /// <summary>
    /// Gets or sets the parent symbol ID.
    /// </summary>
    public int? ParentSymbolId { get; set; }

    /// <summary>
    /// Gets or sets the parent symbol.
    /// </summary>
    public SymbolEntity? ParentSymbol { get; set; }

    /// <summary>
    /// Gets or sets the documentation.
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// Gets or sets the metadata as JSON.
    /// </summary>
    public string? Metadata { get; set; }
}