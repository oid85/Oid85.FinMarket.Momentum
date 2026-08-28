using Microsoft.Extensions.DependencyInjection;
using Oid85.FinMarket.Algo.Application.Factories;
using Oid85.FinMarket.Algo.Application.Interfaces.Factories;
using Oid85.FinMarket.Algo.Application.Interfaces.Services;
using Oid85.FinMarket.Algo.Application.Services;
using Oid85.FinMarket.Algo.Application.Strategies;
using Oid85.FinMarket.Algo.Core.Models;

namespace Oid85.FinMarket.Algo.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static void ConfigureApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAlgoService, AlgoService>();
        services.AddScoped<IDataService, DataService>();
        services.AddScoped<IMonitorService, MonitorService>();

        services.AddScoped<IIndicatorFactory, IndicatorFactory>();

        services.AddKeyedTransient<Strategy, UltimateSmootherLong>(nameof(UltimateSmootherLong));        
        services.AddKeyedTransient<Strategy, SupertrendLong>(nameof(SupertrendLong));
        services.AddKeyedTransient<Strategy, NormalizedMomentumLong>(nameof(NormalizedMomentumLong));
        services.AddKeyedTransient<Strategy, MomentumMonthLong>(nameof(MomentumMonthLong));
        services.AddKeyedTransient<Strategy, MomentumWeekLong>(nameof(MomentumWeekLong));
        services.AddKeyedTransient<Strategy, HmaLong>(nameof(HmaLong));
        services.AddKeyedTransient<Strategy, DonchianBreakoutClassicLong>(nameof(DonchianBreakoutClassicLong));
        services.AddKeyedTransient<Strategy, DonchianBreakoutMiddleLong>(nameof(DonchianBreakoutMiddleLong));
        services.AddKeyedTransient<Strategy, VolatilityBreakoutClassicLong>(nameof(VolatilityBreakoutClassicLong));
        services.AddKeyedTransient<Strategy, VolatilityBreakoutMiddleLong>(nameof(VolatilityBreakoutMiddleLong));
    }
}