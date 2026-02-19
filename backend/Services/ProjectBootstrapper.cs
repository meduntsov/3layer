using DeveloperPlatform.Api.Domain;
using DeveloperPlatform.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DeveloperPlatform.Api.Services;

public class ProjectBootstrapper(AppDbContext db)
{
    public async Task EnsureDemoProjectAsync()
    {
        if (await db.Projects.AnyAsync()) return;

        var project = new Project { Id = Guid.NewGuid(), Name = "Demo Developer Platform", Status = "Active" };
        var wbsRows = new (string code, string name, string? parent)[]
        {
            ("01", "Design", null),
            ("02", "Procurement", null),
            ("03", "Construction", null),
            ("03.01", "HVAC", "03"),
            ("03.02", "Electrical", "03"),
            ("03.03", "Access Control", "03"),
            ("04", "Commissioning", null)
        };

        var wbs = new List<WbsElement>();
        foreach (var row in wbsRows)
        {
            var parent = wbs.FirstOrDefault(x => x.Code == row.parent);
            wbs.Add(new WbsElement
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ParentId = parent?.Id,
                Code = row.code,
                Name = row.name,
                WorkType = row.code.StartsWith("03") ? "Construction" : "Management",
                SystemDiscipline = row.name
            });
        }

        var packages = wbs.Select(w => new ExecutionPackage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            WbsElementId = w.Id,
            Name = $"Package {w.Code} {w.Name}",
            Status = w.Code is "03.01" or "03.02" ? "InProgress" : "Planned"
        }).ToList();

        var activities = new List<ScheduleActivity>();
        var dependencies = new List<Dependency>();
        var baseDate = DateTime.UtcNow.Date;
        foreach (var package in packages)
        {
            var names = new[] { "Design", "Procurement", "Installation", "Testing" };
            ScheduleActivity? prev = null;
            for (var i = 0; i < names.Length; i++)
            {
                var a = new ScheduleActivity
                {
                    Id = Guid.NewGuid(),
                    ExecutionPackageId = package.Id,
                    Name = names[i],
                    PlannedStart = baseDate.AddDays(i * 7),
                    PlannedFinish = baseDate.AddDays(i * 7 + 6),
                    Duration = 7
                };
                activities.Add(a);
                if (prev is not null)
                {
                    dependencies.Add(new Dependency { Id = Guid.NewGuid(), FromActivityId = prev.Id, ToActivityId = a.Id, Type = "FS" });
                }

                prev = a;
            }
        }

        var blocked1 = new DecisionItem { Id = Guid.NewGuid(), ExecutionPackageId = packages[3].Id, Title = "Approve HVAC design", Status = "Open", CreatedAt = DateTime.UtcNow.AddDays(-22) };
        var blocked2 = new DecisionItem { Id = Guid.NewGuid(), ExecutionPackageId = packages[4].Id, Title = "Select UPS vendor", Status = "Open", CreatedAt = DateTime.UtcNow.AddDays(-9) };
        packages[3].DecisionBlocked = true;
        packages[4].DecisionBlocked = true;

        var scenario1 = new OperationalScenario { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Core systems startup", Status = "NotReady" };
        var scenario2 = new OperationalScenario { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Security integration", Status = "Ready" };

        var tests = new List<TestCase>
        {
            new() { Id = Guid.NewGuid(), ScenarioId = scenario1.Id, Name = "Smoke test", Status = "Passed" },
            new() { Id = Guid.NewGuid(), ScenarioId = scenario1.Id, Name = "Failover", Status = "Pending" },
            new() { Id = Guid.NewGuid(), ScenarioId = scenario2.Id, Name = "Access matrix", Status = "Passed" }
        };

        var scenarioPackages = new List<ScenarioPackage>
        {
            new() { ScenarioId = scenario1.Id, ExecutionPackageId = packages[3].Id },
            new() { ScenarioId = scenario1.Id, ExecutionPackageId = packages[4].Id },
            new() { ScenarioId = scenario2.Id, ExecutionPackageId = packages[5].Id }
        };

        db.Projects.Add(project);
        db.WbsElements.AddRange(wbs);
        db.ExecutionPackages.AddRange(packages);
        db.ScheduleActivities.AddRange(activities);
        db.Dependencies.AddRange(dependencies);
        db.DecisionItems.AddRange(blocked1, blocked2);
        db.OperationalScenarios.AddRange(scenario1, scenario2);
        db.TestCases.AddRange(tests);
        db.ScenarioPackages.AddRange(scenarioPackages);
        await db.SaveChangesAsync();
    }
}
