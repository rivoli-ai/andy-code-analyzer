using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Andy.CodeAnalyzer.Analyzers;
using Andy.CodeAnalyzer.Models;
using Andy.CodeAnalyzer.Services;
using Andy.CodeAnalyzer.Storage;

namespace Andy.CodeAnalyzer.Extensions;

/// <summary>
/// Extension methods for configuring code analyzer services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds code analyzer services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">An action to configure the options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCodeAnalyzer(
        this IServiceCollection services,
        Action<CodeAnalyzerOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<CodeAnalyzerOptions>(options => { }); // Use defaults
        }

        // Add database context
        services.AddDbContext<CodeAnalyzerDbContext>((serviceProvider, options) =>
        {
            var analyzerOptions = serviceProvider.GetRequiredService<IOptions<CodeAnalyzerOptions>>().Value;
            options.UseSqlite(analyzerOptions.DatabaseConnectionString);
        });

        // Add core services as Scoped to work with DbContext
        services.AddScoped<ICodeAnalyzerService, CodeAnalyzerService>();
        services.AddScoped<IIndexingService, IndexingService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<ICodeContextProvider, ContextProviderService>();

        // Add language analyzers (these can be singleton as they don't use DbContext)
        services.AddSingleton<ILanguageAnalyzer, CSharpAnalyzer>();
        services.AddSingleton<ILanguageAnalyzer, PythonAnalyzer>();
        // TODO: Add more language analyzers as they are implemented
        // services.AddSingleton<ILanguageAnalyzer, JavaScriptAnalyzer>();

        return services;
    }
}