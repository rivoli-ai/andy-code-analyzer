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
            
            // Create a test directory with some files
            var testDir = "test-project";
            Directory.CreateDirectory(testDir);
            
            // Create sample files
            await File.WriteAllTextAsync(Path.Combine(testDir, "Calculator.cs"), @"
namespace TestProject
{
    public class Calculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Multiply(int a, int b) => a * b;
        public double Divide(int a, int b) => a / (double)b;
    }
}");
            
            await File.WriteAllTextAsync(Path.Combine(testDir, "StringUtils.cs"), @"
using System;
using System.Linq;

namespace TestProject
{
    public static class StringUtils
    {
        public static string Reverse(string input)
        {
            return new string(input.Reverse().ToArray());
        }
        
        public static bool IsPalindrome(string input)
        {
            var cleaned = input.ToLower().Replace("" "", """");
            return cleaned == Reverse(cleaned);
        }
    }
}");
            
            await File.WriteAllTextAsync(Path.Combine(testDir, "data_processor.py"), @"
class DataProcessor:
    def __init__(self):
        self.data = []
    
    def add_item(self, item):
        self.data.append(item)
    
    def process_all(self):
        return [self.process_item(item) for item in self.data]
    
    def process_item(self, item):
        return item.upper() if isinstance(item, str) else str(item)
");
            
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
                
                Console.WriteLine("Monitoring file changes... Make changes to files in test-project/");
                Console.WriteLine("Press any key to stop monitoring.\n");
                
                // Simulate file changes
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    
                    // Modify a file
                    var filePath = Path.Combine("test-project", "Calculator.cs");
                    if (File.Exists(filePath))
                    {
                        var content = await File.ReadAllTextAsync(filePath);
                        content += "\n        public double Power(double x, double y) => Math.Pow(x, y);";
                        await File.WriteAllTextAsync(filePath, content);
                    }
                    
                    await Task.Delay(1000);
                    
                    // Add a new file
                    await File.WriteAllTextAsync(Path.Combine("test-project", "MathHelpers.cs"), @"
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
            
            // Create a workspace with mixed content
            var workspace = "mixed-project";
            Directory.CreateDirectory(workspace);
            
            // Create various files
            var files = new[]
            {
                ("Models/User.cs", @"
namespace Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}"),
                ("Services/UserService.cs", @"
using System;
using System.Collections.Generic;
using System.Linq;
using Models;

namespace Services
{
    public class UserService
    {
        private readonly List<User> _users = new();
        
        public User CreateUser(string name, string email)
        {
            var user = new User 
            { 
                Id = _users.Count + 1,
                Name = name,
                Email = email,
                CreatedAt = DateTime.Now
            };
            _users.Add(user);
            return user;
        }
        
        public User GetUser(int id) => _users.FirstOrDefault(u => u.Id == id);
        public IEnumerable<User> GetAllUsers() => _users.AsReadOnly();
    }
}"),
                ("utils.py", @"
import re
from typing import List, Optional

def validate_email(email: str) -> bool:
    """"""Validate email address format""""""
    pattern = r'^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$'
    return re.match(pattern, email) is not None

def parse_name(full_name: str) -> tuple[str, str]:
    """"""Parse full name into first and last name""""""
    parts = full_name.strip().split(' ', 1)
    first_name = parts[0]
    last_name = parts[1] if len(parts) > 1 else ''
    return first_name, last_name

class NameParser:
    def __init__(self):
        self.prefixes = ['Mr.', 'Mrs.', 'Ms.', 'Dr.']
    
    def parse(self, name: str) -> dict:
        # Remove prefixes
        for prefix in self.prefixes:
            if name.startswith(prefix):
                name = name[len(prefix):].strip()
                break
        
        first, last = parse_name(name)
        return {'first': first, 'last': last}
")
            };
            
            // Create directory structure and files
            foreach (var (path, content) in files)
            {
                var fullPath = Path.Combine(workspace, path);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                await File.WriteAllTextAsync(fullPath, content);
            }
            
            // Initialize analyzer
            dashboard.Start();
            await analyzer.InitializeAsync(workspace);
            
            // Wait a bit to see the results
            await Task.Delay(2000);
            
            // Make a change to trigger file watcher
            Console.WriteLine("\nMaking a change to trigger file watcher...");
            var userServicePath = Path.Combine(workspace, "Services/UserService.cs");
            var serviceContent = await File.ReadAllTextAsync(userServicePath);
            serviceContent = serviceContent.Replace(
                "public IEnumerable<User> GetAllUsers() => _users.AsReadOnly();",
                @"public IEnumerable<User> GetAllUsers() => _users.AsReadOnly();
        public void DeleteUser(int id) => _users.RemoveAll(u => u.Id == id);
        public User UpdateUser(int id, string name, string email)
        {
            var user = GetUser(id);
            if (user != null)
            {
                user.Name = name;
                user.Email = email;
            }
            return user;
        }"
            );
            await File.WriteAllTextAsync(userServicePath, serviceContent);
            
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