using DeveloperPlatform.Api.Domain;
using DeveloperPlatform.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DeveloperPlatform.Api.Services;

public class ProjectBootstrapper(AppDbContext db)
{
    public async Task EnsureDemoProjectAsync()
    {
        if (await db.Projects.AnyAsync()) return;

        var project = new Project { Id = Guid.NewGuid(), Name = "Фрунзенская", Status = "Active" };
        var wbsRows = new (string code, string name, string? parent)[]
        {
            ("01", "Проектирование", null),
            ("02", "Закупка", null),
            ("03", "Строительно-монтажные работы", null),
            ("03.01", "ОВиК", "03"),
            ("03.02", "Электроснабжение", "03"),
            ("03.03", "Контроль доступа", "03"),
            ("04", "Пусконаладка", null)
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
                WorkType = row.code.StartsWith("03") ? "Строительные работы" : "Управление",
                SystemDiscipline = row.name
            });
        }

        var packages = wbs.Select(w => new ExecutionPackage
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            WbsElementId = w.Id,
            Name = $"Пакет {w.Code} {w.Name}",
            Status = w.Code is "03.01" or "03.02" ? "InProgress" : "Planned"
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

        var blocked1 = new DecisionItem { Id = Guid.NewGuid(), ExecutionPackageId = packages[3].Id, Title = "Утвердить проект ОВиК", Status = "Open", CreatedAt = DateTime.UtcNow.AddDays(-22) };
        var blocked2 = new DecisionItem { Id = Guid.NewGuid(), ExecutionPackageId = packages[4].Id, Title = "Выбрать поставщика ИБП", Status = "Open", CreatedAt = DateTime.UtcNow.AddDays(-9) };
        packages[3].DecisionBlocked = true;
        packages[4].DecisionBlocked = true;

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
