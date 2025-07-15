using System;
using System.Collections.Generic;

namespace Andy.CodeAnalyzer.Models;

/// <summary>
/// Represents the analyzed structure of a code file.
/// </summary>
public class CodeStructure
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the programming language.
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of symbols found in the file.
    /// </summary>
    public List<Symbol> Symbols { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of imports/using statements.
    /// </summary>
    public List<Import> Imports { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of exports.
    /// </summary>
    public List<Export> Exports { get; set; } = new();

    /// <summary>
    /// Gets or sets additional metadata about the file.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the last time this structure was analyzed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; }
}

/// <summary>
/// Represents a code symbol (class, method, property, etc.).
/// </summary>
public class Symbol
{
    /// <summary>
    /// Gets or sets the symbol name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol kind.
    /// </summary>
    public SymbolKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the symbol location in the file.
    /// </summary>
    public Location Location { get; set; } = new();

    /// <summary>
    /// Gets or sets the parent symbol name (for nested symbols).
    /// </summary>
    public string? ParentSymbol { get; set; }

    /// <summary>
    /// Gets or sets the modifiers (public, private, static, etc.).
    /// </summary>
    public List<string> Modifiers { get; set; } = new();

    /// <summary>
    /// Gets or sets the documentation/comments for this symbol.
    /// </summary>
    public string? Documentation { get; set; }
}

/// <summary>
/// Types of code symbols.
/// </summary>
public enum SymbolKind
{
    /// <summary>
    /// A class definition.
    /// </summary>
    Class,

    /// <summary>
    /// An interface definition.
    /// </summary>
    Interface,

    /// <summary>
    /// A method or function.
    /// </summary>
    Method,

    /// <summary>
    /// A property.
    /// </summary>
    Property,

    /// <summary>
    /// A field or variable.
    /// </summary>
    Field,

    /// <summary>
    /// An enum definition.
    /// </summary>
    Enum,

    /// <summary>
    /// A namespace or module.
    /// </summary>
    Namespace,

    /// <summary>
    /// A struct definition.
    /// </summary>
    Struct,

    /// <summary>
    /// A function (for functional languages).
    /// </summary>
    Function,

    /// <summary>
    /// A constant value.
    /// </summary>
    Constant
}

/// <summary>
/// Represents a location in a source file.
/// </summary>
public class Location
{
    /// <summary>
    /// Gets or sets the starting line number (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// Gets or sets the starting column number (1-based).
    /// </summary>
    public int StartColumn { get; set; }

    /// <summary>
    /// Gets or sets the ending line number (1-based).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Gets or sets the ending column number (1-based).
    /// </summary>
    public int EndColumn { get; set; }
}

/// <summary>
/// Represents an import statement.
/// </summary>
public class Import
{
    /// <summary>
    /// Gets or sets the imported name or path.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alias (if any).
    /// </summary>
    public string? Alias { get; set; }
}

/// <summary>
/// Represents an export statement.
/// </summary>
public class Export
{
    /// <summary>
    /// Gets or sets the exported name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is a default export.
    /// </summary>
    public bool IsDefault { get; set; }
}