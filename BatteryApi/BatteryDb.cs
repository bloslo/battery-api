using BatteryApi.Models;
using Microsoft.EntityFrameworkCore;

public class BatteryDb : DbContext
{
    public BatteryDb(DbContextOptions<BatteryDb> options)
        : base(options) { }

    public DbSet<Battery> Batteries => Set<Battery>();

    public DbSet<BatteryIssue> BatteryIssues => Set<BatteryIssue>();

    public override int SaveChanges()
    {
        AddTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AddTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AddTimestamps()
    {
        var entities = ChangeTracker.Entries()
            .Where(x => x.Entity is Battery
                && (x.State == EntityState.Added
                    || x.State == EntityState.Modified));

        foreach (var entity in entities)
        {
            var now = DateTime.UtcNow;
            if (entity.State == EntityState.Added)
            {
                ((Battery)entity.Entity).Created = now;
            }
            ((Battery)entity.Entity).Updated = now;
        }
    }
}
