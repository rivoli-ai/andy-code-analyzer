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

public class CSharpAnalyzerTests : IDisposable
{
    private readonly CSharpAnalyzer _analyzer;
    private readonly Mock<ILogger<CSharpAnalyzer>> _loggerMock;
    private readonly string _testDirectory;

    public CSharpAnalyzerTests()
    {
        _loggerMock = new Mock<ILogger<CSharpAnalyzer>>();
        _analyzer = new CSharpAnalyzer(_loggerMock.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"CSharpAnalyzerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void SupportedExtensions_Should_Include_CSharp_Files()
    {
        // Assert
        _analyzer.SupportedExtensions.Should().Contain(".cs");
        _analyzer.SupportedExtensions.Should().Contain(".csx");
    }

    [Fact]
    public void Language_Should_Be_CSharp()
    {
        // Assert
        _analyzer.Language.Should().Be("csharp");
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Extract_Class_And_Methods()
    {
        // Arrange
        const string code = @"
namespace TestNamespace
{
    /// <summary>
    /// Test class documentation.
    /// </summary>
    public class TestClass
    {
        private readonly string _field;

        public string Property { get; set; }

        public TestClass(string field)
        {
            _field = field;
        }

        /// <summary>
        /// Test method documentation.
        /// </summary>
        public void TestMethod()
        {
            Console.WriteLine(_field);
        }

        public async Task TestAsyncMethod()
        {
            await Task.Delay(100);
        }
    }
}";

        // Act
        var symbols = (await _analyzer.ExtractSymbolsAsync(code)).ToList();

        // Assert
        symbols.Should().HaveCount(7); // namespace, class, field, property, constructor, 2 methods

        var classSymbol = symbols.FirstOrDefault(s => s.Name == "TestClass" && s.Kind == SymbolKind.Class);
        classSymbol.Should().NotBeNull();
        classSymbol!.Documentation.Should().Contain("Test class documentation");
        classSymbol.Modifiers.Should().Contain("public");

        var methodSymbol = symbols.FirstOrDefault(s => s.Name == "TestMethod" && s.Kind == SymbolKind.Method);
        methodSymbol.Should().NotBeNull();
        methodSymbol!.Documentation.Should().Contain("Test method documentation");
        methodSymbol.ParentSymbol.Should().Be("TestClass");

        var asyncMethodSymbol = symbols.FirstOrDefault(s => s.Name == "TestAsyncMethod");
        asyncMethodSymbol.Should().NotBeNull();
        asyncMethodSymbol!.Modifiers.Should().Contain("async");
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Handle_Interfaces()
    {
        // Arrange
        const string code = @"
public interface ITestInterface
{
    string GetValue();
    Task<int> GetCountAsync();
}";

        // Act
        var symbols = (await _analyzer.ExtractSymbolsAsync(code)).ToList();

        // Assert
        symbols.Should().HaveCount(3); // interface + 2 methods
        
        var interfaceSymbol = symbols.FirstOrDefault(s => s.Kind == SymbolKind.Interface);
        interfaceSymbol.Should().NotBeNull();
        interfaceSymbol!.Name.Should().Be("ITestInterface");
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Handle_Nested_Types()
    {
        // Arrange
        const string code = @"
public class OuterClass
{
    public class InnerClass
    {
        public void InnerMethod() { }
    }
}";

        // Act
        var symbols = (await _analyzer.ExtractSymbolsAsync(code)).ToList();

        // Assert
        symbols.Should().HaveCount(3); // outer class, inner class, inner method

        var innerClass = symbols.FirstOrDefault(s => s.Name == "InnerClass");
        innerClass.Should().NotBeNull();
        innerClass!.ParentSymbol.Should().Be("OuterClass");

        var innerMethod = symbols.FirstOrDefault(s => s.Name == "InnerMethod");
        innerMethod.Should().NotBeNull();
        innerMethod!.ParentSymbol.Should().Be("InnerClass");
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Extract_Using_Directives()
    {
        // Arrange
        const string code = @"
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using MyAlias = System.Text.StringBuilder;

namespace Test
{
    public class TestClass { }
}";

        var filePath = Path.Combine(_testDirectory, "test.cs");
        await File.WriteAllTextAsync(filePath, code);

        // Act
        var structure = await _analyzer.AnalyzeFileAsync(filePath);

        // Assert
        structure.Imports.Should().HaveCount(6);
        structure.Imports.Should().Contain(i => i.Name == "System");
        structure.Imports.Should().Contain(i => i.Name == "System.Collections.Generic");
        
        var aliasImport = structure.Imports.FirstOrDefault(i => i.Alias == "MyAlias");
        aliasImport.Should().NotBeNull();
        aliasImport!.Name.Should().Be("System.Text.StringBuilder");
    }

    [Fact]
    public async Task AnalyzeFileAsync_Should_Set_Metadata()
    {
        // Arrange
        const string code = @"
public class TestClass
{
    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}";

        var filePath = Path.Combine(_testDirectory, "test.cs");
        await File.WriteAllTextAsync(filePath, code);

        // Act
        var structure = await _analyzer.AnalyzeFileAsync(filePath);

        // Assert
        structure.Metadata.Should().ContainKey("HasAsync");
        structure.Metadata["HasAsync"].Should().Be(true);
        structure.Metadata.Should().ContainKey("TargetFramework");
        structure.Metadata.Should().ContainKey("LangVersion");
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Handle_Properties_And_Fields()
    {
        // Arrange
        const string code = @"
public class TestClass
{
    private readonly int _readOnlyField;
    public static string StaticField;
    
    public int AutoProperty { get; set; }
    public string ReadOnlyProperty { get; }
    private bool PrivateProperty { get; set; }
}";

        // Act
        var symbols = (await _analyzer.ExtractSymbolsAsync(code)).ToList();

        // Assert
        var fields = symbols.Where(s => s.Kind == SymbolKind.Field).ToList();
        fields.Should().HaveCount(2);
        fields.Should().Contain(f => f.Name == "_readOnlyField" && f.Modifiers.Contains("private"));
        fields.Should().Contain(f => f.Name == "StaticField" && f.Modifiers.Contains("static"));

        var properties = symbols.Where(s => s.Kind == SymbolKind.Property).ToList();
        properties.Should().HaveCount(3);
        properties.Should().Contain(p => p.Name == "AutoProperty" && p.Modifiers.Contains("public"));
        properties.Should().Contain(p => p.Name == "PrivateProperty" && p.Modifiers.Contains("private"));
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Handle_Enums()
    {
        // Arrange
        const string code = @"
public enum TestEnum
{
    Value1,
    Value2 = 10,
    Value3
}";

        // Act
        var symbols = (await _analyzer.ExtractSymbolsAsync(code)).ToList();

        // Assert
        symbols.Should().Contain(s => s.Name == "TestEnum" && s.Kind == SymbolKind.Enum);
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Handle_Structs()
    {
        // Arrange
        const string code = @"
public struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}";

        // Act
        var symbols = (await _analyzer.ExtractSymbolsAsync(code)).ToList();

        // Assert
        var structSymbol = symbols.FirstOrDefault(s => s.Kind == SymbolKind.Struct);
        structSymbol.Should().NotBeNull();
        structSymbol!.Name.Should().Be("Point");
    }

    [Fact]
    public async Task ExtractSymbolsAsync_Should_Handle_File_Scoped_Namespaces()
    {
        // Arrange
        const string code = @"
namespace TestNamespace;

public class TestClass
{
    public void TestMethod() { }
}";

        // Act
        var symbols = (await _analyzer.ExtractSymbolsAsync(code)).ToList();

        // Assert
        var namespaceSymbol = symbols.FirstOrDefault(s => s.Kind == SymbolKind.Namespace);
        namespaceSymbol.Should().NotBeNull();
        namespaceSymbol!.Name.Should().Be("TestNamespace");

        var classSymbol = symbols.FirstOrDefault(s => s.Kind == SymbolKind.Class);
        classSymbol.Should().NotBeNull();
        classSymbol!.ParentSymbol.Should().Be("TestNamespace");
    }
}