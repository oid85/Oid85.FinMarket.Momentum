using Microsoft.EntityFrameworkCore;
using Oid85.FinMarket.Algo.Common.KnownConstants;
using Oid85.FinMarket.Algo.Infrastructure.Database.Entities;
using Oid85.FinMarket.Algo.Infrastructure.Database.Schemas;

namespace Oid85.FinMarket.Algo.Infrastructure.Database;

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