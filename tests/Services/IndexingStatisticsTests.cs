using System;
using Andy.CodeAnalyzer.Services;
using Xunit;

namespace Andy.CodeAnalyzer.Tests.Services;

public class IndexingStatisticsTests
{
    [Fact]
    public void IndexingStatistics_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var stats = new IndexingStatistics();

        // Assert
        Assert.Equal(0, stats.TotalFilesIndexed);
        Assert.Equal(0, stats.TotalSymbolsExtracted);
        Assert.Equal(TimeSpan.Zero, stats.IndexingDuration);
        Assert.Null(stats.LastIndexTime);
    }

    [Fact]
    public void IndexingStatistics_SetProperties_ShouldSetCorrectly()
    {
        // Arrange
        var stats = new IndexingStatistics();
        var testTime = DateTime.UtcNow;
        var testDuration = TimeSpan.FromMinutes(5);

        // Act
        stats.TotalFilesIndexed = 150;
        stats.TotalSymbolsExtracted = 1250;
        stats.IndexingDuration = testDuration;
        stats.LastIndexTime = testTime;

        // Assert
        Assert.Equal(150, stats.TotalFilesIndexed);
        Assert.Equal(1250, stats.TotalSymbolsExtracted);
        Assert.Equal(testDuration, stats.IndexingDuration);
        Assert.Equal(testTime, stats.LastIndexTime);
    }

    [Fact]
    public void IndexingStatistics_ComplexScenario_ShouldHandleAllProperties()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // Act
        var stats = new IndexingStatistics
        {
            TotalFilesIndexed = 500,
            TotalSymbolsExtracted = 5432,
            IndexingDuration = duration,
            LastIndexTime = endTime
        };

        // Assert
        Assert.Equal(500, stats.TotalFilesIndexed);
        Assert.Equal(5432, stats.TotalSymbolsExtracted);
        Assert.True(stats.IndexingDuration.TotalMinutes >= 9.9 && stats.IndexingDuration.TotalMinutes <= 10.1);
        Assert.NotNull(stats.LastIndexTime);
        Assert.True(stats.LastIndexTime.Value <= DateTime.UtcNow);
    }

    [Fact]
    public void IndexingStatistics_ZeroFiles_ShouldBeValid()
    {
        // Arrange & Act
        var stats = new IndexingStatistics
        {
            TotalFilesIndexed = 0,
            TotalSymbolsExtracted = 0,
            IndexingDuration = TimeSpan.Zero,
            LastIndexTime = null
        };

        // Assert
        Assert.Equal(0, stats.TotalFilesIndexed);
        Assert.Equal(0, stats.TotalSymbolsExtracted);
        Assert.Equal(TimeSpan.Zero, stats.IndexingDuration);
        Assert.Null(stats.LastIndexTime);
    }

    [Fact]
    public void IndexingStatistics_LargeNumbers_ShouldHandleCorrectly()
    {
        // Arrange & Act
        var stats = new IndexingStatistics
        {
            TotalFilesIndexed = int.MaxValue,
            TotalSymbolsExtracted = int.MaxValue,
            IndexingDuration = TimeSpan.MaxValue,
            LastIndexTime = DateTime.MaxValue
        };

        // Assert
        Assert.Equal(int.MaxValue, stats.TotalFilesIndexed);
        Assert.Equal(int.MaxValue, stats.TotalSymbolsExtracted);
        Assert.Equal(TimeSpan.MaxValue, stats.IndexingDuration);
        Assert.Equal(DateTime.MaxValue, stats.LastIndexTime);
    }

    [Fact]
    public void IndexingStatistics_PartialUpdate_ShouldMaintainOtherValues()
    {
        // Arrange
        var stats = new IndexingStatistics
        {
            TotalFilesIndexed = 100,
            TotalSymbolsExtracted = 1000,
            IndexingDuration = TimeSpan.FromMinutes(2),
            LastIndexTime = DateTime.UtcNow.AddHours(-1)
        };

        var originalTime = stats.LastIndexTime;

        // Act
        stats.TotalFilesIndexed = 200;
        stats.TotalSymbolsExtracted = 2000;

        // Assert
        Assert.Equal(200, stats.TotalFilesIndexed);
        Assert.Equal(2000, stats.TotalSymbolsExtracted);
        Assert.Equal(TimeSpan.FromMinutes(2), stats.IndexingDuration);
        Assert.Equal(originalTime, stats.LastIndexTime);
    }
}