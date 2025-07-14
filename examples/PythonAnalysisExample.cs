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
    /// Example demonstrating how to analyze Python source files using Andy.CodeAnalyzer
    /// </summary>
    class PythonAnalysisExample
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
            
            // Add code analyzer
            services.AddCodeAnalyzer(options =>
            {
                options.DatabaseConnectionString = "Data Source=python-analysis.db";
                options.EnableFileWatcher = false;
                options.IndexOnStartup = false; // We'll index specific files
            });
            
            var serviceProvider = services.BuildServiceProvider();
            var analyzer = serviceProvider.GetRequiredService<ICodeAnalyzerService>();
            
            // Initialize analyzer with sample Python directory
            var samplePath = "sample-python";
            Console.WriteLine($"Initializing analyzer with sample Python project at: {samplePath}");
            await analyzer.InitializeAsync(samplePath);
            
            // Example 1: Analyze a Python file
            Console.WriteLine("\n=== Analyzing data_processor.py ===");
            await AnalyzePythonFile(analyzer, "sample-python/data_processor.py");
            
            // Example 2: Search for functions and methods
            Console.WriteLine("\n=== Searching for Functions ===");
            await SearchForFunctions(analyzer);
            
            // Example 3: Find classes with inheritance
            Console.WriteLine("\n=== Finding Classes with Inheritance ===");
            await FindClassesWithInheritance(analyzer);
            
            // Example 4: Analyze code complexity
            Console.WriteLine("\n=== Code Complexity Analysis ===");
            await AnalyzeComplexity(analyzer);
        }
        
        
        static async Task AnalyzePythonFile(ICodeAnalyzerService analyzer, string filePath)
        {
            var baseDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
            var fullPath = Path.Combine(baseDir, filePath);
            var structure = await analyzer.GetFileStructureAsync(fullPath);
            
            Console.WriteLine($"File: {structure.FilePath}");
            Console.WriteLine($"Language: {structure.Language}");
            Console.WriteLine($"Total symbols: {structure.Symbols.Count}");
            
            // Group symbols by kind
            var symbolGroups = structure.Symbols.GroupBy(s => s.Kind);
            foreach (var group in symbolGroups)
            {
                Console.WriteLine($"\n{group.Key}s ({group.Count()}):");
                foreach (var symbol in group.OrderBy(s => s.Name))
                {
                    var indent = symbol.ParentSymbol != null ? "    " : "  ";
                    Console.WriteLine($"{indent}{symbol.Name}");
                    
                    if (symbol.Modifiers.Any())
                    {
                        Console.WriteLine($"{indent}  Modifiers: {string.Join(", ", symbol.Modifiers)}");
                    }
                    
                    if (!string.IsNullOrEmpty(symbol.Documentation))
                    {
                        var firstLine = symbol.Documentation.Split('\n')[0];
                        Console.WriteLine($"{indent}  Doc: {firstLine}");
                    }
                }
            }
            
            // Show imports
            Console.WriteLine($"\nImports ({structure.Imports.Count}):");
            foreach (var import in structure.Imports)
            {
                var importStr = import.Alias != null 
                    ? $"{import.Name} as {import.Alias}" 
                    : import.Name;
                Console.WriteLine($"  {importStr}");
            }
            
            // Show metadata
            Console.WriteLine("\nMetadata:");
            foreach (var meta in structure.Metadata)
            {
                Console.WriteLine($"  {meta.Key}: {meta.Value}");
            }
        }
        
        static async Task SearchForFunctions(ICodeAnalyzerService analyzer)
        {
            var functions = await analyzer.SearchSymbolsAsync("*", new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Function } 
            });
            
            var methods = await analyzer.SearchSymbolsAsync("*", new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Method } 
            });
            
            Console.WriteLine($"Found {functions.Count()} functions:");
            foreach (var func in functions)
            {
                var asyncMarker = func.Modifiers.Contains("async") ? "[async] " : "";
                Console.WriteLine($"  {asyncMarker}{func.Name}()");
            }
            
            Console.WriteLine($"\nFound {methods.Count()} methods:");
            foreach (var method in methods)
            {
                var visibility = method.Modifiers.Contains("private") ? "private" : "public";
                var staticMarker = method.Modifiers.Contains("staticmethod") ? "[static] " : "";
                var classMarker = method.Modifiers.Contains("classmethod") ? "[class] " : "";
                Console.WriteLine($"  {staticMarker}{classMarker}{visibility} {method.Name}() in {method.ParentSymbol}");
            }
        }
        
        // Remove the CreateSamplePythonFile method as we now use real files
        // The method was at lines 64-177
        
        static async Task FindClassesWithInheritance(ICodeAnalyzerService analyzer)
        {
            var classes = await analyzer.SearchSymbolsAsync("*", new SymbolFilter 
            { 
                Kinds = new[] { SymbolKind.Class } 
            });
            
            Console.WriteLine($"Classes in the codebase:");
            foreach (var cls in classes)
            {
                Console.WriteLine($"  {cls.Name}");
                
                // In a real implementation, we would parse the base classes
                // from the class definition. For now, we can check the name
                if (cls.Name != "BaseProcessor" && cls.Name.Contains("Processor"))
                {
                    Console.WriteLine($"    (likely inherits from BaseProcessor)");
                }
            }
        }
        
        static async Task AnalyzeComplexity(ICodeAnalyzerService analyzer)
        {
            var structure = await analyzer.GetFileStructureAsync("sample_python_module.py");
            
            // Look for complexity metrics in metadata
            var complexityMetrics = structure.Metadata
                .Where(m => m.Key.EndsWith("_complexity"))
                .OrderByDescending(m => m.Value);
            
            if (complexityMetrics.Any())
            {
                Console.WriteLine("Function complexity scores:");
                foreach (var metric in complexityMetrics)
                {
                    var functionName = metric.Key.Replace("_complexity", "");
                    Console.WriteLine($"  {functionName}: {metric.Value}");
                }
            }
            
            // Analyze code patterns
            var hasAsyncFunctions = structure.Metadata.ContainsKey("HasAsyncFunctions") 
                && (bool)structure.Metadata["HasAsyncFunctions"];
            var hasTypeHints = structure.Metadata.ContainsKey("HasTypeHints") 
                && (bool)structure.Metadata["HasTypeHints"];
            var hasTests = structure.Metadata.ContainsKey("HasTests") 
                && (bool)structure.Metadata["HasTests"];
                
            Console.WriteLine("\nCode quality indicators:");
            Console.WriteLine($"  Uses async/await: {(hasAsyncFunctions ? "Yes" : "No")}");
            Console.WriteLine($"  Uses type hints: {(hasTypeHints ? "Yes" : "No")}");
            Console.WriteLine($"  Has test functions: {(hasTests ? "Yes" : "No")}");
            
            if (structure.Metadata.ContainsKey("LinesOfCode"))
            {
                Console.WriteLine($"  Lines of code: {structure.Metadata["LinesOfCode"]}");
            }
        }
    }
}