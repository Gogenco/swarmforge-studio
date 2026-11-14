using System.Collections.Generic;
using Swarmforge.Simulation;

namespace Swarmforge.Presentation
{
    /// <summary>
    /// Keeps already-rendered logical IDs while they are alive. Crossing the visual
    /// budget boundary never resamples every MiniBot or changes gameplay state.
    /// </summary>
    public static class StableVisualSelection
    {
        public static List<long> Choose(IReadOnlyList<UnitView> units, IEnumerable<long> visibleIds, int budget)
        {
            if (units == null) throw new System.ArgumentNullException(nameof(units));
            if (budget < 1) throw new System.ArgumentOutOfRangeException(nameof(budget));
            var live = new Dictionary<long, UnitView>();
            for (int i = 0; i < units.Count; i++) live.Add(units[i].Id, units[i]);
            var result = new List<long>(System.Math.Min(budget, units.Count));
            var selected = new HashSet<long>();
            if (visibleIds != null)
                foreach (long id in visibleIds)
                    if (result.Count < budget && live.ContainsKey(id) && selected.Add(id)) result.Add(id);
            var candidates = new List<UnitView>(units);
            candidates.Sort((a, b) =>
            {
                int byPosition = b.Position.CompareTo(a.Position);
                return byPosition != 0 ? byPosition : a.Id.CompareTo(b.Id);
            });
            for (int i = 0; i < candidates.Count && result.Count < budget; i++)
                if (selected.Add(candidates[i].Id)) result.Add(candidates[i].Id);
            result.Sort(); // canonical enumeration regardless of HashSet implementation.
            return result;
        }
    }
}
