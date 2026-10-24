using AuralIndex.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuralIndex.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AuralIndexDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<IAlgorithmSettingsService, AlgorithmSettingsService>();
        services.AddScoped<IImportExportService, ImportExportService>();
        services.AddScoped<IBookSearchService, BookSearchService>();

        return services;
    }
}
