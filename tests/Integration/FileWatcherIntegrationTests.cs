using System;
using System.IO;
using System.Linq;
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

public class FileWatcherIntegrationTests : IAsyncLifetime
{
    private ServiceProvider _serviceProvider = null!;
    private ICodeAnalyzerService _codeAnalyzer = null!;
    private string _testDirectory = null!;

    public async Task InitializeAsync()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"FileWatcherTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddCodeAnalyzer(options =>
        {
            options.WorkspacePath = _testDirectory;
            options.DatabaseConnectionString = $"Data Source={Path.Combine(_testDirectory, "test.db")}";
            options.IndexOnStartup = false;
            options.EnableFileWatcher = true; // Enable file watcher for these tests
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
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task Should_Handle_File_Updates_With_FileWatcher()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "UpdateTest.cs");
        await File.WriteAllTextAsync(testFile, @"
public class UpdateTest
{
    public void OldMethod() { }
}");

        using var scope = _serviceProvider.CreateScope();
        var indexingService = scope.ServiceProvider.GetRequiredService<IIndexingService>();
        await indexingService.IndexWorkspaceAsync(_testDirectory);

        // Verify initial state
        var initialSymbols = await _codeAnalyzer.SearchSymbolsAsync("OldMethod", 
            new SymbolFilter { MaxResults = 10 });
        initialSymbols.Should().HaveCount(1);

        // Setup file change event handler
        var fileChangedTcs = new TaskCompletionSource<FileChangedEventArgs>();
        _codeAnalyzer.FileChanged += (sender, args) => fileChangedTcs.TrySetResult(args);

        // Act - Update the file
        await File.WriteAllTextAsync(testFile, @"
public class UpdateTest
{
    public void NewMethod() { }
    public string NewProperty { get; set; }
}");

        // Wait for file change to be detected and processed
        var fileChangeTask = fileChangedTcs.Task;
        var completedTask = await Task.WhenAny(fileChangeTask, Task.Delay(5000));
        completedTask.Should().Be(fileChangeTask, "File change should be detected within 5 seconds");

        // Give some time for indexing to complete
        await Task.Delay(500);

        // Assert - Verify the index was updated
        var oldMethodSymbols = await _codeAnalyzer.SearchSymbolsAsync("OldMethod", 
            new SymbolFilter { MaxResults = 10 });
        oldMethodSymbols.Should().BeEmpty();

        var newMethodSymbols = await _codeAnalyzer.SearchSymbolsAsync("NewMethod", 
            new SymbolFilter { MaxResults = 10 });
        newMethodSymbols.Should().HaveCount(1);

        var propertySymbols = await _codeAnalyzer.SearchSymbolsAsync("NewProperty", 
            new SymbolFilter { MaxResults = 10 });
        propertySymbols.Should().HaveCount(1);
    }

    [Fact]
    public async Task Should_Handle_Multiple_Async_File_Updates()
    {
        // Arrange - Create multiple test files
        var testFiles = new[]
        {
            Path.Combine(_testDirectory, "AsyncTest1.cs"),
            Path.Combine(_testDirectory, "AsyncTest2.cs"),
            Path.Combine(_testDirectory, "AsyncTest3.cs")
        };

        foreach (var file in testFiles)
        {
            await File.WriteAllTextAsync(file, @"
public class InitialClass
{
    public void InitialMethod() { }
}");
        }

        using var scope = _serviceProvider.CreateScope();
        var indexingService = scope.ServiceProvider.GetRequiredService<IIndexingService>();
        await indexingService.IndexWorkspaceAsync(_testDirectory);

        // Track file changes
        var fileChangeCount = 0;
        var fileChangeSemaphore = new SemaphoreSlim(0, testFiles.Length);
        _codeAnalyzer.FileChanged += (sender, args) =>
        {
            Interlocked.Increment(ref fileChangeCount);
            fileChangeSemaphore.Release();
        };

        // Act - Update all files asynchronously
        var updateTasks = testFiles.Select((file, index) => Task.Run(async () =>
        {
            await Task.Delay(index * 100); // Stagger the updates
            await File.WriteAllTextAsync(file, $@"
public class UpdatedClass{index}
{{
    public void UpdatedMethod{index}() {{ }}
    public async Task AsyncMethod{index}() {{ await Task.Delay(1); }}
}}");
        })).ToArray();

        await Task.WhenAll(updateTasks);

        // Wait for all file changes to be detected
        for (int i = 0; i < testFiles.Length; i++)
        {
            var acquired = await fileChangeSemaphore.WaitAsync(5000);
            acquired.Should().BeTrue($"File change {i + 1} should be detected within 5 seconds");
        }

        // Give time for indexing to complete
        await Task.Delay(1000);

        // Assert - Verify all files were updated
        for (int i = 0; i < testFiles.Length; i++)
        {
            var symbols = await _codeAnalyzer.SearchSymbolsAsync($"UpdatedMethod{i}", 
                new SymbolFilter { MaxResults = 10 });
            symbols.Should().HaveCount(1, $"UpdatedMethod{i} should be found");

            var asyncSymbols = await _codeAnalyzer.SearchSymbolsAsync($"AsyncMethod{i}", 
                new SymbolFilter { MaxResults = 10 });
            asyncSymbols.Should().HaveCount(1, $"AsyncMethod{i} should be found");
        }

        fileChangeCount.Should().Be(testFiles.Length, "All file changes should be detected");
    }
}