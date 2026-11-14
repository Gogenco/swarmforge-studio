using System;
using System.Collections.Generic;

namespace Swarmforge.Simulation
{
    public interface IMatchSimulation
    {
        MatchSnapshot AdvanceOneTick();
        bool Submit(InputCommand command);
        MatchSnapshot GetSnapshot();
    }

    public interface ILevelDefinitionProvider
    {
        LevelDefinition Load(string levelId);
    }

    /// <summary>
    /// VS-01 authoritative game loop. Uses only integer arithmetic for all gameplay.
    /// Phase order: input -> spawn -> move/gate -> enemy contacts -> fortress hits ->
    /// base breaches -> outcome -> snapshot. Physics/render callbacks never deal damage.
    /// </summary>
    public sealed class MatchSimulation : IMatchSimulation
    {
        private readonly LevelDefinition config;
        private readonly InputCommandQueue commandQueue = new InputCommandQueue();
        private readonly MatchState state;
        private readonly List<GameEvent> events = new List<GameEvent>();
        private readonly List<GameEventBatch> history = new List<GameEventBatch>();
        private GameEventBatch lastEventBatch;
        private MatchSnapshot snapshot;

        public long MatchId { get; private set; }
        public int NextTick { get; private set; }
        public LevelDefinition Config { get { return config.Copy(); } }
        public IReadOnlyList<GameEvent> LastEvents { get { return lastEventBatch.Items; } }
        public GameEventBatch LastEventBatch { get { return lastEventBatch; } }
        public IReadOnlyList<GameEventBatch> GetEventHistory()
        {
            return Array.AsReadOnly(history.ToArray());
        }
        public bool IsTerminal { get { return state.Outcome != MatchOutcome.Playing; } }

        public MatchSimulation(LevelDefinition definition, long matchId = 1)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            config = definition.Copy();
            config.Validate();
            if (matchId < 1) throw new ArgumentOutOfRangeException(nameof(matchId));
            MatchId = matchId;
            state = new MatchState(config);
            NextTick = 0;
            lastEventBatch = new GameEventBatch(matchId, -1, state.EventHash, events);
            snapshot = new MatchSnapshot(-1, state);
        }

        public bool Submit(InputCommand command)
        {
            return !IsTerminal && commandQueue.Submit(command, NextTick, config.MatchTicks);
        }

        public MatchSnapshot GetSnapshot() { return snapshot; }

        public MatchSnapshot AdvanceOneTick()
        {
            if (IsTerminal) return snapshot;
            int tick = NextTick;
            events.Clear();

            // Phase 0: commit all commands in a canonical order.
            List<InputCommand> commands = commandQueue.TakeForTick(tick);
            for (int i = 0; i < commands.Count; i++)
            {
                // Hash effective input history, including moves with identical resulting X.
                MatchSnapshot.Hash(ref state.CommandHash, commands[i].Tick);
                MatchSnapshot.Hash(ref state.CommandHash, commands[i].Sequence);
                MatchSnapshot.Hash(ref state.CommandHash, (int)commands[i].Kind);
                MatchSnapshot.Hash(ref state.CommandHash, commands[i].Value);
                if (commands[i].Kind == CommandKind.MoveCannon)
                {
                    state.CannonX = Clamp(commands[i].Value, config.CannonMinX, config.CannonMaxX);
                    Publish(tick, GameEventKind.CannonMoved, 0, 0, state.CannonX);
                }
            }

            // Phase 1: first raw bot is tick 0; final raw bot tick 1790.
            if (tick < config.MatchTicks && tick >= config.FirstShotTick &&
                (tick - config.FirstShotTick) % config.ShotPeriodTicks == 0)
                SpawnRaw(tick);
            SpawnWaves(tick);

            // Phase 2: born-on-this-tick units start moving on NEXT tick.
            for (int i = 0; i < state.MiniBots.Count; i++)
            {
                MiniBot bot = state.MiniBots[i];
                bot.PreviousPosition = bot.Position;
                if (bot.Alive && bot.BornTick < tick) bot.Position += config.MiniBotSpeedPerTick;
            }
            for (int i = 0; i < state.Enemies.Count; i++)
            {
                EnemyDrone enemy = state.Enemies[i];
                enemy.PreviousPosition = enemy.Position;
                if (enemy.Alive && enemy.BornTick < tick) enemy.Position += config.EnemySpeedPerTick;
            }
            ApplyGates(tick);

            // Phase 3: paired contacts are processed before either goal crossing.
            ResolveContacts(tick);
            ResolveFortressImpacts(tick);
            ResolveBreaches(tick);
            RemoveConsumed();

            // Phase 4: all same-tick damage precedes the terminal tie-break.
            MatchOutcome outcome = OutcomeResolver.Resolve(
                state.BaseHP, state.FortressHP, tick, config.MatchTicks);
            if (outcome != MatchOutcome.Playing)
            {
                state.Outcome = outcome;
                state.TerminalTick = tick;
                state.TerminalEvents++;
                Publish(tick, GameEventKind.MatchEnded, 0, 0, (int)outcome);
            }

            VerifyConservation();
            lastEventBatch = new GameEventBatch(MatchId, tick, state.EventHash, events);
            history.Add(lastEventBatch);
            snapshot = new MatchSnapshot(tick, state);
            NextTick++;
            return snapshot;
        }

        private void SpawnRaw(int tick)
        {
            MiniBot bot = new MiniBot
            {
                Id = state.IssueId(), BornTick = tick,
                CannonX = state.CannonX, Lane = config.LaneForX(state.CannonX)
            };
            state.MiniBots.Add(bot);
            state.RawShots++;
            state.ProducedTotal++;
            state.LastShotTick = tick;
            Publish(tick, GameEventKind.RawShot, bot.Id, 0, 1);
        }

        private void SpawnWaves(int tick)
        {
            for (int i = 0; i < config.EnemyWaves.Length; i++)
            {
                EnemyWave wave = config.EnemyWaves[i];
                if (wave.Tick != tick) continue;
                SpawnEnemies(tick, -1, wave.Left);
                SpawnEnemies(tick, 1, wave.Right);
            }
        }

        private void SpawnEnemies(int tick, int lane, int count)
        {
            for (int n = 0; n < count; n++)
            {
                var drone = new EnemyDrone { Id = state.IssueId(), BornTick = tick, Lane = lane };
                state.Enemies.Add(drone);
                Publish(tick, GameEventKind.EnemySpawned, drone.Id, 0, lane);
            }
        }

        private void ApplyGates(int tick)
        {
            // The original bot becomes one child; each extra clone gets its own stable ID.
            // Iterate only the pre-multiplied set, not children created this phase.
            int countBefore = state.MiniBots.Count;
            for (int i = 0; i < countBefore; i++)
            {
                MiniBot bot = state.MiniBots[i];
                if (!bot.Alive || bot.GateUsed || bot.Position < config.GatePosition) continue;
                bot.GateUsed = true;
                int multiplier = config.GateForX(bot.CannonX);
                for (int j = 1; j < multiplier; j++)
                {
                    state.MiniBots.Add(new MiniBot
                    {
                        Id = state.IssueId(), BornTick = bot.BornTick,
                        Position = bot.Position, PreviousPosition = bot.PreviousPosition,
                        CannonX = bot.CannonX,
                        Lane = bot.Lane, GateUsed = true
                    });
                    state.GateExtraProduced++;
                    state.ProducedTotal++;
                }
                Publish(tick, GameEventKind.GateMultiplied, bot.Id, 0, multiplier);
            }
        }

        private readonly struct ContactCandidate
        {
            public readonly MiniBot Bot;
            public readonly EnemyDrone Enemy;
            public readonly long Numerator;
            public readonly long Denominator;

            public ContactCandidate(MiniBot bot, EnemyDrone enemy, long numerator, long denominator)
            {
                Bot = bot;
                Enemy = enemy;
                Numerator = numerator;
                Denominator = denominator;
            }
        }

        private void ResolveContacts(int tick)
        {
            // Swept crossing, not end-of-tick proximity: a high-speed pass cannot tunnel.
            // Process earliest contact first. Stable IDs break same-time ties.
            var contacts = new List<ContactCandidate>();
            for (int i = 0; i < state.Enemies.Count; i++)
            {
                EnemyDrone enemy = state.Enemies[i];
                if (!enemy.Alive) continue;
                for (int j = 0; j < state.MiniBots.Count; j++)
                {
                    MiniBot bot = state.MiniBots[j];
                    if (!bot.Alive || bot.Lane != enemy.Lane) continue;
                    long startGap = (long)config.AttackPathLength - bot.PreviousPosition - enemy.PreviousPosition;
                    long endGap = (long)config.AttackPathLength - bot.Position - enemy.Position;
                    if (startGap < -config.ContactRadius || endGap > config.ContactRadius) continue;
                    long denominator = Math.Max(1L, startGap - endGap);
                    long numerator = Math.Max(0L, startGap - config.ContactRadius);
                    contacts.Add(new ContactCandidate(bot, enemy, numerator, denominator));
                }
            }
            contacts.Sort((a, b) =>
            {
                // Decimal cross-products avoid long overflow for extreme test configs.
                int byTime = ((decimal)a.Numerator * b.Denominator)
                    .CompareTo((decimal)b.Numerator * a.Denominator);
                if (byTime != 0) return byTime;
                int byEnemy = a.Enemy.Id.CompareTo(b.Enemy.Id);
                return byEnemy != 0 ? byEnemy : a.Bot.Id.CompareTo(b.Bot.Id);
            });
            for (int i = 0; i < contacts.Count; i++)
            {
                ContactCandidate candidate = contacts[i];
                if (!candidate.Bot.Alive || !candidate.Enemy.Alive) continue;
                candidate.Bot.Alive = false;
                candidate.Enemy.Alive = false;
                state.EnemyLosses++;
                state.EnemiesDestroyed++;
                Publish(tick, GameEventKind.EnemyDefeated, candidate.Bot.Id, candidate.Enemy.Id, 1);
                Publish(tick, GameEventKind.MiniBotLost, candidate.Bot.Id, candidate.Enemy.Id, 1);
            }
        }

        private void ResolveFortressImpacts(int tick)
        {
            for (int i = 0; i < state.MiniBots.Count; i++)
            {
                MiniBot bot = state.MiniBots[i];
                if (!bot.Alive || bot.Position < config.AttackPathLength) continue;
                bot.Alive = false;
                state.FortressHits++;
                if (state.FortressHP > 0) state.FortressHP--;
                Publish(tick, GameEventKind.FortressDamaged, bot.Id, 0, state.FortressHP);
            }
            int stage = state.FortressHP == 0 ? 3 :
                state.FortressHP * 3 <= config.FortressHP ? 2 :
                state.FortressHP * 3 <= config.FortressHP * 2 ? 1 : 0;
            if (stage != state.FortressStage)
            {
                state.FortressStage = stage;
                Publish(tick, GameEventKind.FortressStageChanged, 0, 0, stage);
            }
        }

        private void ResolveBreaches(int tick)
        {
            for (int i = 0; i < state.Enemies.Count; i++)
            {
                EnemyDrone enemy = state.Enemies[i];
                if (!enemy.Alive || enemy.Position < config.EnemyPathLength) continue;
                enemy.Alive = false;
                state.Breaches++;
                if (state.BaseHP > 0) state.BaseHP--;
                Publish(tick, GameEventKind.BaseBreached, enemy.Id, 0, state.BaseHP);
            }
        }

        private void RemoveConsumed()
        {
            state.MiniBots.RemoveAll(bot => !bot.Alive);
            state.Enemies.RemoveAll(drone => !drone.Alive);
        }

        private void VerifyConservation()
        {
            if (state.ProducedTotal != state.MiniBots.Count + state.FortressHits + state.EnemyLosses)
                throw new InvalidOperationException("MiniBot conservation invariant violated.");
            if (state.TerminalEvents > 1 || state.BaseHP < 0 || state.FortressHP < 0)
                throw new InvalidOperationException("Terminal/health invariant violated.");
        }

        private void Publish(int tick, GameEventKind kind, long source, long target, int value)
        {
            events.Add(new GameEvent(tick, kind, source, target, value));
            MatchSnapshot.Hash(ref state.EventHash, tick);
            MatchSnapshot.Hash(ref state.EventHash, (int)kind);
            MatchSnapshot.Hash(ref state.EventHash, source);
            MatchSnapshot.Hash(ref state.EventHash, target);
            MatchSnapshot.Hash(ref state.EventHash, value);
        }

        private static int Clamp(int v, int low, int high)
        {
            return v < low ? low : v > high ? high : v;
        }
    }
}
