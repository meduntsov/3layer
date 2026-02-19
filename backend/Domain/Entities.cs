namespace DeveloperPlatform.Api.Domain;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public List<WbsElement> WbsElements { get; set; } = [];
    public List<ExecutionPackage> ExecutionPackages { get; set; } = [];
    public List<OperationalScenario> OperationalScenarios { get; set; } = [];
}

public class WbsElement
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WorkType { get; set; } = "General";
    public string SystemDiscipline { get; set; } = "General";
}

public class ExecutionPackage
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid WbsElementId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Planned";
    public bool DecisionBlocked { get; set; }
    public bool IntegrationRequired { get; set; } = true;
    public List<ScheduleActivity> Activities { get; set; } = [];
    public List<DecisionItem> Decisions { get; set; } = [];
}

public class ScheduleActivity
{
    public Guid Id { get; set; }
    public Guid ExecutionPackageId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedFinish { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualFinish { get; set; }
    public int Duration { get; set; }
    public int Float { get; set; }
    public bool IsCritical { get; set; }
}

public class Dependency
{
    public Guid Id { get; set; }
    public Guid FromActivityId { get; set; }
    public Guid ToActivityId { get; set; }
    public string Type { get; set; } = "FS";
}

public class DecisionItem
{
    public Guid Id { get; set; }
    public Guid ExecutionPackageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class OperationalScenario
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "NotReady";
    public List<TestCase> TestCases { get; set; } = [];
}

public class TestCase
{
    public Guid Id { get; set; }
    public Guid ScenarioId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public class ScenarioPackage
{
    public Guid ScenarioId { get; set; }
    public Guid ExecutionPackageId { get; set; }
}
