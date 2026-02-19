using DeveloperPlatform.Api.Infrastructure;
using DeveloperPlatform.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default") ?? "Host=db;Port=5432;Database=devplatform;Username=postgres;Password=postgres"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddPolicy("all", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddHttpClient();
builder.Services.AddScoped<ProjectBootstrapper>();
builder.Services.AddScoped<CpmService>();
builder.Services.AddScoped<AiService>();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("all");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    await scope.ServiceProvider.GetRequiredService<ProjectBootstrapper>().EnsureDemoProjectAsync();

    var cpm = scope.ServiceProvider.GetRequiredService<CpmService>();
    cpm.Recalculate(db.ScheduleActivities.ToList(), db.Dependencies.ToList());
    await db.SaveChangesAsync();
}

app.MapGet("/api/projects", async (AppDbContext db) => await db.Projects.ToListAsync());
app.MapGet("/api/wbs/{projectId:guid}", async (Guid projectId, AppDbContext db) => await db.WbsElements.Where(x => x.ProjectId == projectId).OrderBy(x => x.Code).ToListAsync());
app.MapGet("/api/packages/{projectId:guid}", async (Guid projectId, AppDbContext db) => await db.ExecutionPackages.Where(x => x.ProjectId == projectId).ToListAsync());
app.MapGet("/api/schedule/{projectId:guid}", async (Guid projectId, AppDbContext db) =>
    await db.ScheduleActivities.Join(db.ExecutionPackages.Where(p => p.ProjectId == projectId), a => a.ExecutionPackageId, p => p.Id, (a, p) => a).ToListAsync());
app.MapGet("/api/decisions/{projectId:guid}", async (Guid projectId, AppDbContext db) =>
    await db.DecisionItems.Join(db.ExecutionPackages.Where(p => p.ProjectId == projectId), d => d.ExecutionPackageId, p => p.Id, (d, p) => d).ToListAsync());
app.MapGet("/api/integration/{projectId:guid}", async (Guid projectId, AppDbContext db) =>
{
    var scenarios = await db.OperationalScenarios.Where(x => x.ProjectId == projectId).ToListAsync();
    var tests = await db.TestCases.ToListAsync();
    var links = await db.ScenarioPackages.ToListAsync();
    var packs = await db.ExecutionPackages.Where(x => x.ProjectId == projectId).ToListAsync();

    foreach (var s in scenarios)
    {
        var sTests = tests.Where(t => t.ScenarioId == s.Id).ToList();
        var pkgIds = links.Where(x => x.ScenarioId == s.Id).Select(x => x.ExecutionPackageId).ToList();
        var complete = packs.Where(p => pkgIds.Contains(p.Id)).All(p => p.Status == "Complete");
        s.Status = (complete && sTests.All(t => t.Status == "Passed")) ? "Ready" : "NotReady";
    }

    await db.SaveChangesAsync();
    return scenarios.Select(s => new { s.Id, s.Name, s.Status, Tests = tests.Where(t => t.ScenarioId == s.Id) });
});

app.MapPost("/api/packages/{id:guid}/status", async (Guid id, string status, AppDbContext db) =>
{
    var package = await db.ExecutionPackages.FindAsync(id);
    if (package is null) return Results.NotFound();

    if (status == "Ready")
    {
        var hasOpen = await db.DecisionItems.AnyAsync(d => d.ExecutionPackageId == id && d.Status == "Open");
        if (hasOpen) return Results.BadRequest("Package blocked by open decisions");
    }

    package.Status = status;
    await db.SaveChangesAsync();
    return Results.Ok(package);
});

app.MapGet("/api/dashboard/{projectId:guid}", async (Guid projectId, AppDbContext db) =>
{
    var packages = await db.ExecutionPackages.Where(x => x.ProjectId == projectId).ToListAsync();
    var decisions = await db.DecisionItems.Join(db.ExecutionPackages.Where(p => p.ProjectId == projectId), d => d.ExecutionPackageId, p => p.Id, (d, p) => d).ToListAsync();
    var scenarios = await db.OperationalScenarios.Where(x => x.ProjectId == projectId).ToListAsync();
    var activities = await db.ScheduleActivities.Join(db.ExecutionPackages.Where(p => p.ProjectId == projectId), a => a.ExecutionPackageId, p => p.Id, (a, p) => a).ToListAsync();

    return new
    {
        CriticalActivities = activities.Count(a => a.IsCritical),
        BlockedPackages = packages.Count(p => decisions.Any(d => d.ExecutionPackageId == p.Id && d.Status == "Open")),
        OpenDecisionAgeDays = decisions.Where(d => d.Status == "Open").Select(d => (DateTime.UtcNow - d.CreatedAt).Days),
        NotReadyScenarios = scenarios.Count(s => s.Status != "Ready")
    };
});

app.MapPost("/api/ai/analyze", async (Guid projectId, AppDbContext db, AiService ai) =>
{
    var packages = await db.ExecutionPackages.Where(x => x.ProjectId == projectId).ToListAsync();
    var packageIds = packages.Select(x => x.Id).ToHashSet();
    var payload = new
    {
        packages,
        activities = await db.ScheduleActivities.Where(a => packageIds.Contains(a.ExecutionPackageId)).Select(a => new { a.Name, a.Duration, a.Float, a.IsCritical }).ToListAsync(),
        openDecisions = await db.DecisionItems.Where(d => packageIds.Contains(d.ExecutionPackageId) && d.Status == "Open")
            .Select(d => new { d.Title, DecisionAge = (DateTime.UtcNow - d.CreatedAt).Days }).ToListAsync(),
        scenarios = await db.OperationalScenarios.Where(s => s.ProjectId == projectId).Select(s => new { s.Name, s.Status }).ToListAsync()
    };

    var text = await ai.AnalyzeAsync(payload);
    return Results.Ok(new { report = text });
});

app.Run();
