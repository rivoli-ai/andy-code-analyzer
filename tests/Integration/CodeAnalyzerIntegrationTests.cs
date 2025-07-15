using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Extensions;
using Andy.CodeAnalyzer.Models;
using Andy.CodeAnalyzer.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Integration;

public class CodeAnalyzerIntegrationTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;
    private ICodeAnalyzerService _codeAnalyzer = null!;
    private string _testDirectory = null!;

    public async Task InitializeAsync()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CodeAnalyzerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddCodeAnalyzer(options =>
        {
            options.WorkspacePath = _testDirectory;
            options.DatabaseConnectionString = $"Data Source={Path.Combine(_testDirectory, "test.db")}";
            options.IndexOnStartup = false;
            options.EnableFileWatcher = false; // Disable file watcher in tests
            options.IgnorePatterns = new[] { "**/bin/**", "**/obj/**" };
        });

        _serviceProvider = services.BuildServiceProvider();
        _codeAnalyzer = _serviceProvider.GetRequiredService<ICodeAnalyzerService>();
        
        await _codeAnalyzer.InitializeAsync(_testDirectory);
    }

    public async Task DisposeAsync()
    {
        await _codeAnalyzer.ShutdownAsync();
        _serviceProvider?.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            // Give some time for file handles to be released
            await Task.Delay(100);
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    [Fact]
    public async Task Should_Index_CSharp_Files_In_Directory()
    {
        // Arrange
        var testFile1 = Path.Combine(_testDirectory, "TestClass1.cs");
        await File.WriteAllTextAsync(testFile1, @"
namespace TestProject
{
    public class TestClass1
    {
        public void Method1() { }
        public string Property1 { get; set; }
    }
}");

        var testFile2 = Path.Combine(_testDirectory, "TestClass2.cs");
        await File.WriteAllTextAsync(testFile2, @"
namespace TestProject
{
    public interface ITestInterface
    {
        Task<string> GetDataAsync();
    }

    public class TestClass2 : ITestInterface
    {
        public async Task<string> GetDataAsync()
        {
            await Task.Delay(10);
            return ""test"";
        }
    }
}");

        // Create a file that should be ignored
        var binDir = Path.Combine(_testDirectory, "bin");
        Directory.CreateDirectory(binDir);
        await File.WriteAllTextAsync(Path.Combine(binDir, "ignored.cs"), "public class Ignored { }");

        // Act
        var indexingService = _serviceProvider.GetRequiredService<IIndexingService>();
        await indexingService.IndexWorkspaceAsync(_testDirectory);

        // Small delay to ensure all database operations complete
        await Task.Delay(100);

        // Assert - Check files were indexed
        var files = await _codeAnalyzer.GetFilesAsync();
        files.Should().HaveCount(2);
        files.Should().Contain(f => f.Path == testFile1);
        files.Should().Contain(f => f.Path == testFile2);
        files.Should().NotContain(f => f.Path.Contains("bin"));

        // Assert - Check symbols were extracted
        var symbols = await _codeAnalyzer.SearchSymbolsAsync("Test", new SymbolFilter { MaxResults = 50 });
        symbols.Should().Contain(s => s.Name == "TestClass1");
        symbols.Should().Contain(s => s.Name == "TestClass2");
        symbols.Should().Contain(s => s.Name == "ITestInterface");
    }

    [Fact]
    public async Task Should_Search_For_Classes()
    {
        // Arrange
        await CreateSampleProject();

        // Act
        var indexingService = _serviceProvider.GetRequiredService<IIndexingService>();
        await indexingService.IndexWorkspaceAsync(_testDirectory);

        var classSymbols = await _codeAnalyzer.SearchSymbolsAsync("Service", 
            new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Class },
                MaxResults = 10 
            });

        // Assert
        classSymbols.Should().NotBeEmpty();
        classSymbols.Should().OnlyContain(s => s.Kind == SymbolKind.Class);
        classSymbols.Should().Contain(s => s.Name.Contains("Service"));
    }

    [Fact]
    public async Task Should_Search_Text_In_Files()
    {
        // Arrange
        await CreateSampleProject();
        
        var indexingService = _serviceProvider.GetRequiredService<IIndexingService>();
        await indexingService.IndexWorkspaceAsync(_testDirectory);

        // Act
        var searchResults = await _codeAnalyzer.SearchTextAsync("async Task", 
            new SearchOptions { MaxResults = 10 });

        // Assert
        searchResults.Should().NotBeEmpty();
        searchResults.All(r => r.Snippet.Contains("async") || r.Snippet.Contains("Task"))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Should_Get_File_Structure()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "StructureTest.cs");
        await File.WriteAllTextAsync(testFile, @"
using System;
using System.Threading.Tasks;

namespace TestNamespace
{
    /// <summary>
    /// Test class for structure analysis.
    /// </summary>
    public class StructureTest
    {
        private readonly string _field;

        public string Property { get; set; }

        public StructureTest(string field)
        {
            _field = field;
        }

        public async Task<string> GetDataAsync()
        {
            await Task.Delay(10);
            return _field;
        }
    }
}");

        var indexingService = _serviceProvider.GetRequiredService<IIndexingService>();
        await indexingService.IndexFileAsync(testFile);

        // Act
        var structure = await _codeAnalyzer.GetFileStructureAsync(testFile);

        // Assert
        structure.Should().NotBeNull();
        structure.FilePath.Should().Be(testFile);
        structure.Language.Should().Be("csharp");
        
        structure.Imports.Should().HaveCount(2);
        structure.Imports.Should().Contain(i => i.Name == "System");
        structure.Imports.Should().Contain(i => i.Name == "System.Threading.Tasks");

        structure.Symbols.Should().Contain(s => s.Name == "TestNamespace" && s.Kind == SymbolKind.Namespace);
        structure.Symbols.Should().Contain(s => s.Name == "StructureTest" && s.Kind == SymbolKind.Class);
        structure.Symbols.Should().Contain(s => s.Name == "_field" && s.Kind == SymbolKind.Field);
        structure.Symbols.Should().Contain(s => s.Name == "Property" && s.Kind == SymbolKind.Property);
        structure.Symbols.Should().Contain(s => s.Name == "GetDataAsync" && s.Kind == SymbolKind.Method);

        var classSymbol = structure.Symbols.First(s => s.Kind == SymbolKind.Class);
        classSymbol.Documentation.Should().Contain("Test class for structure analysis");
    }

    private async Task CreateSampleProject()
    {
        // Create a simple project structure
        var srcDir = Path.Combine(_testDirectory, "src");
        Directory.CreateDirectory(srcDir);

        await File.WriteAllTextAsync(Path.Combine(srcDir, "IService.cs"), @"
namespace SampleProject
{
    public interface IService
    {
        Task<string> GetDataAsync();
    }
}");

        await File.WriteAllTextAsync(Path.Combine(srcDir, "DataService.cs"), @"
using System;
using System.Threading.Tasks;

namespace SampleProject
{
    public class DataService : IService
    {
        private readonly string _connectionString;

        public DataService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<string> GetDataAsync()
        {
            await Task.Delay(100);
            return ""Sample data"";
        }
    }
}");

        await File.WriteAllTextAsync(Path.Combine(srcDir, "UserService.cs"), @"
namespace SampleProject
{
    public class UserService
    {
        private readonly IService _dataService;

        public UserService(IService dataService)
        {
            _dataService = dataService;
        }

        public async Task<User> GetUserAsync(int id)
        {
            var data = await _dataService.GetDataAsync();
            return new User { Id = id, Name = data };
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}");
    }
}