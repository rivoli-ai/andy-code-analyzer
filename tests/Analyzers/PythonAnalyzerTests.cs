using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Analyzers;

public class PythonAnalyzerTests
{
    private readonly PythonAnalyzer _analyzer;
    private readonly Mock<ILogger<PythonAnalyzer>> _mockLogger;

    public PythonAnalyzerTests()
    {
        _mockLogger = new Mock<ILogger<PythonAnalyzer>>();
        _analyzer = new PythonAnalyzer(_mockLogger.Object);
    }

    [Fact]
    public void SupportedExtensions_Should_Include_Python_Extensions()
    {
        // Act
        var extensions = _analyzer.SupportedExtensions;

        // Assert
        extensions.Should().Contain(new[] { ".py", ".pyw", ".pyi" });
    }

    [Fact]
    public void Language_Should_Be_Python()
    {
        // Assert
        _analyzer.Language.Should().Be("python");
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Extract_Classes()
    {
        // Arrange
        var content = @"
class Person:
    def __init__(self, name):
        self.name = name

class Student(Person):
    def __init__(self, name, student_id):
        super().__init__(name)
        self.student_id = student_id
";
        var filePath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _analyzer.AnalyzeFileAsync(filePath);

            // Assert
            result.Should().NotBeNull();
            result.Language.Should().Be("python");
            result.FilePath.Should().Be(filePath);
            
            var classes = result.Symbols.Where(s => s.Kind == SymbolKind.Class).ToList();
            classes.Should().HaveCount(2);
            classes.Should().Contain(s => s.Name == "Person");
            classes.Should().Contain(s => s.Name == "Student");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Extract_Functions()
    {
        // Arrange
        var content = @"
def hello_world():
    print('Hello, World!')

async def fetch_data(url):
    # Async function
    pass

def calculate_sum(a: int, b: int) -> int:
    return a + b
";
        var filePath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _analyzer.AnalyzeFileAsync(filePath);

            // Assert
            var functions = result.Symbols.Where(s => s.Kind == SymbolKind.Function).ToList();
            functions.Should().HaveCount(3);
            functions.Should().Contain(s => s.Name == "hello_world");
            functions.Should().Contain(s => s.Name == "fetch_data");
            functions.Should().Contain(s => s.Name == "calculate_sum");

            // Check async modifier
            var asyncFunc = functions.First(f => f.Name == "fetch_data");
            asyncFunc.Modifiers.Should().Contain("async");

            // Check metadata
            result.Metadata["HasAsyncFunctions"].Should().Be(true);
            result.Metadata["HasTypeHints"].Should().Be(true);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Extract_Imports()
    {
        // Arrange
        var content = @"
import os
import sys
from datetime import datetime
from typing import List, Dict as DictType
import numpy as np
";
        var filePath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _analyzer.AnalyzeFileAsync(filePath);

            // Assert
            result.Imports.Should().HaveCount(6); // os, sys, datetime, List, Dict, numpy
            result.Imports.Should().Contain(i => i.Name == "os");
            result.Imports.Should().Contain(i => i.Name == "sys");
            result.Imports.Should().Contain(i => i.Name == "datetime.datetime");
            result.Imports.Should().Contain(i => i.Name == "typing.List");
            result.Imports.Should().Contain(i => i.Name == "typing.Dict" && i.Alias == "DictType");
            result.Imports.Should().Contain(i => i.Name == "numpy" && i.Alias == "np");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Extract_Module_Level_Variables()
    {
        // Arrange
        var content = @"
# Constants
MAX_SIZE = 100
DEFAULT_NAME = 'Unknown'

# Module-level variables
counter = 0
config = {}
";
        var filePath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _analyzer.AnalyzeFileAsync(filePath);

            // Assert
            var constants = result.Symbols.Where(s => s.Kind == SymbolKind.Constant).ToList();
            constants.Should().HaveCount(2);
            constants.Should().Contain(s => s.Name == "MAX_SIZE");
            constants.Should().Contain(s => s.Name == "DEFAULT_NAME");

            var fields = result.Symbols.Where(s => s.Kind == SymbolKind.Field).ToList();
            fields.Should().HaveCount(2);
            fields.Should().Contain(s => s.Name == "counter");
            fields.Should().Contain(s => s.Name == "config");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Work_With_String_Content()
    {
        // Arrange
        var content = @"
class Calculator:
    def add(self, a, b):
        return a + b
    
    def subtract(self, a, b):
        return a - b
";

        // Act
        var symbols = await _analyzer.ExtractSymbolsAsync(content);

        // Assert
        var symbolList = symbols.ToList();
        symbolList.Should().HaveCount(3); // 1 class + 2 methods
        symbolList.Should().Contain(s => s.Name == "Calculator" && s.Kind == SymbolKind.Class);
        symbolList.Should().Contain(s => s.Name == "add" && s.Kind == SymbolKind.Method);
        symbolList.Should().Contain(s => s.Name == "subtract" && s.Kind == SymbolKind.Method);
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Detect_Tests()
    {
        // Arrange
        var content = @"
import unittest

class TestCalculator(unittest.TestCase):
    def test_addition(self):
        self.assertEqual(1 + 1, 2)
    
    def test_subtraction(self):
        self.assertEqual(5 - 3, 2)
";
        var filePath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _analyzer.AnalyzeFileAsync(filePath);

            // Assert
            result.Metadata["HasTests"].Should().Be(true);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Handle_Private_Members()
    {
        // Arrange
        var content = @"
class MyClass:
    def _private_method(self):
        pass
    
    def __magic_method__(self):
        pass
    
    def public_method(self):
        pass

_PRIVATE_VAR = 'private'
";
        var filePath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _analyzer.AnalyzeFileAsync(filePath);

            // Assert
            var privateMethod = result.Symbols.First(s => s.Name == "_private_method");
            privateMethod.Modifiers.Should().Contain("private");

            var magicMethod = result.Symbols.First(s => s.Name == "__magic_method__");
            magicMethod.Modifiers.Should().Contain("magic");

            var privateVar = result.Symbols.First(s => s.Name == "_PRIVATE_VAR");
            privateVar.Modifiers.Should().Contain("private");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Handle_Decorators()
    {
        // Arrange
        var content = @"
class MyClass:
    @staticmethod
    def static_method():
        pass
    
    @classmethod
    def class_method(cls):
        pass
";
        var filePath = Path.GetTempFileName() + ".py";
        await File.WriteAllTextAsync(filePath, content);

        try
        {
            // Act
            var result = await _analyzer.AnalyzeFileAsync(filePath);

            // Assert
            var staticMethod = result.Symbols.First(s => s.Name == "static_method");
            staticMethod.Modifiers.Should().Contain("staticmethod");

            var classMethod = result.Symbols.First(s => s.Name == "class_method");
            classMethod.Modifiers.Should().Contain("classmethod");
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}