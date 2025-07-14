using System;
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
            
            // Add code analyzer with configuration
            services.AddCodeAnalyzer(options =>
            {
                options.DatabaseConnectionString = "Data Source=csharp-analysis.db";
                options.EnableFileWatcher = false; // Disable for this example
                options.IndexOnStartup = true;
            });
            
            var serviceProvider = services.BuildServiceProvider();
            
            // Get the analyzer service
            var analyzer = serviceProvider.GetRequiredService<ICodeAnalyzerService>();
            
            // Example 1: Analyze a single C# file
            Console.WriteLine("=== Analyzing a Single C# File ===");
            await AnalyzeSingleFile(analyzer);
            
            // Example 2: Search for classes in a project
            Console.WriteLine("\n=== Searching for Classes ===");
            await SearchForClasses(analyzer);
            
            // Example 3: Find all methods with specific modifiers
            Console.WriteLine("\n=== Finding Public Async Methods ===");
            await FindPublicAsyncMethods(analyzer);
            
            // Example 4: Get file statistics
            Console.WriteLine("\n=== File Statistics ===");
            await ShowFileStatistics(analyzer);
        }
        
        static async Task AnalyzeSingleFile(ICodeAnalyzerService analyzer)
        {
            // Initialize with current directory (or specify a path)
            await analyzer.InitializeAsync(".");
            
            // Analyze this example file itself
            var structure = await analyzer.GetFileStructureAsync("CSharpAnalysisExample.cs");
            
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
            var classes = await analyzer.SearchSymbolsAsync("*", new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Class } 
            });
            
            Console.WriteLine($"Found {classes.Count()} classes:");
            foreach (var cls in classes)
            {
                Console.WriteLine($"  {cls.Name}");
                if (cls.ParentSymbol != null)
                {
                    Console.WriteLine($"    Parent: {cls.ParentSymbol}");
                }
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
    }
}