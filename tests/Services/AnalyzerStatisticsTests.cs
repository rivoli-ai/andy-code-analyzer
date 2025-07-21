using System;
using System.Collections.Generic;
using Andy.CodeAnalyzer.Services;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Services;

public class AnalyzerStatisticsTests
{
    [Fact]
    public void AnalyzerStatistics_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var stats = new AnalyzerStatistics();

        // Assert
        Assert.Equal(0, stats.TotalFiles);
        Assert.Equal(0, stats.TotalSymbols);
        Assert.Equal(0L, stats.MemoryUsage);
        Assert.Equal(TimeSpan.Zero, stats.IndexingTime);
        Assert.Equal(0L, stats.DatabaseSize);
        Assert.NotNull(stats.LanguageDistribution);
        Assert.Empty(stats.LanguageDistribution);
    }

    [Fact]
    public void AnalyzerStatistics_SetProperties_ShouldSetCorrectly()
    {
        // Arrange
        var stats = new AnalyzerStatistics();
        var indexingTime = TimeSpan.FromMinutes(3.5);

        // Act
        stats.TotalFiles = 250;
        stats.TotalSymbols = 3500;
        stats.MemoryUsage = 1024 * 1024 * 50; // 50 MB
        stats.IndexingTime = indexingTime;
        stats.DatabaseSize = 1024 * 1024 * 10; // 10 MB

        // Assert
        Assert.Equal(250, stats.TotalFiles);
        Assert.Equal(3500, stats.TotalSymbols);
        Assert.Equal(52428800L, stats.MemoryUsage);
        Assert.Equal(indexingTime, stats.IndexingTime);
        Assert.Equal(10485760L, stats.DatabaseSize);
    }

    [Fact]
    public void AnalyzerStatistics_LanguageDistribution_ShouldHandleMultipleLanguages()
    {
        // Arrange
        var stats = new AnalyzerStatistics();

        // Act
        stats.LanguageDistribution["csharp"] = 150;
        stats.LanguageDistribution["python"] = 75;
        stats.LanguageDistribution["javascript"] = 50;
        stats.LanguageDistribution["typescript"] = 25;

        // Assert
        Assert.Equal(4, stats.LanguageDistribution.Count);
        Assert.Equal(150, stats.LanguageDistribution["csharp"]);
        Assert.Equal(75, stats.LanguageDistribution["python"]);
        Assert.Equal(50, stats.LanguageDistribution["javascript"]);
        Assert.Equal(25, stats.LanguageDistribution["typescript"]);
        
        // Verify total matches TotalFiles
        var totalFromDistribution = 0;
        foreach (var count in stats.LanguageDistribution.Values)
        {
            totalFromDistribution += count;
        }
        Assert.Equal(300, totalFromDistribution);
    }

    [Fact]
    public void AnalyzerStatistics_ComplexScenario_LargeCodebase()
    {
        // Arrange & Act
        var stats = new AnalyzerStatistics
        {
            TotalFiles = 5000,
            TotalSymbols = 125000,
            MemoryUsage = 1024L * 1024 * 512, // 512 MB
            IndexingTime = TimeSpan.FromMinutes(15.5),
            DatabaseSize = 1024L * 1024 * 200, // 200 MB
            LanguageDistribution = new Dictionary<string, int>
            {
                ["csharp"] = 3000,
                ["python"] = 1000,
                ["javascript"] = 500,
                ["typescript"] = 300,
                ["html"] = 150,
                ["css"] = 50
            }
        };

        // Assert
        Assert.Equal(5000, stats.TotalFiles);
        Assert.Equal(125000, stats.TotalSymbols);
        Assert.Equal(536870912L, stats.MemoryUsage);
        Assert.Equal(15.5, stats.IndexingTime.TotalMinutes);
        Assert.Equal(209715200L, stats.DatabaseSize);
        Assert.Equal(6, stats.LanguageDistribution.Count);
        Assert.Equal(3000, stats.LanguageDistribution["csharp"]);
    }

    [Fact]
    public void AnalyzerStatistics_EmptyProject_ShouldHandleZeroValues()
    {
        // Arrange & Act
        var stats = new AnalyzerStatistics
        {
            TotalFiles = 0,
            TotalSymbols = 0,
            MemoryUsage = 0,
            IndexingTime = TimeSpan.Zero,
            DatabaseSize = 0,
            LanguageDistribution = new Dictionary<string, int>()
        };

        // Assert
        Assert.Equal(0, stats.TotalFiles);
        Assert.Equal(0, stats.TotalSymbols);
        Assert.Equal(0L, stats.MemoryUsage);
        Assert.Equal(TimeSpan.Zero, stats.IndexingTime);
        Assert.Equal(0L, stats.DatabaseSize);
        Assert.Empty(stats.LanguageDistribution);
    }

    [Fact]
    public void AnalyzerStatistics_MaxValues_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var stats = new AnalyzerStatistics
        {
            TotalFiles = int.MaxValue,
            TotalSymbols = int.MaxValue,
            MemoryUsage = long.MaxValue,
            IndexingTime = TimeSpan.MaxValue,
            DatabaseSize = long.MaxValue
        };

        // Assert
        Assert.Equal(int.MaxValue, stats.TotalFiles);
        Assert.Equal(int.MaxValue, stats.TotalSymbols);
        Assert.Equal(long.MaxValue, stats.MemoryUsage);
        Assert.Equal(TimeSpan.MaxValue, stats.IndexingTime);
        Assert.Equal(long.MaxValue, stats.DatabaseSize);
    }

    [Fact]
    public void AnalyzerStatistics_PartialUpdate_ShouldMaintainOtherValues()
    {
        // Arrange
        var stats = new AnalyzerStatistics
        {
            TotalFiles = 100,
            TotalSymbols = 1000,
            MemoryUsage = 1024 * 1024,
            IndexingTime = TimeSpan.FromSeconds(30),
            DatabaseSize = 1024 * 512,
            LanguageDistribution = new Dictionary<string, int> { ["csharp"] = 100 }
        };

        var originalMemory = stats.MemoryUsage;
        var originalTime = stats.IndexingTime;
        var originalDistribution = stats.LanguageDistribution;

        // Act
        stats.TotalFiles = 200;
        stats.TotalSymbols = 2500;

        // Assert
        Assert.Equal(200, stats.TotalFiles);
        Assert.Equal(2500, stats.TotalSymbols);
        Assert.Equal(originalMemory, stats.MemoryUsage);
        Assert.Equal(originalTime, stats.IndexingTime);
        Assert.Same(originalDistribution, stats.LanguageDistribution);
        Assert.Equal(100, stats.LanguageDistribution["csharp"]);
    }

    [Fact]
    public void AnalyzerStatistics_ReplaceLanguageDistribution_ShouldWork()
    {
        // Arrange
        var stats = new AnalyzerStatistics
        {
            LanguageDistribution = new Dictionary<string, int>
            {
                ["csharp"] = 50,
                ["python"] = 30
            }
        };

        // Act
        stats.LanguageDistribution = new Dictionary<string, int>
        {
            ["javascript"] = 100,
            ["typescript"] = 80
        };

        // Assert
        Assert.Equal(2, stats.LanguageDistribution.Count);
        Assert.False(stats.LanguageDistribution.ContainsKey("csharp"));
        Assert.False(stats.LanguageDistribution.ContainsKey("python"));
        Assert.Equal(100, stats.LanguageDistribution["javascript"]);
        Assert.Equal(80, stats.LanguageDistribution["typescript"]);
    }

    [Fact]
    public void AnalyzerStatistics_CalculateAverages_ShouldBeCorrect()
    {
        // Arrange
        var stats = new AnalyzerStatistics
        {
            TotalFiles = 1000,
            TotalSymbols = 50000,
            MemoryUsage = 1024L * 1024 * 100, // 100 MB
            IndexingTime = TimeSpan.FromMinutes(5),
            DatabaseSize = 1024L * 1024 * 50 // 50 MB
        };

        // Act
        var avgSymbolsPerFile = stats.TotalFiles > 0 ? (double)stats.TotalSymbols / stats.TotalFiles : 0;
        var avgMemoryPerFile = stats.TotalFiles > 0 ? (double)stats.MemoryUsage / stats.TotalFiles : 0;
        var avgTimePerFile = stats.TotalFiles > 0 ? stats.IndexingTime.TotalMilliseconds / stats.TotalFiles : 0;

        // Assert
        Assert.Equal(50.0, avgSymbolsPerFile);
        Assert.Equal(104857.6, avgMemoryPerFile, 1);
        Assert.Equal(300.0, avgTimePerFile, 1); // 5 minutes = 300000ms / 1000 files = 300ms per file
    }
}