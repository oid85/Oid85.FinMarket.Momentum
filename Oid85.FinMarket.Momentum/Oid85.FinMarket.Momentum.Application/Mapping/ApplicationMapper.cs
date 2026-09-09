using Oid85.FinMarket.Momentum.Application.Helpers;
using Oid85.FinMarket.Momentum.Application.Models;
using Oid85.FinMarket.Momentum.Core.Responses;

namespace Oid85.FinMarket.Momentum.Application.Mapping;

public static class ApplicationMapper
{
    public static MonitorResponse Map(MomentumStrategy strategy) => 
        new ()
        {
            ProtocolMessages = [.. strategy.ProtocolMessages.OrderByDescending(x => x.Date)],
            TotalSumLife = strategy.TotalSumLife,
            BacktestSeries = [strategy.EquitySeries, strategy.MoneySeries, strategy.DrawdownSeries],
            PriceDynamicSeries = strategy.GetPriceDynamicSeries(),
            CurrentPositions = strategy.GetCurrentPositions(),
            Yield = DiagramSeriesHelper.GetAnnualPercentageYield(strategy.EquitySeries),
            Yield2021 = DiagramSeriesHelper.GetAnnualPercentageYield(strategy.EquitySeries, 2021),
            Yield2022 = DiagramSeriesHelper.GetAnnualPercentageYield(strategy.EquitySeries, 2022),
            Yield2023 = DiagramSeriesHelper.GetAnnualPercentageYield(strategy.EquitySeries, 2023),
            Yield2024 = DiagramSeriesHelper.GetAnnualPercentageYield(strategy.EquitySeries, 2024),
            Yield2025 = DiagramSeriesHelper.GetAnnualPercentageYield(strategy.EquitySeries, 2025),
            Yield2026 = DiagramSeriesHelper.GetAnnualPercentageYield(strategy.EquitySeries, 2026),
            MaxDrawdown = strategy.MaxDrawdown,
            CurrentDrawdown = strategy.CurrentDrawdown
        };
}