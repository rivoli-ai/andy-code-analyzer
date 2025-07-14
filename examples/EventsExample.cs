using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Andy.CodeAnalyzer.Extensions;
using Andy.CodeAnalyzer.Services;
using Andy.CodeAnalyzer.Models;

namespace Examples
{
    /// <summary>
    /// Example demonstrating event handling with Andy.CodeAnalyzer
    /// Shows progress tracking, file change monitoring, and real-time updates
    /// </summary>
    class EventsExample
    {
        static async Task Main(string[] args)
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // Add code analyzer with file watching enabled
            services.AddCodeAnalyzer(options =>
            {
                options.DatabaseConnectionString = "Data Source=events-example.db";
                options.EnableFileWatcher = true;
                options.EnableFileWatcher = true;
                options.IndexOnStartup = true;
                options.DebounceDelay = TimeSpan.FromMilliseconds(500);
            });
            
            var serviceProvider = services.BuildServiceProvider();
            var analyzer = serviceProvider.GetRequiredService<ICodeAnalyzerService>();
            
            // Example 1: Track indexing progress
            Console.WriteLine("=== Indexing Progress Example ===");
            await TrackIndexingProgress(analyzer);
            
            // Example 2: Monitor file changes
            Console.WriteLine("\n=== File Change Monitoring Example ===");
            await MonitorFileChanges(analyzer);
            
            // Example 3: Real-time analysis with progress UI
            Console.WriteLine("\n=== Real-time Analysis Example ===");
            await RealTimeAnalysisExample(analyzer);
        }
        
        static async Task TrackIndexingProgress(ICodeAnalyzerService analyzer)
        {
            var progressBar = new ConsoleProgressBar();
            var indexingComplete = new TaskCompletionSource<bool>();
            
            // Subscribe to progress events
            analyzer.IndexingProgress += (sender, e) =>
            {
                if (e.TotalFiles == 0) return;
                
                var percentage = (e.ProcessedFiles * 100.0) / e.TotalFiles;
                progressBar.Update(percentage, e.CurrentFile ?? string.Empty);
                
                if (e.IsComplete)
                {
                    progressBar.Complete($"Indexed {e.ProcessedFiles} files successfully!");
                    indexingComplete.SetResult(true);
                }
            };
            
            // Use the sample directories for tracking progress
            var testDir = "sample-mixed";
            Directory.CreateDirectory(testDir);
            
            // Copy some files from our samples to create a mixed project
            if (File.Exists("sample-csharp/Calculator.cs"))
                File.Copy("sample-csharp/Calculator.cs", Path.Combine(testDir, "Calculator.cs"), true);
            if (File.Exists("sample-csharp/UserService.cs"))
                File.Copy("sample-csharp/UserService.cs", Path.Combine(testDir, "UserService.cs"), true);
            if (File.Exists("sample-python/data_processor.py"))
                File.Copy("sample-python/data_processor.py", Path.Combine(testDir, "data_processor.py"), true);
            
            // Initialize and index the directory
            await analyzer.InitializeAsync(testDir);
            
            // Wait for indexing to complete
            await indexingComplete.Task;
            
            // Show statistics
            var stats = await analyzer.GetStatisticsAsync();
            Console.WriteLine($"\nIndexing Statistics:");
            Console.WriteLine($"  Total files: {stats.TotalFiles}");
            Console.WriteLine($"  Total symbols: {stats.TotalSymbols}");
            Console.WriteLine($"  Time taken: {stats.IndexingTime.TotalSeconds:F2} seconds");
        }
        
        static Task MonitorFileChanges(ICodeAnalyzerService analyzer)
        {
            return Task.Run(() =>
            {
                var cts = new CancellationTokenSource();
                var changesDetected = 0;
                
                // Subscribe to file change events
                analyzer.FileChanged += (sender, e) =>
                {
                    changesDetected++;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] File {e.Change.ChangeType}: {e.Change.Path}");
                    
                    // Analyze what changed
                    if (e.Change.ChangeType == FileChangeType.Modified)
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var structure = await analyzer.GetFileStructureAsync(e.Change.Path);
                                Console.WriteLine($"  Updated symbols: {structure.Symbols.Count}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  Error analyzing: {ex.Message}");
                            }
                        });
                    }
                };
                
                Console.WriteLine("Monitoring file changes... Make changes to files in sample-mixed/");
                Console.WriteLine("Press any key to stop monitoring.\n");
                
                // Simulate file changes
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    
                    // Modify a file
                    var filePath = Path.Combine("sample-mixed", "Calculator.cs");
                    if (File.Exists(filePath))
                    {
                        var content = await File.ReadAllTextAsync(filePath);
                        content += "\n        public double Power(double x, double y) => Math.Pow(x, y);";
                        await File.WriteAllTextAsync(filePath, content);
                    }
                    
                    await Task.Delay(1000);
                    
                    // Add a new file
                    await File.WriteAllTextAsync(Path.Combine("sample-mixed", "MathHelpers.cs"), @"
namespace TestProject
{
    public static class MathHelpers
    {
        public static int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
        public static bool IsPrime(int n) => n > 1 && !Enumerable.Range(2, (int)Math.Sqrt(n) - 1).Any(i => n % i == 0);
    }
}");
                });
                
                // Wait for user input
                Console.ReadKey();
                cts.Cancel();
                
                Console.WriteLine($"\nTotal changes detected: {changesDetected}");
            });
        }
        
        static async Task RealTimeAnalysisExample(ICodeAnalyzerService analyzer)
        {
            var dashboard = new AnalysisDashboard();
            
            // Subscribe to both events
            analyzer.IndexingProgress += (sender, e) =>
            {
                dashboard.UpdateProgress(e);
            };
            
            analyzer.FileChanged += async (sender, e) =>
            {
                dashboard.LogFileChange(e.Change);
                
                // Get updated statistics
                var stats = await analyzer.GetStatisticsAsync();
                dashboard.UpdateStatistics(stats);
            };
            
            // Create a workspace with mixed content from our samples
            var workspace = "sample-realtime";
            Directory.CreateDirectory(workspace);
            
            // Copy files from samples to create a mixed project
            Directory.CreateDirectory(Path.Combine(workspace, "Models"));
            Directory.CreateDirectory(Path.Combine(workspace, "Services"));
            
            // Copy some C# files
            if (File.Exists("sample-csharp/UserService.cs"))
            {
                var content = await File.ReadAllTextAsync("sample-csharp/UserService.cs");
                await File.WriteAllTextAsync(Path.Combine(workspace, "Services/UserService.cs"), content);
            }
            
            if (File.Exists("sample-csharp/DataProcessor.cs"))
            {
                var content = await File.ReadAllTextAsync("sample-csharp/DataProcessor.cs");
                await File.WriteAllTextAsync(Path.Combine(workspace, "DataProcessor.cs"), content);
            }
            
            // Copy some Python files
            if (File.Exists("sample-python/web_scraper.py"))
            {
                var content = await File.ReadAllTextAsync("sample-python/web_scraper.py");
                await File.WriteAllTextAsync(Path.Combine(workspace, "web_scraper.py"), content);
            }
            
            if (File.Exists("sample-python/file_utils.py"))
            {
                var content = await File.ReadAllTextAsync("sample-python/file_utils.py");
                await File.WriteAllTextAsync(Path.Combine(workspace, "file_utils.py"), content);
            }
            
            // Initialize analyzer
            dashboard.Start();
            await analyzer.InitializeAsync(workspace);
            
            // Wait a bit to see the results
            await Task.Delay(2000);
            
            // Make a change to trigger file watcher
            Console.WriteLine("\nMaking a change to trigger file watcher...");
            var dataProcessorPath = Path.Combine(workspace, "DataProcessor.cs");
            if (File.Exists(dataProcessorPath))
            {
                var content = await File.ReadAllTextAsync(dataProcessorPath);
                // Add a new method to the StringDataProcessor class
                content = content.Replace(
                    "private async Task<string> ProcessStringAsync(string input)",
                    @"public async Task<int> CountWordsAsync(string input)
        {
            await Task.Delay(1); // Simulate async operation
            return input.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }
        
        private async Task<string> ProcessStringAsync(string input)"
                );
                await File.WriteAllTextAsync(dataProcessorPath, content);
            }
            
            await Task.Delay(2000);
            dashboard.Stop();
        }
    }
    
    // Helper classes for the examples
    
    class ConsoleProgressBar
    {
        private int _lastPercentage = -1;
        
        public void Update(double percentage, string currentFile)
        {
            var percent = (int)percentage;
            if (percent != _lastPercentage)
            {
                _lastPercentage = percent;
                
                Console.CursorLeft = 0;
                Console.Write($"[{new string('#', percent / 5)}{new string('-', 20 - percent / 5)}] {percent}%");
                
                if (!string.IsNullOrEmpty(currentFile))
                {
                    Console.Write($" - {Path.GetFileName(currentFile)}");
                }
                
                Console.Write(new string(' ', 20)); // Clear rest of line
            }
        }
        
        public void Complete(string message)
        {
            Console.CursorLeft = 0;
            Console.WriteLine($"{message}{new string(' ', 50)}");
        }
    }
    
    class AnalysisDashboard
    {
        private readonly object _lock = new object();
        private int _fileChanges = 0;
        private AnalyzerStatistics? _stats;
        
        public void Start()
        {
            Console.Clear();
            Console.WriteLine("=== Real-time Code Analysis Dashboard ===\n");
        }
        
        public void UpdateProgress(IndexingProgressEventArgs e)
        {
            lock (_lock)
            {
                Console.SetCursorPosition(0, 2);
                Console.WriteLine($"Indexing Progress: {e.ProcessedFiles}/{e.TotalFiles} files");
                
                if (e.CurrentFile != null)
                {
                    Console.WriteLine($"Current: {Path.GetFileName(e.CurrentFile)}     ");
                }
            }
        }
        
        public void LogFileChange(FileChangeEvent change)
        {
            lock (_lock)
            {
                _fileChanges++;
                Console.SetCursorPosition(0, 5);
                Console.WriteLine($"File Changes: {_fileChanges}");
                Console.WriteLine($"Latest: {change.ChangeType} - {Path.GetFileName(change.Path)}     ");
            }
        }
        
        public void UpdateStatistics(AnalyzerStatistics stats)
        {
            lock (_lock)
            {
                _stats = stats;
                Console.SetCursorPosition(0, 8);
                Console.WriteLine("Statistics:");
                Console.WriteLine($"  Files: {stats.TotalFiles}");
                Console.WriteLine($"  Symbols: {stats.TotalSymbols}");
                
                if (stats.LanguageDistribution != null)
                {
                    Console.WriteLine("  Languages:");
                    foreach (var lang in stats.LanguageDistribution)
                    {
                        Console.WriteLine($"    {lang.Key}: {lang.Value} files");
                    }
                }
            }
        }
        
        public void Stop()
        {
            Console.SetCursorPosition(0, 15);
            Console.WriteLine("\nDashboard stopped.");
        }
    }
}