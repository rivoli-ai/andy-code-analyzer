using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Analyzers;

public class ReferenceTests
{
    [Fact]
    public void Reference_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var reference = new Reference();

        // Assert
        Assert.Equal(string.Empty, reference.FilePath);
        Assert.NotNull(reference.Location);
        Assert.Equal(0, reference.Location.StartLine);
        Assert.Equal(0, reference.Location.StartColumn);
        Assert.Equal(ReferenceKind.Usage, reference.Kind); // Default enum value
        Assert.Equal(string.Empty, reference.ContextSnippet);
    }

    [Fact]
    public void Reference_SetProperties_ShouldSetCorrectly()
    {
        // Arrange
        var reference = new Reference();
        var location = new Location 
        { 
            StartLine = 10, 
            StartColumn = 5, 
            EndLine = 10, 
            EndColumn = 25 
        };

        // Act
        reference.FilePath = "/src/MyClass.cs";
        reference.Location = location;
        reference.Kind = ReferenceKind.Definition;
        reference.ContextSnippet = "public class MyClass { }";

        // Assert
        Assert.Equal("/src/MyClass.cs", reference.FilePath);
        Assert.Equal(10, reference.Location.StartLine);
        Assert.Equal(5, reference.Location.StartColumn);
        Assert.Equal(10, reference.Location.EndLine);
        Assert.Equal(25, reference.Location.EndColumn);
        Assert.Equal(ReferenceKind.Definition, reference.Kind);
        Assert.Equal("public class MyClass { }", reference.ContextSnippet);
    }

    [Fact]
    public void Reference_AllReferenceKinds_ShouldWorkCorrectly()
    {
        // Arrange & Act & Assert
        var usageRef = new Reference { Kind = ReferenceKind.Usage };
        Assert.Equal(ReferenceKind.Usage, usageRef.Kind);

        var definitionRef = new Reference { Kind = ReferenceKind.Definition };
        Assert.Equal(ReferenceKind.Definition, definitionRef.Kind);

        var implementationRef = new Reference { Kind = ReferenceKind.Implementation };
        Assert.Equal(ReferenceKind.Implementation, implementationRef.Kind);

        var overrideRef = new Reference { Kind = ReferenceKind.Override };
        Assert.Equal(ReferenceKind.Override, overrideRef.Kind);

        var inheritanceRef = new Reference { Kind = ReferenceKind.Inheritance };
        Assert.Equal(ReferenceKind.Inheritance, inheritanceRef.Kind);
    }

    [Fact]
    public void Reference_ComplexScenario_MethodUsage()
    {
        // Arrange & Act
        var reference = new Reference
        {
            FilePath = "/src/Services/OrderService.cs",
            Location = new Location 
            { 
                StartLine = 45, 
                StartColumn = 12, 
                EndLine = 45, 
                EndColumn = 35 
            },
            Kind = ReferenceKind.Usage,
            ContextSnippet = "var total = CalculateTotal(order);"
        };

        // Assert
        Assert.Equal("/src/Services/OrderService.cs", reference.FilePath);
        Assert.Equal(45, reference.Location.StartLine);
        Assert.Equal(12, reference.Location.StartColumn);
        Assert.Equal(ReferenceKind.Usage, reference.Kind);
        Assert.Contains("CalculateTotal", reference.ContextSnippet);
    }

    [Fact]
    public void Reference_ComplexScenario_ClassDefinition()
    {
        // Arrange & Act
        var reference = new Reference
        {
            FilePath = "/src/Models/Customer.cs",
            Location = new Location 
            { 
                StartLine = 5, 
                StartColumn = 1, 
                EndLine = 50, 
                EndColumn = 1 
            },
            Kind = ReferenceKind.Definition,
            ContextSnippet = "public class Customer : IEntity\n{\n    // Class implementation\n}"
        };

        // Assert
        Assert.Equal("/src/Models/Customer.cs", reference.FilePath);
        Assert.Equal(5, reference.Location.StartLine);
        Assert.Equal(50, reference.Location.EndLine);
        Assert.Equal(ReferenceKind.Definition, reference.Kind);
        Assert.Contains("public class Customer", reference.ContextSnippet);
    }

    [Fact]
    public void Reference_ComplexScenario_InterfaceImplementation()
    {
        // Arrange & Act
        var reference = new Reference
        {
            FilePath = "/src/Services/EmailService.cs",
            Location = new Location 
            { 
                StartLine = 15, 
                StartColumn = 5, 
                EndLine = 20, 
                EndColumn = 5 
            },
            Kind = ReferenceKind.Implementation,
            ContextSnippet = "public async Task SendAsync(string to, string subject, string body)\n{\n    // Implementation\n}"
        };

        // Assert
        Assert.Equal(ReferenceKind.Implementation, reference.Kind);
        Assert.Contains("SendAsync", reference.ContextSnippet);
    }

    [Fact]
    public void Reference_ComplexScenario_MethodOverride()
    {
        // Arrange & Act
        var reference = new Reference
        {
            FilePath = "/src/Models/PremiumCustomer.cs",
            Location = new Location 
            { 
                StartLine = 25, 
                StartColumn = 5, 
                EndLine = 30, 
                EndColumn = 5 
            },
            Kind = ReferenceKind.Override,
            ContextSnippet = "public override decimal CalculateDiscount()\n{\n    return base.CalculateDiscount() * 1.5m;\n}"
        };

        // Assert
        Assert.Equal(ReferenceKind.Override, reference.Kind);
        Assert.Contains("override", reference.ContextSnippet);
        Assert.Contains("CalculateDiscount", reference.ContextSnippet);
    }

    [Fact]
    public void Reference_ComplexScenario_ClassInheritance()
    {
        // Arrange & Act
        var reference = new Reference
        {
            FilePath = "/src/Models/Vehicle.cs",
            Location = new Location 
            { 
                StartLine = 3, 
                StartColumn = 1, 
                EndLine = 3, 
                EndColumn = 50 
            },
            Kind = ReferenceKind.Inheritance,
            ContextSnippet = "public abstract class Vehicle : IMovable, ISerializable"
        };

        // Assert
        Assert.Equal(ReferenceKind.Inheritance, reference.Kind);
        Assert.Contains("Vehicle", reference.ContextSnippet);
        Assert.Contains("IMovable", reference.ContextSnippet);
    }

    [Fact]
    public void Reference_EmptyContextSnippet_ShouldBeAllowed()
    {
        // Arrange & Act
        var reference = new Reference
        {
            FilePath = "/src/test.cs",
            Location = new Location { StartLine = 1 },
            Kind = ReferenceKind.Usage,
            ContextSnippet = ""
        };

        // Assert
        Assert.Equal(string.Empty, reference.ContextSnippet);
    }

    [Fact]
    public void Reference_LongFilePath_ShouldBeHandled()
    {
        // Arrange
        var longPath = "/very/long/path/to/deeply/nested/source/files/in/project/structure/MyVeryLongClassNameForTestingPurposes.cs";

        // Act
        var reference = new Reference
        {
            FilePath = longPath,
            Location = new Location { StartLine = 100, StartColumn = 50 },
            Kind = ReferenceKind.Definition
        };

        // Assert
        Assert.Equal(longPath, reference.FilePath);
        Assert.Equal(100, reference.Location.StartLine);
    }
}