using Microsoft.Extensions.DependencyInjection;
using Oid85.FinMarket.Momentum.Application.Interfaces.Services;
using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Application.Services;
using Oid85.FinMarket.Momentum.Application.Strategies;

namespace Oid85.FinMarket.Momentum.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IDataService, DataService>();
        services.AddScoped<IMomentumService, MomentumService>();
        services.AddScoped<IBacktestService, BacktestService>();

        services.AddKeyedTransient<MomentumStrategy, MomentumStrategyVersion1>(nameof(MomentumStrategyVersion1));
    }
}