using System;
using UnityEngine;

namespace Swarmforge.Presentation
{
    /// <summary>
    /// Presentation-only offsets for the P2 trial. Green bot spacing is
    /// authoritative (second spawn is eight sim ticks later); adding visual
    /// fan-out would once again make the cannon appear to fire sideways.
    /// Red enemies retain their deterministic four-slot appearance.
    /// </summary>
    public static class V12FormationLayout
    {
        public static Vector2 ForId(long unitId)
        {
            if (unitId <= 0) throw new ArgumentOutOfRangeException(nameof(unitId));
            int slot = (int)((unitId - 1) % 4);
            return new Vector2((slot & 1) == 0 ? -.70f : .70f,
                slot < 2 ? -.70f : .70f);
        }

        /// <summary>
        /// Every green MiniBot renders at its simulation X/Z. It never
        /// receives a sideways or artificial forward offset, regardless
        /// of ID, twin survival, aim commands or frame rate.
        /// </summary>
        public static Vector2 ForBot(int position, int attackPathLength)
        {
            if (attackPathLength <= 0) throw new ArgumentOutOfRangeException(nameof(attackPathLength));
            return Vector2.zero;
        }
    }
}
