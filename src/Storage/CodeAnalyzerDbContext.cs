using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Andy.CodeAnalyzer.Storage.Entities;

namespace Andy.CodeAnalyzer.Storage;

/// <summary>
/// Entity Framework database context for code analyzer.
/// </summary>
public class CodeAnalyzerDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeAnalyzerDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    public CodeAnalyzerDbContext(DbContextOptions<CodeAnalyzerDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the files.
    /// </summary>
    public DbSet<FileEntity> Files { get; set; } = null!;

    /// <summary>
    /// Gets or sets the symbols.
    /// </summary>
    public DbSet<SymbolEntity> Symbols { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file dependencies.
    /// </summary>
    public DbSet<DependencyEntity> Dependencies { get; set; } = null!;

    /// <summary>
    /// Configures the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure File entity
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.LastModified);

            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.Language).IsRequired();
            entity.Property(e => e.ContentHash).IsRequired();
        });

        // Configure Symbol entity
        modelBuilder.Entity<SymbolEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FileId, e.Name });
            entity.HasIndex(e => e.Kind);
            entity.HasIndex(e => e.Name);

            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Kind).IsRequired();

            entity.HasOne(e => e.File)
                .WithMany(f => f.Symbols)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentSymbol)
                .WithMany()
                .HasForeignKey(e => e.ParentSymbolId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Dependency entity
        modelBuilder.Entity<DependencyEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FromFileId, e.ToFileId }).IsUnique();

            entity.Property(e => e.DependencyType).IsRequired();

            entity.HasOne(e => e.FromFile)
                .WithMany(f => f.OutgoingDependencies)
                .HasForeignKey(e => e.FromFileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ToFile)
                .WithMany(f => f.IncomingDependencies)
                .HasForeignKey(e => e.ToFileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Creates the FTS5 tables for full-text search.
    /// </summary>
    /// <remarks>
    /// This must be called after the database is created since EF Core doesn't support
    /// creating virtual tables through migrations.
    /// </remarks>
    public async Task CreateFtsTablesAsync()
    {
        await Database.ExecuteSqlRawAsync(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS file_content USING fts5(
                file_id UNINDEXED,
                content,
                tokenize = 'porter unicode61'
            );
        ");

        await Database.ExecuteSqlRawAsync(@"
            CREATE VIRTUAL TABLE IF NOT EXISTS symbol_search USING fts5(
                symbol_id UNINDEXED,
                name,
                documentation,
                tokenize = 'porter unicode61'
            );
        ");

        // Create triggers to keep FTS tables in sync
        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS file_content_insert 
            AFTER INSERT ON Files 
            BEGIN
                INSERT INTO file_content(file_id, content) 
                VALUES (new.Id, '');
            END;
        ");

        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS file_content_delete 
            AFTER DELETE ON Files 
            BEGIN
                DELETE FROM file_content WHERE file_id = old.Id;
            END;
        ");

        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS symbol_search_insert 
            AFTER INSERT ON Symbols 
            BEGIN
                INSERT INTO symbol_search(symbol_id, name, documentation) 
                VALUES (new.Id, new.Name, new.Documentation);
            END;
        ");

        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS symbol_search_update 
            AFTER UPDATE ON Symbols 
            BEGIN
                UPDATE symbol_search 
                SET name = new.Name, documentation = new.Documentation 
                WHERE symbol_id = new.Id;
            END;
        ");

        await Database.ExecuteSqlRawAsync(@"
            CREATE TRIGGER IF NOT EXISTS symbol_search_delete 
            AFTER DELETE ON Symbols 
            BEGIN
                DELETE FROM symbol_search WHERE symbol_id = old.Id;
            END;
        ");
    }
}