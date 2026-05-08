using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Whodat.Api.Auth;
using Whodat.Api.Data;
using Whodat.Api.Endpoints;
using Whodat.Api.Infisical;

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
