using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Oid85.FinMarket.Algo.Application.Interfaces.Repositories;
using Oid85.FinMarket.Algo.Core.Configuration;
using Oid85.FinMarket.Algo.Core.Models;
using Oid85.FinMarket.Algo.Infrastructure.Database.Entities;

namespace Oid85.FinMarket.Algo.Infrastructure.Database.Repositories
{
    public class StrategyExecuteResultRepository(
        IOptions<AlgoSettings> options,
        IDbContextFactory<AlgoContext> contextFactory) 
        : IStrategyExecuteResultRepository
    {
        public async Task AddAsync(List<StrategyExecuteResult> strategyExecuteResults)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            if (strategyExecuteResults is []) return;

            var entities = strategyExecuteResults.Select(Map);

            await context.StrategyExecuteResultEntities.AddRangeAsync(entities);
            await context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string portfolioName, string processName)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            await context.StrategyExecuteResultEntities
                .Where(x => x.PortfolioName == portfolioName)
                .Where(x => x.ProcessName == processName)
                .ExecuteDeleteAsync();

            await context.SaveChangesAsync();
        }

        public async Task<List<StrategyExecuteResult>> GetAsync(string portfolioName, string strategyName, string processName)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var queryableEntities = context.StrategyExecuteResultEntities.AsQueryable();

            queryableEntities = queryableEntities.Where(x => x.ProcessName == processName);
            queryableEntities = queryableEntities.Where(x => x.PortfolioName == portfolioName);
            queryableEntities = queryableEntities.Where(x => x.StrategyName == strategyName);

            var entities = await queryableEntities.AsNoTracking().ToListAsync();

            var models = entities.Select(Map).ToList();

            return models;
        }

        public async Task<List<StrategyExecuteResult>> GetFilteredAsync()
        {
            var algoSettings = options.Value;

            await using var context = await contextFactory.CreateDbContextAsync();

            var queryableEntities = context.StrategyExecuteResultEntities.AsQueryable();

            queryableEntities = queryableEntities.Where(x => x.ProfitFactor >= algoSettings.StrategyExecuteResultFilter.MinProfitFactor);
            queryableEntities = queryableEntities.Where(x => x.RecoveryFactor >= algoSettings.StrategyExecuteResultFilter.MinRecoveryFactor);
            queryableEntities = queryableEntities.Where(x => x.WinningTradesPercent >= algoSettings.StrategyExecuteResultFilter.MinWinningTradesPercent);
            queryableEntities = queryableEntities.Where(x => x.WinningTradesPercent <= algoSettings.StrategyExecuteResultFilter.MaxWinningTradesPercent);
            queryableEntities = queryableEntities.Where(x => x.AnnualYieldReturn >= algoSettings.StrategyExecuteResultFilter.MinAnnualYieldReturn);
            queryableEntities = queryableEntities.Where(x => x.MaxDrawdownPercent <= algoSettings.StrategyExecuteResultFilter.MaxDrawdownPercent);

            var entities = await queryableEntities.AsNoTracking().ToListAsync();

            var models = entities.Select(Map).ToList();

            return models;
        }

        private static StrategyExecuteResult Map(StrategyExecuteResultEntity entity) => 
            new()
            {
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Ticker = entity.Ticker,
                StrategyDescription = entity.StrategyDescription,
                PortfolioName = entity.PortfolioName,
                ProcessName = entity.ProcessName,
                StrategyName = entity.StrategyName,
                StrategyParams = entity.StrategyParams,
                StrategyParamsHash = entity.StrategyParamsHash,
                NumberPositions = entity.NumberPositions,
                CurrentPosition = entity.CurrentPosition,
                CurrentPositionCost = entity.CurrentPositionCost,
                ProfitFactor = entity.ProfitFactor,
                RecoveryFactor = entity.RecoveryFactor,
                TotalNetProfit = entity.NetProfit,
                AverageNetProfit = entity.AverageNetProfit,
                AverageNetProfitPercent = entity.AverageNetProfitPercent,
                Drawdown = entity.Drawdown,
                MaxDrawdown = entity.MaxDrawdown,
                MaxDrawdownPercent = entity.MaxDrawdownPercent,
                WinningPositions = entity.WinningPositions,
                WinningTradesPercent = entity.WinningTradesPercent,
                StartMoney = entity.StartMoney,
                EndMoney = entity.EndMoney,
                TotalReturn = entity.TotalReturn,
                AnnualYieldReturn = entity.AnnualYieldReturn,
                ResultMessage = entity.ResultMessage
            };

        private static StrategyExecuteResultEntity Map(StrategyExecuteResult model) =>
            new()
            {
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Ticker = model.Ticker,
                StrategyDescription = model.StrategyDescription,
                PortfolioName = model.PortfolioName,
                ProcessName = model.ProcessName,
                StrategyName = model.StrategyName,
                StrategyParams = model.StrategyParams,
                StrategyParamsHash = model.StrategyParamsHash,
                NumberPositions = model.NumberPositions,
                CurrentPosition = model.CurrentPosition,
                CurrentPositionCost = model.CurrentPositionCost,
                ProfitFactor = model.ProfitFactor,
                RecoveryFactor = model.RecoveryFactor,
                NetProfit = model.TotalNetProfit,
                AverageNetProfit = model.AverageNetProfit,
                AverageNetProfitPercent = model.AverageNetProfitPercent,
                Drawdown = model.Drawdown,
                MaxDrawdown = model.MaxDrawdown,
                MaxDrawdownPercent = model.MaxDrawdownPercent,
                WinningPositions = model.WinningPositions,
                WinningTradesPercent = model.WinningTradesPercent,
                StartMoney = model.StartMoney,
                EndMoney = model.EndMoney,
                TotalReturn = model.TotalReturn,
                AnnualYieldReturn = model.AnnualYieldReturn,
                ResultMessage = model.ResultMessage
            };
    }
}
