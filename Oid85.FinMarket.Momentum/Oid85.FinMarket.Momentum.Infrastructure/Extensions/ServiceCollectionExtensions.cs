using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oid85.FinMarket.Momentum.Application.Interfaces.ApiClients;
using Oid85.FinMarket.Momentum.Application.Interfaces.Repositories;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Infrastructure.ApiClients;
using Oid85.FinMarket.Momentum.Infrastructure.Database;
using Oid85.FinMarket.Momentum.Infrastructure.Database.Repositories;

namespace Oid85.FinMarket.Momentum.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {    
        services.AddDbContextPool<MomentumContext>((serviceProvider, options) =>
        {  
            options.UseNpgsql(configuration.GetValue<string>(KnownSettingsKeys.PostgresMomentumConnectionString));
        });

        services.AddPooledDbContextFactory<MomentumContext>(options =>
            options
                .UseNpgsql(configuration.GetValue<string>(KnownSettingsKeys.PostgresMomentumConnectionString))
                .EnableServiceProviderCaching(false), poolSize: 32);

        services.AddScoped<IParameterRepository, ParameterRepository>();
    }

    public static void ConfigureStorageApiClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient(KnownHttpClients.FinMarketStorageServiceApiClient, client =>
        {
            string baseUrl = configuration.GetValue<string>(KnownSettingsKeys.FinMarketStorageServiceApiClientBaseAddress)!;
            client.BaseAddress = new Uri(baseUrl);
        });

        services.AddScoped<IStorageApiClient, StorageApiClient>();
    }

    public static async Task ApplyMigrations(this IHost host)
    {
        var scopeFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
        await using var scope = scopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<MomentumContext>();
        await context.Database.MigrateAsync();
    }
}