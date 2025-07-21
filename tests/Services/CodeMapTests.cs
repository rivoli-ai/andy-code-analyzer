using System.Collections.Generic;
using System.Linq;
using Andy.CodeAnalyzer.Services;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Services;

public class CodeMapTests
{
    [Fact]
    public void CodeMap_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var codeMap = new CodeMap();

        // Assert
        Assert.NotNull(codeMap.Files);
        Assert.Empty(codeMap.Files);
        Assert.NotNull(codeMap.Dependencies);
        Assert.Empty(codeMap.Dependencies);
        Assert.NotNull(codeMap.SymbolReferences);
        Assert.Empty(codeMap.SymbolReferences);
    }

    [Fact]
    public void CodeMap_AddFile_ShouldAddToFilesDictionary()
    {
        // Arrange
        var codeMap = new CodeMap();
        var fileOverview = new FileOverview
        {
            Path = "/src/MyClass.cs",
            Language = "csharp",
            MainSymbols = new List<string> { "MyClass", "MyMethod" },
            Imports = new List<string> { "System", "System.Collections" },
            Exports = new List<string> { "MyClass" }
        };

        // Act
        codeMap.Files[fileOverview.Path] = fileOverview;

        // Assert
        Assert.Single(codeMap.Files);
        Assert.True(codeMap.Files.ContainsKey("/src/MyClass.cs"));
        Assert.Equal("csharp", codeMap.Files["/src/MyClass.cs"].Language);
        Assert.Equal(2, codeMap.Files["/src/MyClass.cs"].MainSymbols.Count);
    }

    [Fact]
    public void CodeMap_AddMultipleFiles_ShouldAddAllToFilesDictionary()
    {
        // Arrange
        var codeMap = new CodeMap();
        var files = new Dictionary<string, FileOverview>
        {
            {
                "/src/File1.cs",
                new FileOverview
                {
                    Path = "/src/File1.cs",
                    Language = "csharp",
                    MainSymbols = new List<string> { "Class1" }
                }
            },
            {
                "/src/File2.py",
                new FileOverview
                {
                    Path = "/src/File2.py",
                    Language = "python",
                    MainSymbols = new List<string> { "function1", "Class2" }
                }
            }
        };

        // Act
        foreach (var kvp in files)
        {
            codeMap.Files[kvp.Key] = kvp.Value;
        }

        // Assert
        Assert.Equal(2, codeMap.Files.Count);
        Assert.True(codeMap.Files.ContainsKey("/src/File1.cs"));
        Assert.True(codeMap.Files.ContainsKey("/src/File2.py"));
        Assert.Equal("csharp", codeMap.Files["/src/File1.cs"].Language);
        Assert.Equal("python", codeMap.Files["/src/File2.py"].Language);
    }

    [Fact]
    public void CodeMap_AddDependencies_ShouldAddToList()
    {
        // Arrange
        var codeMap = new CodeMap();
        var dependencies = new List<Dependency>
        {
            new Dependency
            {
                FromFile = "/src/Service.cs",
                ToFile = "/src/Repository.cs",
                Type = "import"
            },
            new Dependency
            {
                FromFile = "/src/Controller.cs",
                ToFile = "/src/Service.cs",
                Type = "import"
            }
        };

        // Act
        codeMap.Dependencies.AddRange(dependencies);

        // Assert
        Assert.Equal(2, codeMap.Dependencies.Count);
        Assert.Contains(codeMap.Dependencies, d => d.FromFile == "/src/Service.cs" && d.ToFile == "/src/Repository.cs");
        Assert.Contains(codeMap.Dependencies, d => d.FromFile == "/src/Controller.cs" && d.ToFile == "/src/Service.cs");
    }

    [Fact]
    public void CodeMap_AddSymbolReferences_ShouldAddToDictionary()
    {
        // Arrange
        var codeMap = new CodeMap();
        
        // Act
        codeMap.SymbolReferences["MyClass"] = new List<string> { "/src/File1.cs", "/src/File2.cs" };
        codeMap.SymbolReferences["MyMethod"] = new List<string> { "/src/File1.cs", "/src/File3.cs", "/src/File4.cs" };

        // Assert
        Assert.Equal(2, codeMap.SymbolReferences.Count);
        Assert.Equal(2, codeMap.SymbolReferences["MyClass"].Count);
        Assert.Equal(3, codeMap.SymbolReferences["MyMethod"].Count);
        Assert.Contains("/src/File2.cs", codeMap.SymbolReferences["MyClass"]);
    }

    [Fact]
    public void CodeMap_ComplexScenario_ShouldHandleCompleteCodeMap()
    {
        // Arrange
        var codeMap = new CodeMap();
        
        // Add files
        codeMap.Files["/src/OrderService.cs"] = new FileOverview
        {
            Path = "/src/OrderService.cs",
            Language = "csharp",
            MainSymbols = new List<string> { "OrderService", "ProcessOrder", "ValidateOrder" },
            Imports = new List<string> { "System", "OrderRepository", "EmailService" },
            Exports = new List<string> { "OrderService" }
        };
        
        codeMap.Files["/src/OrderRepository.cs"] = new FileOverview
        {
            Path = "/src/OrderRepository.cs",
            Language = "csharp",
            MainSymbols = new List<string> { "OrderRepository", "GetOrder", "SaveOrder" },
            Imports = new List<string> { "System.Data", "DatabaseContext" },
            Exports = new List<string> { "OrderRepository" }
        };

        // Add dependencies
        codeMap.Dependencies.Add(new Dependency
        {
            FromFile = "/src/OrderService.cs",
            ToFile = "/src/OrderRepository.cs",
            Type = "import"
        });

        // Add symbol references
        codeMap.SymbolReferences["OrderService"] = new List<string> { "/src/OrderController.cs", "/tests/OrderServiceTests.cs" };
        codeMap.SymbolReferences["OrderRepository"] = new List<string> { "/src/OrderService.cs", "/tests/OrderRepositoryTests.cs" };

        // Assert
        Assert.Equal(2, codeMap.Files.Count);
        Assert.Single(codeMap.Dependencies);
        Assert.Equal(2, codeMap.SymbolReferences.Count);
        
        // Verify file details
        var orderServiceFile = codeMap.Files["/src/OrderService.cs"];
        Assert.Equal(3, orderServiceFile.MainSymbols.Count);
        Assert.Contains("ProcessOrder", orderServiceFile.MainSymbols);
        
        // Verify dependency
        var dependency = codeMap.Dependencies.First();
        Assert.Equal("/src/OrderService.cs", dependency.FromFile);
        Assert.Equal("/src/OrderRepository.cs", dependency.ToFile);
        
        // Verify symbol references
        Assert.Contains("/src/OrderController.cs", codeMap.SymbolReferences["OrderService"]);
        Assert.Equal(2, codeMap.SymbolReferences["OrderRepository"].Count);
    }
}

public class FileOverviewTests
{
    [Fact]
    public void FileOverview_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var fileOverview = new FileOverview();

        // Assert
        Assert.Equal(string.Empty, fileOverview.Path);
        Assert.Equal(string.Empty, fileOverview.Language);
        Assert.NotNull(fileOverview.MainSymbols);
        Assert.Empty(fileOverview.MainSymbols);
        Assert.NotNull(fileOverview.Imports);
        Assert.Empty(fileOverview.Imports);
        Assert.NotNull(fileOverview.Exports);
        Assert.Empty(fileOverview.Exports);
    }

    [Fact]
    public void FileOverview_SetProperties_ShouldSetCorrectly()
    {
        // Arrange
        var fileOverview = new FileOverview();

        // Act
        fileOverview.Path = "/src/MyFile.cs";
        fileOverview.Language = "csharp";
        fileOverview.MainSymbols.AddRange(new[] { "Class1", "Method1", "Property1" });
        fileOverview.Imports.AddRange(new[] { "System", "System.Linq" });
        fileOverview.Exports.AddRange(new[] { "Class1" });

        // Assert
        Assert.Equal("/src/MyFile.cs", fileOverview.Path);
        Assert.Equal("csharp", fileOverview.Language);
        Assert.Equal(3, fileOverview.MainSymbols.Count);
        Assert.Equal(2, fileOverview.Imports.Count);
        Assert.Single(fileOverview.Exports);
        Assert.Contains("Method1", fileOverview.MainSymbols);
        Assert.Contains("System.Linq", fileOverview.Imports);
    }
}

public class DependencyTests
{
    [Fact]
    public void Dependency_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var dependency = new Dependency();

        // Assert
        Assert.Equal(string.Empty, dependency.FromFile);
        Assert.Equal(string.Empty, dependency.ToFile);
        Assert.Equal(string.Empty, dependency.Type);
    }

    [Fact]
    public void Dependency_SetProperties_ShouldSetCorrectly()
    {
        // Arrange
        var dependency = new Dependency();

        // Act
        dependency.FromFile = "/src/ServiceA.cs";
        dependency.ToFile = "/src/ServiceB.cs";
        dependency.Type = "import";

        // Assert
        Assert.Equal("/src/ServiceA.cs", dependency.FromFile);
        Assert.Equal("/src/ServiceB.cs", dependency.ToFile);
        Assert.Equal("import", dependency.Type);
    }

    [Fact]
    public void Dependency_DifferentTypes_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var importDep = new Dependency { FromFile = "A", ToFile = "B", Type = "import" };
        var inheritanceDep = new Dependency { FromFile = "C", ToFile = "D", Type = "inheritance" };
        var referenceDep = new Dependency { FromFile = "E", ToFile = "F", Type = "reference" };

        // Assert
        Assert.Equal("import", importDep.Type);
        Assert.Equal("inheritance", inheritanceDep.Type);
        Assert.Equal("reference", referenceDep.Type);
    }
}