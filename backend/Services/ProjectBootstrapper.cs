using System.Globalization;
using DeveloperPlatform.Api.Domain;
using DeveloperPlatform.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DeveloperPlatform.Api.Services;

public class ProjectBootstrapper(AppDbContext db)
{
    public async Task EnsureDemoProjectAsync()
    {
        var existingProject = await db.Projects.FirstOrDefaultAsync();
        if (existingProject is not null)
        {
            if (existingProject.Name == "Demo Developer Platform")
            {
                existingProject.Name = "Фрунзенская";
                await db.SaveChangesAsync();
            }

            return;
        }

        var project = new Project { Id = Guid.NewGuid(), Name = "Фрунзенская", Status = "Active" };
        var wbsRows = LoadWbsRows();

        var wbsById = new Dictionary<string, WbsElement>();
        var wbs = new List<WbsElement>();
        foreach (var row in wbsRows.OrderBy(r => r.Level).ThenBy(r => ParseCodeForOrdering(r.WbsId)))
        {
            wbsById.TryGetValue(row.ParentId, out var parent);

            var element = new WbsElement
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                ParentId = parent?.Id,
                Code = row.WbsCode,
                Name = row.NameRu,
                WorkType = string.IsNullOrWhiteSpace(row.DeliverableType) ? "Service" : row.DeliverableType,
                SystemDiscipline = string.IsNullOrWhiteSpace(row.Discipline) ? "General" : row.Discipline
            };

            wbs.Add(element);
            wbsById[row.WbsId] = element;
        }

        var packages = wbs.Select(w => new ExecutionPackage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            WbsElementId = w.Id,
            Name = $"Пакет {w.Code} {w.Name}",
            Status = w.Code.Contains("-1-5-") ? "InProgress" : "Planned"
        }).ToList();

        var activities = new List<ScheduleActivity>();
        var dependencies = new List<Dependency>();
        var baseDate = DateTime.UtcNow.Date;
        foreach (var package in packages)
        {
            var names = new[] { "Проектирование", "Закупка", "Монтаж", "Испытания" };
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

        var blockedPackages = packages.Take(2).ToList();
        var blocked1 = new DecisionItem { Id = Guid.NewGuid(), ExecutionPackageId = blockedPackages[0].Id, Title = "Согласовать критичный пакет WBS", Status = "Open", CreatedAt = DateTime.UtcNow.AddDays(-22) };
        var blocked2 = new DecisionItem { Id = Guid.NewGuid(), ExecutionPackageId = blockedPackages[1].Id, Title = "Подтвердить базовую дисциплину", Status = "Open", CreatedAt = DateTime.UtcNow.AddDays(-9) };
        blockedPackages[0].DecisionBlocked = true;
        blockedPackages[1].DecisionBlocked = true;

        var scenario1 = new OperationalScenario { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Интеграция базовых систем", Status = "NotReady" };
        var scenario2 = new OperationalScenario { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Интеграция систем безопасности", Status = "Ready" };

        var tests = new List<TestCase>
        {
            new() { Id = Guid.NewGuid(), ScenarioId = scenario1.Id, Name = "Дымовой тест", Status = "Passed" },
            new() { Id = Guid.NewGuid(), ScenarioId = scenario1.Id, Name = "Отработка отказа", Status = "Pending" },
            new() { Id = Guid.NewGuid(), ScenarioId = scenario2.Id, Name = "Матрица доступа", Status = "Passed" }
        };

        var scenarioPackages = new List<ScenarioPackage>
        {
            new() { ScenarioId = scenario1.Id, ExecutionPackageId = blockedPackages[0].Id },
            new() { ScenarioId = scenario1.Id, ExecutionPackageId = blockedPackages[1].Id },
            new() { ScenarioId = scenario2.Id, ExecutionPackageId = packages[2].Id }
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

    private static IEnumerable<WbsTemplateRow> LoadWbsRows()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "wbs-template.csv");
        var lines = File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("WBS template is empty.");
        }

        return lines.Skip(1).Select(ParseRow).ToList();
    }

    private static WbsTemplateRow ParseRow(string line)
    {
        var parts = line.Split(';');
        if (parts.Length < 14)
        {
            throw new InvalidOperationException($"Invalid WBS row: {line}");
        }

        return new WbsTemplateRow(
            WbsId: parts[0],
            WbsCode: parts[1],
            ParentId: parts[2],
            Level: int.Parse(parts[3], CultureInfo.InvariantCulture),
            NameRu: parts[4],
            Discipline: parts[7],
            DeliverableType: parts[8]);
    }

    private static string ParseCodeForOrdering(string wbsId)
    {
        return string.Join('.', wbsId.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => int.TryParse(p, out var n) ? n.ToString("D4", CultureInfo.InvariantCulture) : p));
    }

    private sealed record WbsTemplateRow(
        string WbsId,
        string WbsCode,
        string ParentId,
        int Level,
        string NameRu,
        string Discipline,
        string DeliverableType);
}
