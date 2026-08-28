using System.Text.Json;
using Oid85.FinMarket.Algo.Common.Utils;
using Oid85.FinMarket.Algo.Core.Models;
using Skender.Stock.Indicators;

namespace Oid85.FinMarket.Algo.Application.Mapping;

public static class ApplicationMapper
{
    public static Quote Map(Candle model) =>
        new()
        {
            Open = Convert.ToDecimal(model.Open),
            Close = Convert.ToDecimal(model.Close),
            High = Convert.ToDecimal(model.High),
            Low = Convert.ToDecimal(model.Low),
            Date = model.Date.ToDateTime(TimeOnly.MinValue)
        };

    public static StrategyExecuteResult MapToStrategyExecuteResult(Strategy strategy)
    {
        var json = JsonSerializer.Serialize(strategy.Parameters);

        var result = new StrategyExecuteResult
        {
            StartDate = strategy.StartDate,
            EndDate = strategy.EndDate,
            Ticker = strategy.Ticker,
            StrategyDescription = strategy.StrategyDescription,
            PortfolioName = strategy.PortfolioName,
            ProcessName = strategy.ProcessName,
            StrategyName = strategy.StrategyName,
            StrategyParams = json,
            StrategyParamsHash = StringUtils.GetMd5(json),
            NumberPositions = strategy.NumberPositions,
            CurrentPosition = strategy.CurrentPosition,
            LastActivePosition = strategy.LastActivePosition,
            LastPosition = strategy.LastPosition,
            CurrentPositionCost = strategy.CurrentPositionCost,
            ProfitFactor = strategy.ProfitFactor,
            RecoveryFactor = strategy.RecoveryFactor,
            TotalNetProfit = strategy.TotalNetProfit,
            AverageNetProfit = strategy.AverageNetProfit,
            AverageNetProfitPercent = strategy.AverageNetProfitPercent,
            Drawdown = strategy.Drawdown,
            MaxDrawdown = strategy.MaxDrawdown,
            MaxDrawdownPercent = strategy.MaxDrawdownPercent,
            WinningPositions = strategy.WinningPositions,
            WinningTradesPercent = strategy.WinningTradesPercent,
            StartMoney = strategy.StartMoney,
            EndMoney = strategy.EndMoney,
            TotalReturn = strategy.TotalReturn,
            AnnualYieldReturn = strategy.AnnualYieldReturn,
            Positions = new SortedDictionary<DateOnly, Position>(strategy.Positions),
            EqiutyCurve = new Dictionary<DateOnly, double>(strategy.EqiutyCurve),
            DrawdownCurve = new Dictionary<DateOnly, double>(strategy.DrawdownCurve),
            DiagramPoints = [.. strategy.DiagramPoints]
        };

        return result;
    }
}