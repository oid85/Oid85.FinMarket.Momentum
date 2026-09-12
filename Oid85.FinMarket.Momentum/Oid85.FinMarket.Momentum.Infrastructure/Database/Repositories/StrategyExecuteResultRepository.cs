using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Oid85.FinMarket.Momentum.Application.Interfaces.Repositories;
using Oid85.FinMarket.Momentum.Core.Configuration;
using Oid85.FinMarket.Momentum.Core.Models;
using Oid85.FinMarket.Momentum.Infrastructure.Database.Entities;

namespace Oid85.FinMarket.Momentum.Infrastructure.Database.Repositories
{
    public class StrategyExecuteResultRepository(
        IOptions<MomentumSettings> options,
        IDbContextFactory<MomentumContext> contextFactory) 
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

        public async Task DeleteAsync()
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            await context.StrategyExecuteResultEntities
                .ExecuteDeleteAsync();

            await context.SaveChangesAsync();
        }

        public async Task<List<StrategyExecuteResult>> GetAsync(string strategyName)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var queryableEntities = context.StrategyExecuteResultEntities.AsQueryable();

            queryableEntities = queryableEntities.Where(x => x.StrategyName == strategyName);

            var entities = await queryableEntities.AsNoTracking().ToListAsync();

            var models = entities.Select(Map).ToList();

            return models;
        }

        public async Task<List<StrategyExecuteResult>> GetFilteredAsync()
        {
            var momentumSettings = options.Value;

            await using var context = await contextFactory.CreateDbContextAsync();

            var queryableEntities = context.StrategyExecuteResultEntities.AsQueryable();

            queryableEntities = queryableEntities.Where(x => x.RecoveryFactor >= momentumSettings.StrategyExecuteResultFilter.MinRecoveryFactor);
            queryableEntities = queryableEntities.Where(x => x.AnnualYieldReturn >= momentumSettings.StrategyExecuteResultFilter.MinAnnualYieldReturn);
            queryableEntities = queryableEntities.Where(x => x.MaxDrawdownPercent <= momentumSettings.StrategyExecuteResultFilter.MaxDrawdownPercent);

            var entities = await queryableEntities.AsNoTracking().ToListAsync();

            var models = entities.Select(Map).ToList();

            return models;
        }

        private static StrategyExecuteResult Map(StrategyExecuteResultEntity entity) => 
            new()
            {
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                Tickers = JsonSerializer.Deserialize<List<string>>(entity.Tickers)!,
                EquityCurve = JsonSerializer.Deserialize<List<DateValue<double?>>>(entity.EquityCurve)!,
                StrategyName = entity.StrategyName,
                StrategyParams = entity.StrategyParams,
                RecoveryFactor = entity.RecoveryFactor,
                NetProfit = entity.NetProfit,                
                MaxDrawdownPercent = entity.MaxDrawdownPercent,
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
                Tickers = JsonSerializer.Serialize(model.Tickers),
                EquityCurve = JsonSerializer.Serialize(model.EquityCurve),
                StrategyName = model.StrategyName,
                StrategyParams = model.StrategyParams,
                RecoveryFactor = model.RecoveryFactor,
                NetProfit = model.NetProfit,
                MaxDrawdownPercent = model.MaxDrawdownPercent,
                StartMoney = model.StartMoney,
                EndMoney = model.EndMoney,
                TotalReturn = model.TotalReturn,
                AnnualYieldReturn = model.AnnualYieldReturn,
                ResultMessage = model.ResultMessage
            };
    }
}
