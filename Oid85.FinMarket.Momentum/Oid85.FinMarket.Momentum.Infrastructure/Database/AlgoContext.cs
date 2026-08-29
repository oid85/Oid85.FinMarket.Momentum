using Microsoft.EntityFrameworkCore;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Infrastructure.Database.Entities;
using Oid85.FinMarket.Momentum.Infrastructure.Database.Schemas;

namespace Oid85.FinMarket.Momentum.Infrastructure.Database;

public class AlgoContext(DbContextOptions<AlgoContext> options) : DbContext(options)
{
    public DbSet<StrategyExecuteResultEntity> StrategyExecuteResultEntities { get; set; }
    public DbSet<ParameterEntity> ParameterEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .HasDefaultSchema(KnownDatabaseSchemas.Default)
            .ApplyConfigurationsFromAssembly(
                typeof(AlgoContext).Assembly,
                type => type
                    .GetInterface(typeof(IAlgoSchema).ToString()) != null)
            .UseIdentityAlwaysColumns();
    }    
}