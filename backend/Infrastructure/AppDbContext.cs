using DeveloperPlatform.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DeveloperPlatform.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WbsElement> WbsElements => Set<WbsElement>();
    public DbSet<ExecutionPackage> ExecutionPackages => Set<ExecutionPackage>();
    public DbSet<ScheduleActivity> ScheduleActivities => Set<ScheduleActivity>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<DecisionItem> DecisionItems => Set<DecisionItem>();
    public DbSet<OperationalScenario> OperationalScenarios => Set<OperationalScenario>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<ScenarioPackage> ScenarioPackages => Set<ScenarioPackage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScenarioPackage>().HasKey(x => new { x.ScenarioId, x.ExecutionPackageId });
    }
}
