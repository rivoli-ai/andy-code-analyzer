using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Andy.CodeAnalyzer.Extensions;
using Andy.CodeAnalyzer.Services;
using Andy.CodeAnalyzer.Models;

namespace Examples
{
    /// <summary>
    /// Example demonstrating how to analyze C# source files using Andy.CodeAnalyzer
    /// </summary>
    class CSharpAnalysisExample
    {
        public static async Task Main(string[] args)
        {
            // Set up dependency injection
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // Add code analyzer with configuration
            services.AddCodeAnalyzer(options =>
            {
                options.DatabaseConnectionString = "Data Source=csharp-analysis.db";
                options.EnableFileWatcher = false; // Disable for this example
                options.IndexOnStartup = true;
                // Override ignore patterns to allow indexing our sample files in bin directory
                options.IgnorePatterns = new[]
                {
                    "**/node_modules/**",
                    "**/obj/**",
                    "**/.git/**",
                    "**/dist/**",
                    "**/.vs/**",
                    "**/.vscode/**",
                    "**/.idea/**"
                    // Removed **/bin/** to allow indexing our sample files
                };
            });
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Get the analyzer service
            var analyzer = serviceProvider.GetRequiredService<ICodeAnalyzerService>();
            
            // Initialize with the sample C# project
            var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
            var samplePath = Path.Combine(baseDir, "sample-csharp");
            Console.WriteLine($"Initializing analyzer with sample C# project at: {samplePath}");
            await analyzer.InitializeAsync(samplePath);
            
            // Wait a bit for indexing to complete
            await Task.Delay(2000);
            
            // Get statistics to verify indexing completed
            var stats = await analyzer.GetStatisticsAsync();
            Console.WriteLine($"\nIndexing complete: {stats.TotalFiles} files, {stats.TotalSymbols} symbols");
            
            // Example 1: Analyze a single C# file
            Console.WriteLine("\n=== Analyzing Calculator.cs ===");
            await AnalyzeSingleFile(analyzer, "sample-csharp/Calculator.cs");
            
            // Example 2: Search for classes in the project
            Console.WriteLine("\n=== Searching for Classes ===");
            await SearchForClasses(analyzer);
            
            // Example 3: Find all methods with specific modifiers
            Console.WriteLine("\n=== Finding Public Async Methods ===");
            await FindPublicAsyncMethods(analyzer);
            
            // Example 4: Get file statistics
            Console.WriteLine("\n=== File Statistics ===");
            await ShowFileStatistics(analyzer);
            
            // Example 5: Analyze interfaces
            Console.WriteLine("\n=== Analyzing Interfaces ===");
            await AnalyzeInterfaces(analyzer);
        }
        
        static async Task AnalyzeSingleFile(ICodeAnalyzerService analyzer, string filePath)
        {
            // Analyze the specified file
            var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
            var fullPath = Path.Combine(baseDir, filePath);
            var structure = await analyzer.GetFileStructureAsync(fullPath);
            
            Console.WriteLine($"File: {structure.FilePath}");
            Console.WriteLine($"Language: {structure.Language}");
            Console.WriteLine($"Symbols found: {structure.Symbols.Count}");
            
            // List all symbols
            foreach (var symbol in structure.Symbols.OrderBy(s => s.Location.StartLine))
            {
                var modifiers = string.Join(" ", symbol.Modifiers);
                Console.WriteLine($"  {symbol.Kind}: {modifiers} {symbol.Name} (line {symbol.Location.StartLine})");
                
                if (!string.IsNullOrEmpty(symbol.Documentation))
                {
                    Console.WriteLine($"    Doc: {symbol.Documentation}");
                }
            }
            
            // Show imports
            Console.WriteLine($"\nImports: {structure.Imports.Count}");
            foreach (var import in structure.Imports)
            {
                Console.WriteLine($"  using {import.Name};");
            }
        }
        
        static async Task SearchForClasses(ICodeAnalyzerService analyzer)
        {
            // Search for all classes
            var filter = new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Class },
                MaxResults = 100
            };
            Console.WriteLine($"Searching for kinds: {string.Join(", ", filter.Kinds)}");
            
            // Try different search patterns
            Console.WriteLine("\nSearching with empty query:");
            var classes1 = await analyzer.SearchSymbolsAsync("", filter);
            Console.WriteLine($"Found {classes1.Count()} classes");
            
            Console.WriteLine("\nSearching with wildcard (*):");
            var classes2 = await analyzer.SearchSymbolsAsync("*", filter);
            Console.WriteLine($"Found {classes2.Count()} classes");
            
            var allClasses = classes1.Count() > classes2.Count() ? classes1 : classes2;
            
            if (allClasses.Any())
            {
                Console.WriteLine("\nClasses found:");
                foreach (var cls in allClasses)
                {
                    Console.WriteLine($"  {cls.Name}");
                    if (cls.ParentSymbol != null)
                    {
                        Console.WriteLine($"    Parent: {cls.ParentSymbol}");
                    }
                }
            }
            else
            {
                Console.WriteLine("No classes found. Checking database...");
                // This will help us debug
            }
        }
        
        static async Task FindPublicAsyncMethods(ICodeAnalyzerService analyzer)
        {
            // Search for all methods
            var methods = await analyzer.SearchSymbolsAsync("*", new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Method } 
            });
            
            // Filter for public async methods
            var publicAsyncMethods = methods
                .Where(m => m.Modifiers.Contains("public") && m.Modifiers.Contains("async"))
                .ToList();
            
            Console.WriteLine($"Found {publicAsyncMethods.Count} public async methods:");
            foreach (var method in publicAsyncMethods)
            {
                Console.WriteLine($"  {method.Name} in {method.ParentSymbol ?? "global"}");
            }
        }
        
        static async Task ShowFileStatistics(ICodeAnalyzerService analyzer)
        {
            var stats = await analyzer.GetStatisticsAsync();
            
            Console.WriteLine($"Total files indexed: {stats.TotalFiles}");
            Console.WriteLine($"Total symbols extracted: {stats.TotalSymbols}");
            Console.WriteLine($"Indexing time: {stats.IndexingTime.TotalSeconds:F2} seconds");
            
            if (stats.LanguageDistribution != null && stats.LanguageDistribution.Any())
            {
                Console.WriteLine("\nLanguage distribution:");
                foreach (var lang in stats.LanguageDistribution)
                {
                    Console.WriteLine($"  {lang.Key}: {lang.Value} files");
                }
            }
        }
        
        static async Task AnalyzeInterfaces(ICodeAnalyzerService analyzer)
        {
            // Search for all interfaces
            var interfaces = await analyzer.SearchSymbolsAsync("*", new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Interface } 
            });
            
            Console.WriteLine($"Found {interfaces.Count()} interfaces:");
            foreach (var iface in interfaces)
            {
                Console.WriteLine($"  {iface.Name}");
                
                // Find methods in this interface
                // Note: To find methods within a specific interface, we would need to
                // filter by file path or use a more specific search query
                var methods = await analyzer.SearchSymbolsAsync(iface.Name + ".*", new SymbolFilter
                {
                    Kinds = new[] { SymbolKind.Method }
                });
                
                if (methods.Any())
                {
                    Console.WriteLine($"    Methods: {string.Join(", ", methods.Select(m => m.Name))}");
                }
            }
        }
    }
}