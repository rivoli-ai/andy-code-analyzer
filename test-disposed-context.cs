using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Options;
using Andy.CodeAnalyzer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class TestDisposedContext
{
    static async Task Main()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddCodeAnalyzer(options =>
        {
            options.DatabasePath = Path.Combine(Path.GetTempPath(), "test-andy.db");
            options.EnableFileWatcher = true;
        });

        var provider = services.BuildServiceProvider();
        
        // Create a scope and initialize
        using (var scope = provider.CreateScope())
        {
            var analyzer = scope.ServiceProvider.GetRequiredService<ICodeAnalyzerService>();
            var testDir = Path.Combine(Path.GetTempPath(), "test-workspace");
            Directory.CreateDirectory(testDir);
            
            await analyzer.InitializeAsync(testDir);
            Console.WriteLine("Initialized analyzer");
        }
        
        // Scope is now disposed - simulate file change after delay
        await Task.Delay(1000);
        
        // Create a test file to trigger file watcher
        var testFile = Path.Combine(Path.GetTempPath(), "test-workspace", "test.cs");
        await File.WriteAllTextAsync(testFile, "public class Test { }");
        
        Console.WriteLine("Created test file, waiting for file watcher to process...");
        await Task.Delay(5000);
        
        Console.WriteLine("Test completed - check logs for ObjectDisposedException");
    }
}