using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Whodat.Api.Models;

namespace Whodat.Api.Data;

public class WhodatDb(DbContextOptions<WhodatDb> options) : IdentityDbContext<WhodatUser>(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Token lookups happen on every authed request — index it.
        b.Entity<WhodatUser>().HasIndex(u => u.TokenHash);
    }
}
