using DeveloperPlatform.Api.Domain;

namespace DeveloperPlatform.Api.Services;

public class CpmService
{
    public void Recalculate(List<ScheduleActivity> activities, List<Dependency> dependencies)
    {
        var byId = activities.ToDictionary(a => a.Id);
        var incoming = activities.ToDictionary(a => a.Id, _ => new List<Guid>());
        var outgoing = activities.ToDictionary(a => a.Id, _ => new List<Guid>());

        foreach (var d in dependencies)
        {
            outgoing[d.FromActivityId].Add(d.ToActivityId);
            incoming[d.ToActivityId].Add(d.FromActivityId);
        }

        var order = new List<Guid>();
        var queue = new Queue<Guid>(incoming.Where(x => x.Value.Count == 0).Select(x => x.Key));
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            order.Add(id);
            foreach (var next in outgoing[id])
            {
                incoming[next].Remove(id);
                if (incoming[next].Count == 0) queue.Enqueue(next);
            }
        }

        var es = new Dictionary<Guid, int>();
        var ef = new Dictionary<Guid, int>();
        foreach (var id in order)
        {
            es[id] = dependencies.Where(d => d.ToActivityId == id).Select(d => ef[d.FromActivityId]).DefaultIfEmpty(0).Max();
            ef[id] = es[id] + byId[id].Duration;
        }

        var projectFinish = ef.Values.DefaultIfEmpty(0).Max();
        var ls = new Dictionary<Guid, int>();
        var lf = new Dictionary<Guid, int>();

        foreach (var id in order.AsEnumerable().Reverse())
        {
            var succ = dependencies.Where(d => d.FromActivityId == id).Select(d => ls[d.ToActivityId]).ToList();
            lf[id] = succ.Count == 0 ? projectFinish : succ.Min();
            ls[id] = lf[id] - byId[id].Duration;
            byId[id].Float = ls[id] - es[id];
            byId[id].IsCritical = byId[id].Float == 0;
        }
    }
}
