using System;

namespace Swarmforge.Simulation
{
    /// <summary>Render-frame accumulator: never drops unprocessed sim time.</summary>
    public sealed class SimulationClock
    {
        private readonly int tickRate;
        private double secondsPending;
        public bool Paused { get; set; }
        public int MaxTicksPerFrame { get; set; } = 8;
        public double PendingSeconds { get { return secondsPending; } }

        public SimulationClock(int tickRate)
        {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));
            this.tickRate = tickRate;
        }

        public int Advance(double frameSeconds, Action advanceOneTick)
        {
            if (frameSeconds < 0 || double.IsNaN(frameSeconds) || double.IsInfinity(frameSeconds))
                throw new ArgumentOutOfRangeException(nameof(frameSeconds));
            if (Paused) return 0;
            secondsPending += frameSeconds;
            int advanced = 0;
            double duration = 1.0 / tickRate;
            while (secondsPending + 1e-10 >= duration && advanced < MaxTicksPerFrame)
            {
                secondsPending -= duration;
                if (secondsPending < 0) secondsPending = 0;
                advanceOneTick();
                advanced++;
            }
            return advanced;
        }

        public void Reset() { secondsPending = 0; Paused = false; }
    }
}
