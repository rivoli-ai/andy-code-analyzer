using Andy.CodeAnalyzer.Storage.Entities;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Storage.Entities;

public class DependencyEntityTests
{
    [Fact]
    public void DependencyEntity_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var dependency = new DependencyEntity();

        // Assert
        Assert.Equal(0, dependency.Id);
        Assert.Equal(0, dependency.FromFileId);
        Assert.Equal(0, dependency.ToFileId);
        Assert.Equal(string.Empty, dependency.DependencyType);
        Assert.Null(dependency.FromFile!);
        Assert.Null(dependency.ToFile!);
    }

    [Fact]
    public void DependencyEntity_SetProperties_ShouldSetCorrectly()
    {
        // Arrange
        var dependency = new DependencyEntity();
        var fromFile = new FileEntity { Id = 1, Path = "/src/ServiceA.cs" };
        var toFile = new FileEntity { Id = 2, Path = "/src/ServiceB.cs" };

        // Act
        dependency.Id = 42;
        dependency.FromFileId = 1;
        dependency.ToFileId = 2;
        dependency.DependencyType = "import";
        dependency.FromFile = fromFile;
        dependency.ToFile = toFile;

        // Assert
        Assert.Equal(42, dependency.Id);
        Assert.Equal(1, dependency.FromFileId);
        Assert.Equal(2, dependency.ToFileId);
        Assert.Equal("import", dependency.DependencyType);
        Assert.NotNull(dependency.FromFile);
        Assert.Equal("/src/ServiceA.cs", dependency.FromFile.Path);
        Assert.NotNull(dependency.ToFile);
        Assert.Equal("/src/ServiceB.cs", dependency.ToFile.Path);
    }

    [Fact]
    public void DependencyEntity_DifferentDependencyTypes_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var importDep = new DependencyEntity { DependencyType = "import" };
        var includeDep = new DependencyEntity { DependencyType = "include" };
        var inheritanceDep = new DependencyEntity { DependencyType = "inheritance" };
        var referenceDep = new DependencyEntity { DependencyType = "reference" };

        // Assert
        Assert.Equal("import", importDep.DependencyType);
        Assert.Equal("include", includeDep.DependencyType);
        Assert.Equal("inheritance", inheritanceDep.DependencyType);
        Assert.Equal("reference", referenceDep.DependencyType);
    }

    [Fact]
    public void DependencyEntity_ComplexScenario_ShouldHandleAllProperties()
    {
        // Arrange
        var sourceFile = new FileEntity 
        { 
            Id = 10, 
            Path = "/src/Controllers/UserController.cs",
            Language = "csharp",
            LastModified = System.DateTime.UtcNow.AddDays(-1)
        };
        
        var targetFile = new FileEntity 
        { 
            Id = 20, 
            Path = "/src/Services/UserService.cs",
            Language = "csharp",
            LastModified = System.DateTime.UtcNow.AddDays(-2)
        };

        // Act
        var dependency = new DependencyEntity
        {
            Id = 100,
            FromFileId = sourceFile.Id,
            FromFile = sourceFile,
            ToFileId = targetFile.Id,
            ToFile = targetFile,
            DependencyType = "import"
        };

        // Assert
        Assert.Equal(100, dependency.Id);
        Assert.Equal(10, dependency.FromFileId);
        Assert.Equal(20, dependency.ToFileId);
        Assert.Equal("import", dependency.DependencyType);
        
        // Verify file relationships
        Assert.NotNull(dependency.FromFile);
        Assert.Equal("/src/Controllers/UserController.cs", dependency.FromFile.Path);
        Assert.Equal("csharp", dependency.FromFile.Language);
        
        Assert.NotNull(dependency.ToFile);
        Assert.Equal("/src/Services/UserService.cs", dependency.ToFile.Path);
        Assert.Equal("csharp", dependency.ToFile.Language);
    }

    [Fact]
    public void DependencyEntity_SelfReference_ShouldBeAllowed()
    {
        // Arrange
        var file = new FileEntity { Id = 5, Path = "/src/RecursiveClass.cs" };

        // Act
        var dependency = new DependencyEntity
        {
            Id = 1,
            FromFileId = 5,
            FromFile = file,
            ToFileId = 5,
            ToFile = file,
            DependencyType = "self-reference"
        };

        // Assert
        Assert.Equal(dependency.FromFileId, dependency.ToFileId);
        Assert.Same(dependency.FromFile, dependency.ToFile);
        Assert.Equal("self-reference", dependency.DependencyType);
    }

    [Fact]
    public void DependencyEntity_NullFileEntities_ShouldBeHandled()
    {
        // Arrange & Act
        var dependency = new DependencyEntity
        {
            Id = 1,
            FromFileId = 10,
            ToFileId = 20,
            DependencyType = "import",
            FromFile = null!,
            ToFile = null!
        };

        // Assert
        Assert.Equal(1, dependency.Id);
        Assert.Equal(10, dependency.FromFileId);
        Assert.Equal(20, dependency.ToFileId);
        Assert.Equal("import", dependency.DependencyType);
        Assert.Null(dependency.FromFile);
        Assert.Null(dependency.ToFile);
    }
}