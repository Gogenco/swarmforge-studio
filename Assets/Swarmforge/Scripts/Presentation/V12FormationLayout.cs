using System;
using UnityEngine;

namespace Swarmforge.Presentation
{
    /// <summary>
    /// Stable visual formation for opt-in P2 only. Logical lane, hitbox, HP,
    /// timing and replay state remain completely unchanged: this offset is
    /// applied by V12TrialView after the authoritative snapshot is copied.
    /// P2 creates at most two simultaneous ×2 copies or two enemies per lane,
    /// while four distinct slots provide room for nearby groups.
    /// </summary>
    public static class V12FormationLayout
    {
        public static Vector2 ForId(long unitId)
        {
            if (unitId <= 0) throw new ArgumentOutOfRangeException(nameof(unitId));

            // IDs are deterministic and immutable. Avoid array-index shifts
            // when one unit dies, frame-rate-dependent allocations, random
            // jitter or updates to the authoritative simulation coordinates.
            int slot = (int)((unitId - 1) % 4);
            float x = (slot & 1) == 0 ? -.52f : .52f;
            float z = slot < 2 ? -.45f : .45f;
            return new Vector2(x, z);
        }
    }
}
