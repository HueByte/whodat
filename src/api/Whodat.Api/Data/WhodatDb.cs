using Microsoft.EntityFrameworkCore;
using Whodat.Api.Models;

namespace Whodat.Api.Data;

public class WhodatDb(DbContextOptions<WhodatDb> options) : DbContext(options)
{
    public DbSet<UserEntry> Users => Set<UserEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var user = b.Entity<UserEntry>();
        user.HasKey(u => u.Handle);
        user.Property(u => u.Handle).HasMaxLength(32);
        user.Property(u => u.AuthKind).HasConversion<string>().HasMaxLength(16);
        user.HasIndex(u => u.GithubId).IsUnique();
        user.HasIndex(u => u.TokenHash).IsUnique();
    }
}
