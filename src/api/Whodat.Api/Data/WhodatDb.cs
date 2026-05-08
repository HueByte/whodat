using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Whodat.Api.Models;

namespace Whodat.Api.Data;

public class WhodatDb(DbContextOptions<WhodatDb> options) : IdentityDbContext<WhodatUser>(options)
{
    public DbSet<UserAlias> UserAliases => Set<UserAlias>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Token lookups happen on every authed request — index it.
        b.Entity<WhodatUser>().HasIndex(u => u.TokenHash);

        b.Entity<UserAlias>(a =>
        {
            // Aliases are globally unique so lookups by alias are deterministic.
            a.HasIndex(x => x.Alias).IsUnique();
            a.Property(x => x.Alias).HasMaxLength(32);
            a.HasOne(x => x.User)
                .WithMany(u => u.Aliases)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
