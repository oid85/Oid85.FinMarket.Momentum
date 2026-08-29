using Microsoft.Extensions.DependencyInjection;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Application.Services;

namespace Oid85.FinMarket.Momentum.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IDataService, DataService>();
        services.AddScoped<IMomentumService, MomentumService>();
    }
}