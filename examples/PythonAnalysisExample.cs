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
            
            // Add code analyzer
            services.AddCodeAnalyzer(options =>
            {
                options.DatabaseConnectionString = "Data Source=python-analysis.db";
                options.EnableFileWatcher = false;
                options.IndexOnStartup = false; // We'll index specific files
            });
            
            var serviceProvider = services.BuildServiceProvider();
            var analyzer = serviceProvider.GetRequiredService<ICodeAnalyzerService>();
            
            // Create a sample Python file to analyze
            await CreateSamplePythonFile();
            
            // Initialize analyzer
            await analyzer.InitializeAsync(".");
            
            // Example 1: Analyze the Python file
            Console.WriteLine("=== Analyzing Python File ===");
            await AnalyzePythonFile(analyzer);
            
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
        
        static async Task CreateSamplePythonFile()
        {
            var pythonCode = @"#!/usr/bin/env python3
""""""
Sample Python module for demonstrating code analysis.
This module contains various Python constructs to analyze.
""""""

import os
import sys
from datetime import datetime
from typing import List, Optional, Dict
import asyncio

# Constants
DEFAULT_TIMEOUT = 30
MAX_RETRIES = 3

class BaseProcessor:
    """"""Base class for all processors""""""
    
    def __init__(self, name: str):
        self.name = name
        self._internal_state = {}
    
    def process(self, data: Dict) -> bool:
        """"""Process data and return success status""""""
        raise NotImplementedError()

class DataProcessor(BaseProcessor):
    """"""
    Main data processor implementation.
    Handles various data transformation tasks.
    """"""
    
    def __init__(self, name: str, config: Optional[Dict] = None):
        super().__init__(name)
        self.config = config or {}
        self._cache = {}
    
    def process(self, data: Dict) -> bool:
        """"""Process data with validation and transformation""""""
        if not self._validate_data(data):
            return False
        
        # Complex processing logic
        for key, value in data.items():
            if isinstance(value, list):
                data[key] = self._process_list(value)
            elif isinstance(value, dict):
                data[key] = self._process_dict(value)
        
        return True
    
    def _validate_data(self, data: Dict) -> bool:
        """"""Validate input data""""""
        return bool(data) and 'id' in data
    
    @staticmethod
    def _process_list(items: List) -> List:
        """"""Process list items""""""
        return [item.upper() if isinstance(item, str) else item for item in items]
    
    @classmethod
    def from_config(cls, config_path: str) -> 'DataProcessor':
        """"""Create processor from configuration file""""""
        # Load config logic here
        return cls('ConfiguredProcessor', {})

async def async_fetch_data(url: str) -> Dict:
    """"""Asynchronously fetch data from URL""""""
    # Simulated async operation
    await asyncio.sleep(0.1)
    return {'url': url, 'data': 'sample'}

def calculate_metrics(values: List[float]) -> Dict[str, float]:
    """"""
    Calculate statistical metrics for a list of values.
    
    Args:
        values: List of numerical values
        
    Returns:
        Dictionary containing mean, min, max, and sum
    """"""
    if not values:
        return {'mean': 0, 'min': 0, 'max': 0, 'sum': 0}
    
    total = sum(values)
    return {
        'mean': total / len(values),
        'min': min(values),
        'max': max(values),
        'sum': total
    }

# Module-level function
def main():
    """"""Main entry point""""""
    processor = DataProcessor('MainProcessor')
    test_data = {'id': 1, 'values': [1, 2, 3]}
    
    if processor.process(test_data):
        print('Processing successful')
    else:
        print('Processing failed')

if __name__ == '__main__':
    main()
";
            
            await File.WriteAllTextAsync("sample_python_module.py", pythonCode);
            Console.WriteLine("Created sample_python_module.py for analysis");
        }
        
        static async Task AnalyzePythonFile(ICodeAnalyzerService analyzer)
        {
            var structure = await analyzer.GetFileStructureAsync("sample_python_module.py");
            
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