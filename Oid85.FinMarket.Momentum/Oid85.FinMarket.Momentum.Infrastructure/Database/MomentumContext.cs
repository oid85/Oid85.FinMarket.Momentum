using Microsoft.EntityFrameworkCore;
using Oid85.FinMarket.Momentum.Common.KnownConstants;
using Oid85.FinMarket.Momentum.Infrastructure.Database.Entities;
using Oid85.FinMarket.Momentum.Infrastructure.Database.Schemas;

namespace Oid85.FinMarket.Momentum.Infrastructure.Database;

public class MomentumContext(DbContextOptions<MomentumContext> options) : DbContext(options)
{
    public DbSet<ParameterEntity> ParameterEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .HasDefaultSchema(KnownDatabaseSchemas.Default)
            .ApplyConfigurationsFromAssembly(
                typeof(MomentumContext).Assembly,
                type => type
                    .GetInterface(typeof(IMomentumSchema).ToString()) != null)
            .UseIdentityAlwaysColumns();
    }    
}