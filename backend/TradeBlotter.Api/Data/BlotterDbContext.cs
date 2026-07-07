using Microsoft.EntityFrameworkCore;
using TradeBlotter.Api.Domain;

namespace TradeBlotter.Api.Data;

/// <summary>EF Core context. Only trades are persisted; positions are always derived.</summary>
public class BlotterDbContext(DbContextOptions<BlotterDbContext> options) : DbContext(options)
{
    public DbSet<Trade> Trades => Set<Trade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var trade = modelBuilder.Entity<Trade>();
        trade.HasKey(t => t.Id);
        trade.Property(t => t.Symbol).IsRequired().HasMaxLength(16);
        trade.Property(t => t.Side).HasConversion<string>().HasMaxLength(4);
        // SQLite has no native decimal type. EF Core's default maps decimal to a TEXT
        // column, which round-trips the exact value — important for money math. We never
        // sort/compare these columns DB-side (ordering is by Timestamp), so the usual
        // SQLite decimal-comparison caveat does not apply.
        trade.Ignore(t => t.SignedQuantity);
        trade.HasIndex(t => t.Symbol);
        trade.HasIndex(t => t.Timestamp);
    }
}
