using System;
using System.Collections.Generic;
using Swarmforge.Simulation;
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
        private const float BotFanStartWorldZ = .25f;
        private const float BotFanEndWorldZ = 1.60f;

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

        /// <summary>
        /// Green MiniBots always emerge from the exact logical muzzle X.
        /// A paired shot fans out only after traveling away from the barrel;
        /// smoothstep gives zero lateral velocity at both ends of the ramp.
        /// This never changes the bot's authoritative simulation coordinates.
        /// </summary>
        public static Vector2 ForBot(int position, int attackPathLength, float cohortSlot)
        {
            if (attackPathLength <= 0) throw new ArgumentOutOfRangeException(nameof(attackPathLength));
            if (cohortSlot < -1f || cohortSlot > 1f)
                throw new ArgumentOutOfRangeException(nameof(cohortSlot));
            if (cohortSlot == 0f) return Vector2.zero;

            float travelWorldZ = 18f * Mathf.Max(0, position) / attackPathLength;
            float t = Mathf.Clamp01((travelWorldZ - BotFanStartWorldZ) /
                (BotFanEndWorldZ - BotFanStartWorldZ));
            float smooth = t * t * (3f - 2f * t);
            return new Vector2(cohortSlot * .52f * smooth, 0f);
        }
    }

    /// <summary>
    /// Presentation-only one-time assignment: same-lane, same-position twins
    /// receive stable left/right slots; single bots always receive zero.
    /// Assignment survives a partner dying, so no remaining robot snaps to
    /// center. Group rank uses immutable unit IDs, never iteration order.
    /// </summary>
    public sealed class V12BotVisualCohorts
    {
        private readonly Dictionary<long, float> slots = new Dictionary<long, float>();

        public float GetOrAssign(V12TrialUnitView bot, IReadOnlyList<V12TrialUnitView> visibleBots)
        {
            if (bot.Id <= 0) throw new ArgumentOutOfRangeException(nameof(bot));
            if (visibleBots == null) throw new ArgumentNullException(nameof(visibleBots));
            if (slots.TryGetValue(bot.Id, out float assigned)) return assigned;

            int count = 0, rank = 0;
            for (int i = 0; i < visibleBots.Count; i++)
            {
                V12TrialUnitView other = visibleBots[i];
                if (other.X != bot.X || other.Position != bot.Position) continue;
                count++;
                if (other.Id < bot.Id) rank++;
            }
            if (count == 0) throw new ArgumentException("Bot must belong to its visible cohort.");
            assigned = count == 1 ? 0f : -1f + 2f * rank / (count - 1);
            slots.Add(bot.Id, assigned);
            return assigned;
        }

        public void Forget(long botId) { slots.Remove(botId); }
    }
}
