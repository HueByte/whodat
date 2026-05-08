using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Whodat.Api.Auth;
using Whodat.Api.Data;
using Whodat.Api.Endpoints;
using Whodat.Api.Infisical;
using Whodat.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Pull secrets from Infisical (no-op when Infisical:Enabled=false) before any
// other configuration is read. Added last in the chain, so values override
// appsettings.json and env vars; use command-line args for emergency overrides.
builder.Configuration.AddInfisical(builder.Configuration);

builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration).ReadFrom.Services(services));

var dbPath = builder.Configuration["Whodat:DbPath"] ?? "whodat.db";
builder.Services.AddDbContext<WhodatDb>(o => o.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddOpenApi();

// ASP.NET Core Identity (slim — no SignInManager / cookies). UserManager is
// the only Identity surface we use; password hashing, external logins, and
// the AspNetUsers schema all come along for the ride.
builder.Services
    .AddIdentityCore<WhodatUser>(opts =>
    {
        opts.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789-";
        opts.User.RequireUniqueEmail = false;
        opts.Password.RequireDigit = false;
        opts.Password.RequireUppercase = false;
        opts.Password.RequireLowercase = false;
        opts.Password.RequireNonAlphanumeric = false;
        opts.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<WhodatDb>();

builder.Services
    .AddAuthentication(BearerTokenHandler.SchemeName)
    .AddScheme<BearerTokenOptions, BearerTokenHandler>(BearerTokenHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

builder.Services.Configure<GithubOptions>(builder.Configuration.GetSection("GitHub"));
builder.Services.AddHttpClient(GithubAuthEndpoints.HttpClientName, c =>
{
    c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("whodat-api");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Container networks renumber proxy IPs each restart, so trust any RFC1918 hop.
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WhodatDb>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var api = app.MapGroup("/api");
api.MapGet("/health", () => Results.Ok(new { status = "ok" }));
UsersEndpoints.Map(api);
GithubAuthEndpoints.Map(api);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
