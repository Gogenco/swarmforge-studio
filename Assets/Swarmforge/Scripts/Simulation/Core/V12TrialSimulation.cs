using System;
using System.Collections.Generic;

namespace Swarmforge.Simulation
{
    /// <summary>
    /// Opt-in 60-second v1.2 greybox trial. This never invokes or mutates the
    /// existing MatchSimulation / legacy Battle scene. All combat and geometry
    /// are integer fixed units with rational within-tick event timestamps.
    /// </summary>
    public sealed class V12TrialSimulation
    {
        // 8 sim ticks = 0.267s. At 340 fixed units/tick the preceding bot
        // is already 7*340 units (0.84 world Z) ahead when its twin appears.
        // Two actual bots share ONE captured aim lane; this is never VFX jitter.
        public const int BoostFollowupDelayTicks = 8;

        private readonly struct PendingFollowup
        {
            public readonly int DueTick;
            public readonly int CapturedX;
            public readonly bool BoxDirected;
            public PendingFollowup(int dueTick, int capturedX, bool boxDirected)
            {
                DueTick = dueTick;
                CapturedX = capturedX;
                BoxDirected = boxDirected;
            }
        }

        private sealed class Bot
        {
            public long Id;
            public int X, HP, MaxHP, BornTick, ResumeTick, LastContactTick = -1;
            public long Z, StartZ, EngagedEnemyId;
            public bool Alive = true, Stopped, BoxDirected, HasStruck;
            public V12TickTime LastStrike;
            public V12TickTime LastContactTime;
        }

        private sealed class Enemy
        {
            public long Id;
            public int X, HP, MaxHP, Damage, Speed, Cooldown, BornTick, ResumeTick;
            public long Z, StartZ;
            public bool Alive = true, Stopped, HasStruck;
            public V12TickTime LastStrike;
        }

        private enum CandidateKind { Combat, Box, Fortress, Breach }

        private readonly struct Candidate : IComparable<Candidate>
        {
            public readonly V12TickTime Time;
            public readonly CandidateKind Kind;
            public readonly Bot Bot;
            public readonly Enemy Enemy;

            public Candidate(V12TickTime time, CandidateKind kind, Bot bot, Enemy enemy)
            {
                Time = time; Kind = kind; Bot = bot; Enemy = enemy;
            }

            public int CompareTo(Candidate other)
            {
                int c = Time.CompareTo(other.Time);
                if (c != 0) return c;
                c = Kind.CompareTo(other.Kind);
                if (c != 0) return c;
                long target = Enemy == null ? 0 : Enemy.Id;
                long otherTarget = other.Enemy == null ? 0 : other.Enemy.Id;
                c = target.CompareTo(otherTarget);
                if (c != 0) return c;
                long actor = Bot == null ? 0 : Bot.Id;
                long otherActor = other.Bot == null ? 0 : other.Bot.Id;
                return actor.CompareTo(otherActor);
            }
        }

        private readonly V12MatchConfig match;
        private readonly V12BalanceDefinition cfg;
        private readonly V12BoxBalance box;
        private readonly List<Bot> bots = new List<Bot>();
        private readonly List<Enemy> enemies = new List<Enemy>();
        // Owned by THIS match. A restart creates a fresh queue; no global delay
        // callbacks, coroutines, frame delta or Time.time can spawn robots.
        private readonly List<PendingFollowup> pendingFollowups = new List<PendingFollowup>();
        private readonly Dictionary<int, List<AimCommand>> input = new Dictionary<int, List<AimCommand>>();
        private readonly List<V12TrialEvent> tickEvents = new List<V12TrialEvent>();
        private readonly List<V12TrialEvent> history = new List<V12TrialEvent>();
        private readonly V12BoostLedger boosts = new V12BoostLedger();
        private long nextUnitId = 1;
        private long lastCommandSequence;
        private int nextTick, cannonX, baseHp, fortressHp, boxHp;
        private int rawShots, botsProduced, enemiesSpawned, enemiesKilled, groupsSpawned, boxHits;
        private bool boxPromise;
        private MatchOutcome outcome = MatchOutcome.Playing;
        private ulong eventHash = 14695981039346656037UL;
        private ulong commandHash = 14695981039346656037UL;
        private V12TrialSnapshot snapshot;

        private readonly struct AimCommand
        {
            public readonly long Sequence;
            public readonly int X;
            public AimCommand(long sequence, int x) { Sequence = sequence; X = x; }
        }

        public int NextTick { get { return nextTick; } }
        public bool IsTerminal { get { return outcome != MatchOutcome.Playing; } }
        public V12MatchConfig MatchConfig { get { return match; } }
        public IReadOnlyList<V12TrialEvent> History { get { return history.AsReadOnly(); } }

        public V12TrialSimulation(V12MatchConfig config)
        {
            match = config ?? throw new ArgumentNullException(nameof(config));
            cfg = match.CreateEditableCopy();
            cfg.Validate();
            if (cfg.TickRate != 30 || cfg.MatchTicks != 1800 || cfg.Boxes.Length != 1 ||
                cfg.Boxes[0].Multiplier != 2 || cfg.Boxes[0].MaxHP != 5 ||
                cfg.Boxes[0].DurationTicks != 360 || cfg.Waves.Length != 1 ||
                cfg.MiniBotMaxHP < 2 || cfg.MiniBotMaxHP > 3)
                throw new ArgumentException("P2 only permits the 60s/one X2 HP5/12s/one-group HP2-or-HP3 trial.");
            // A single authoritative P2 central combat corridor is separate
            // from the one side reward box, even when both hit radii are used.
            // Without this guard an Inspector edit can silently restore the
            // previous impossible-to-read overlapping combat/box targeting.
            int centerX = cfg.CannonMinX + (cfg.CannonMaxX - cfg.CannonMinX) / 2;
            if (Math.Abs((long)cfg.Boxes[0].X - centerX) <=
                (long)cfg.ContactRadius + cfg.Boxes[0].Radius)
                throw new ArgumentException("P2 side box overlaps central enemy combat corridor.");
            box = cfg.Boxes[0];
            boxHp = box.MaxHP;
            baseHp = cfg.BaseHP;
            fortressHp = cfg.FortressHP;
            cannonX = (cfg.CannonMinX + cfg.CannonMaxX) / 2;
            snapshot = MakeSnapshot(-1);
        }

        public bool SubmitAim(int tick, long sequence, int x)
        {
            if (IsTerminal || tick < nextTick || tick >= cfg.MatchTicks || sequence <= lastCommandSequence)
                return false;
            if (!input.TryGetValue(tick, out List<AimCommand> commands))
            {
                commands = new List<AimCommand>();
                input.Add(tick, commands);
            }
            commands.Add(new AimCommand(sequence, Clamp(x, cfg.CannonMinX, cfg.CannonMaxX)));
            lastCommandSequence = sequence;
            return true;
        }

        public V12TrialSnapshot Snapshot() { return snapshot; }

        public V12TrialSnapshot AdvanceOneTick()
        {
            if (IsTerminal) return snapshot;
            int tick = nextTick++;
            tickEvents.Clear();
            V12TickTime boundary = new V12TickTime(tick);
            if (boosts.BeginTick(tick))
                Publish(boundary, boosts.HasActive ? V12TrialEventKind.BoostStarted : V12TrialEventKind.BoostExpired,
                    0, 0, boosts.ActiveMultiplier);
            ApplyCommands(tick, boundary);
            // A committed second bot uses the aim X captured by its first
            // shot, even if the cannon has moved or the bonus just expired.
            EmitDueFollowups(tick, boundary);
            if (tick >= cfg.FirstShotTick && (tick - cfg.FirstShotTick) % cfg.ShotPeriodTicks == 0)
                Fire(tick, boundary);
            SpawnWave(tick, boundary);

            for (int i = 0; i < bots.Count; i++) { bots[i].StartZ = bots[i].Z; bots[i].Stopped = false; }
            for (int i = 0; i < enemies.Count; i++) { enemies[i].StartZ = enemies[i].Z; enemies[i].Stopped = false; }

            List<Candidate> candidates = Discover(tick);
            candidates.Sort();
            for (int i = 0; i < candidates.Count;)
            {
                Candidate candidate = candidates[i];
                if (candidate.Kind == CandidateKind.Combat)
                {
                    int end = i + 1;
                    while (end < candidates.Count && candidates[end].Kind == CandidateKind.Combat &&
                        candidates[end].Time.CompareTo(candidate.Time) == 0) end++;
                    CombatBatch(candidates, i, end, tick, candidate.Time);
                    i = end;
                    continue;
                }
                if (candidate.Kind == CandidateKind.Box) BoxImpact(candidate, tick);
                else if (candidate.Kind == CandidateKind.Fortress) FortressImpact(candidate, tick);
                else if (candidate.Kind == CandidateKind.Breach) BaseBreach(candidate, tick);
                i++;
            }

            for (int i = 0; i < bots.Count; i++)
            {
                Bot bot = bots[i];
                if (bot.Alive && !bot.Stopped && CanMove(bot, tick)) bot.Z = checked(bot.StartZ + cfg.MiniBotSpeedPerTick);
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy.Alive && !enemy.Stopped && CanMove(enemy, tick)) enemy.Z = checked(enemy.StartZ - enemy.Speed);
            }
            bots.RemoveAll(b => !b.Alive);
            enemies.RemoveAll(e => !e.Alive);
            RefreshBoxPromise();
            if (baseHp <= 0) outcome = MatchOutcome.LoseBase;
            else if (fortressHp <= 0) outcome = MatchOutcome.Win;
            else if (tick + 1 >= cfg.MatchTicks) outcome = MatchOutcome.LoseTimeout;
            if (outcome != MatchOutcome.Playing)
            {
                // Terminal state forbids further emissions. Do not carry a
                // pending shot into Restart or try to fire after match end.
                pendingFollowups.Clear();
                Publish(new V12TickTime(tick), V12TrialEventKind.MatchEnded, 0, 0, (int)outcome);
            }
            snapshot = MakeSnapshot(tick);
            return snapshot;
        }

        private void ApplyCommands(int tick, V12TickTime at)
        {
            if (!input.TryGetValue(tick, out List<AimCommand> commands)) return;
            commands.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
            for (int i = 0; i < commands.Count; i++)
            {
                cannonX = commands[i].X;
                Hash(ref commandHash, tick);
                Hash(ref commandHash, commands[i].Sequence);
                Hash(ref commandHash, cannonX);
                Publish(at, V12TrialEventKind.AimChanged, 0, 0, cannonX);
            }
            input.Remove(tick);
        }

        private void Fire(int tick, V12TickTime at)
        {
            int capturedX = cannonX;
            bool targetsBox = boxHp > 0 && Math.Abs(capturedX - box.X) <= box.Radius;
            int copies = boosts.ActiveMultiplier;
            // Count the committed but not-yet-visible follower as real
            // incoming damage, to preserve one-active/one-queued reward
            // reservation and prevent duplicate terminal promises.
            if (targetsBox && (boosts.OccupiedSlots >= 2 ||
                (!boxPromise && CountApproachingBox() + copies >= boxHp && !boosts.CanReserve)))
            {
                Publish(at, V12TrialEventKind.ShotSuppressed, 0, 0, capturedX);
                return;
            }
            if (targetsBox && !boxPromise && CountApproachingBox() + copies >= boxHp)
            {
                if (!boosts.ReserveTerminal())
                    throw new InvalidOperationException("Terminal entitlement not available before dispatch.");
                boxPromise = true;
            }
            rawShots++;
            Publish(at, V12TrialEventKind.RawShot, 0, 0, copies);
            SpawnBot(tick, at, capturedX, targetsBox);
            if (copies == 2)
            {
                // This is the SECOND REAL MiniBot, not a display duplicate.
                // Preserve shot ownership/aim/box intent through movement,
                // boost expiry and later player input. No side offsets.
                pendingFollowups.Add(new PendingFollowup(
                    tick + BoostFollowupDelayTicks, capturedX, targetsBox));
            }
        }

        private void EmitDueFollowups(int tick, V12TickTime at)
        {
            for (int i = 0; i < pendingFollowups.Count;)
            {
                PendingFollowup pending = pendingFollowups[i];
                if (pending.DueTick > tick) { i++; continue; }
                if (pending.DueTick != tick)
                    throw new InvalidOperationException("Missed deterministic followup emission tick.");
                pendingFollowups.RemoveAt(i);
                // If the chest was already destroyed by the first bot,
                // convert this promised follower into a normal ATTACK bot,
                // just like any previously launched box-directed survivors.
                SpawnBot(tick, at, pending.CapturedX,
                    pending.BoxDirected && boxHp > 0);
            }
        }

        private void SpawnBot(int tick, V12TickTime at, int capturedX, bool targetsBox)
        {
            var bot = new Bot
            {
                Id = nextUnitId++, X = capturedX, BornTick = tick,
                HP = cfg.MiniBotMaxHP, MaxHP = cfg.MiniBotMaxHP,
                Z = 0, BoxDirected = targetsBox
            };
            bots.Add(bot);
            botsProduced++;
            Publish(at, V12TrialEventKind.BotSpawned, bot.Id, 0, bot.HP);
        }

        private void SpawnWave(int tick, V12TickTime at)
        {
            // Wave lane -1/+1 still identifies authored subgroups and stable
            // spawn ordering. P2 enemies converge on ONE central physical
            // combat corridor; red model separation is presentation-only.
            // Green shots aimed at the same center X can now truly collide.
            int centerX = cfg.CannonMinX + (cfg.CannonMaxX - cfg.CannonMinX) / 2;
            for (int w = 0; w < cfg.Waves.Length; w++)
            {
                V12WaveBalance wave = cfg.Waves[w];
                if (wave.Tick != tick) continue;
                groupsSpawned++;
                // Data authored order is canonical: WaveId, GroupId, Lane, slot.
                for (int j = 0; j < wave.Spawns.Length; j++)
                {
                    V12WaveSpawn spawn = wave.Spawns[j];
                    V12EnemyBalance type = FindEnemy(spawn.EnemyKey);
                    for (int slot = 0; slot < spawn.Count; slot++)
                    {
                        var enemy = new Enemy
                        {
                            Id = nextUnitId++, X = centerX,
                            HP = type.MaxHP, MaxHP = type.MaxHP, Damage = type.Damage,
                            Speed = type.SpeedPerTick, Cooldown = type.ContactCooldownTicks,
                            BornTick = tick, Z = cfg.EnemyPathLength
                        };
                        enemies.Add(enemy);
                        enemiesSpawned++;
                        Publish(at, V12TrialEventKind.EnemySpawned, enemy.Id, w + 1, enemy.HP);
                    }
                }
            }
        }

        private V12EnemyBalance FindEnemy(string key)
        {
            for (int i = 0; i < cfg.EnemyTypes.Length; i++)
                if (cfg.EnemyTypes[i].Key == key) return cfg.EnemyTypes[i];
            throw new InvalidOperationException("Missing validated enemy type.");
        }

        private List<Candidate> Discover(int tick)
        {
            var result = new List<Candidate>();
            for (int i = 0; i < bots.Count; i++)
            {
                Bot bot = bots[i];
                if (!bot.Alive || !CanMove(bot, tick)) continue;
                if (bot.BoxDirected && boxHp > 0)
                {
                    if (TryCrossForward(bot.Z, cfg.MiniBotSpeedPerTick,
                        box.Position - box.Radius, tick, out V12TickTime hit))
                        result.Add(new Candidate(hit, CandidateKind.Box, bot, null));
                }
                else if (TryCrossForward(bot.Z, cfg.MiniBotSpeedPerTick,
                    cfg.AttackPathLength, tick, out V12TickTime impact))
                    result.Add(new Candidate(impact, CandidateKind.Fortress, bot, null));
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e.Alive && CanMove(e, tick) &&
                    TryCrossBackward(e.Z, e.Speed, 0, tick, out V12TickTime breach))
                    result.Add(new Candidate(breach, CandidateKind.Breach, null, e));
            }
            for (int i = 0; i < bots.Count; i++)
            {
                Bot bot = bots[i];
                if (!bot.Alive || bot.BornTick >= tick || bot.ResumeTick > tick) continue;
                for (int j = 0; j < enemies.Count; j++)
                {
                    Enemy e = enemies[j];
                    if (!e.Alive || e.BornTick >= tick || Math.Abs(bot.X - e.X) > cfg.ContactRadius) continue;
                    if (bot.EngagedEnemyId != 0 && bot.EngagedEnemyId != e.Id) continue;
                    if (bot.EngagedEnemyId == e.Id)
                    {
                        V12TickTime nextBot = ReadyAt(bot.HasStruck, bot.LastStrike,
                            cfg.MiniBotContactCooldownTicks, tick);
                        V12TickTime nextEnemy = ReadyAt(e.HasStruck, e.LastStrike, e.Cooldown, tick);
                        V12TickTime soonest = nextBot.CompareTo(nextEnemy) <= 0 ? nextBot : nextEnemy;
                        if (soonest.CompareTo(new V12TickTime(tick + 1)) < 0)
                            result.Add(new Candidate(soonest, CandidateKind.Combat, bot, e));
                        continue;
                    }
                    if (!CanMove(bot, tick)) continue;
                    bool enemyMoves = CanMove(e, tick);
                    long gap = e.Z - bot.Z - cfg.ContactRadius;
                    if (gap < -cfg.ContactRadius) continue; // Already passed behind one another.
                    long rate = cfg.MiniBotSpeedPerTick + (enemyMoves ? e.Speed : 0);
                    if (gap <= rate && rate > 0)
                    {
                        V12TickTime contact = gap <= 0 ? new V12TickTime(tick) :
                            new V12TickTime(tick, gap, rate);
                        if (contact.CompareTo(new V12TickTime(tick + 1)) < 0)
                            result.Add(new Candidate(contact, CandidateKind.Combat, bot, e));
                    }
                }
            }
            return result;
        }

        private void CombatBatch(List<Candidate> all, int start, int end, int tick, V12TickTime time)
        {
            // A bot can choose only one target per bucket. Sorting candidates
            // by stable IDs makes acquisition independent of list enumeration.
            var chosen = new Dictionary<long, Enemy>();
            for (int i = start; i < end; i++)
            {
                Bot bot = all[i].Bot;
                Enemy enemy = all[i].Enemy;
                if (!bot.Alive || !enemy.Alive || bot.ResumeTick > tick ||
                    (bot.EngagedEnemyId != 0 && bot.EngagedEnemyId != enemy.Id)) continue;
                if (!chosen.TryGetValue(bot.Id, out Enemy old) ||
                    ContactDistanceSquared(bot, enemy, tick, time) < ContactDistanceSquared(bot, old, tick, time) ||
                    (ContactDistanceSquared(bot, enemy, tick, time) == ContactDistanceSquared(bot, old, tick, time) && enemy.Id < old.Id))
                    chosen[bot.Id] = enemy;
            }
            if (chosen.Count == 0) return;
            var ordered = new List<Bot>();
            for (int i = 0; i < bots.Count; i++)
                if (chosen.ContainsKey(bots[i].Id)) ordered.Add(bots[i]);
            ordered.Sort((a, b) => a.Id.CompareTo(b.Id));
            var enemyDamage = new Dictionary<long, int>();
            var botDamage = new Dictionary<long, int>();
            for (int i = 0; i < ordered.Count; i++)
            {
                Bot bot = ordered[i];
                Enemy enemy = chosen[bot.Id];
                if (bot.EngagedEnemyId == 0)
                {
                    bot.EngagedEnemyId = enemy.Id;
                    AnchorBot(bot, tick, time);
                }
                AnchorEnemy(enemy, tick, time);
                if (IsReady(bot.HasStruck, bot.LastStrike, cfg.MiniBotContactCooldownTicks, time))
                {
                    enemyDamage[enemy.Id] = enemyDamage.TryGetValue(enemy.Id, out int total) ?
                        total + cfg.MiniBotDamage : cfg.MiniBotDamage;
                    bot.HasStruck = true; bot.LastStrike = time;
                    Publish(time, V12TrialEventKind.BotStruck, bot.Id, enemy.Id, cfg.MiniBotDamage);
                }
            }
            // One retaliation per eligible enemy, regardless of number of bots.
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (!enemy.Alive || !IsReady(enemy.HasStruck, enemy.LastStrike, enemy.Cooldown, time)) continue;
                Bot target = null;
                long closest = long.MaxValue;
                for (int j = 0; j < bots.Count; j++)
                {
                    Bot bot = bots[j];
                    if (!bot.Alive || bot.EngagedEnemyId != enemy.Id) continue;
                    long dx = bot.X - (long)enemy.X;
                    long dz = bot.Z - enemy.Z;
                    long distance = checked(dx * dx + dz * dz);
                    if (target == null || distance < closest || (distance == closest && bot.Id < target.Id))
                    { target = bot; closest = distance; }
                }
                if (target == null) continue;
                botDamage[target.Id] = botDamage.TryGetValue(target.Id, out int total) ?
                    total + enemy.Damage : enemy.Damage;
                enemy.HasStruck = true; enemy.LastStrike = time;
                Publish(time, V12TrialEventKind.EnemyStruck, enemy.Id, target.Id, enemy.Damage);
            }
            // Simultaneous pre-HP exchange: fatalities never cancel legal strikes.
            for (int i = 0; i < bots.Count; i++)
            {
                Bot bot = bots[i];
                if (!bot.Alive || !botDamage.TryGetValue(bot.Id, out int damage)) continue;
                bot.HP -= damage;
                if (bot.HP <= 0) KillBot(bot, tick, time);
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (!enemy.Alive || !enemyDamage.TryGetValue(enemy.Id, out int damage)) continue;
                enemy.HP -= damage;
                if (enemy.HP <= 0)
                {
                    enemy.Alive = false; enemiesKilled++;
                    Publish(time, V12TrialEventKind.EnemyDestroyed, enemy.Id, 0, 0);
                    for (int j = 0; j < bots.Count; j++)
                    {
                        Bot bot = bots[j];
                        if (bot.Alive && bot.EngagedEnemyId == enemy.Id)
                        { bot.EngagedEnemyId = 0; bot.ResumeTick = tick + 1; bot.Stopped = true; }
                    }
                }
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy.Alive && enemy.Stopped && !HasEngagedBot(enemy.Id)) enemy.ResumeTick = tick + 1;
            }
            RefreshBoxPromise();
        }

        private void BoxImpact(Candidate candidate, int tick)
        {
            Bot bot = candidate.Bot;
            if (!bot.Alive || boxHp <= 0 || !bot.BoxDirected ||
                (bot.Stopped && bot.ResumeTick > tick && bot.EngagedEnemyId == 0 &&
                 !WasContactAtSameTime(bot, tick, candidate.Time))) return;
            // Equal enemy/box time: combat already committed; a survivor may
            // still sacrifice itself on the reserved box contact.
            if (bot.EngagedEnemyId != 0 && !WasContactAtSameTime(bot, tick, candidate.Time)) return;
            AnchorBot(bot, tick, candidate.Time);
            boxHp--;
            boxHits++;
            Publish(candidate.Time, V12TrialEventKind.BoxDamaged, bot.Id, 1, boxHp);
            KillBot(bot, tick, candidate.Time);
            if (boxHp == 0)
            {
                Publish(candidate.Time, V12TrialEventKind.BoxBroken, bot.Id, 1, 0);
                if (!boxPromise)
                {
                    // Defensive fail-closed path: terminal entitlement must be
                    // held before dispatch, otherwise no silent lost reward.
                    throw new InvalidOperationException("Terminal box reward had no pre-dispatch promise.");
                }
                boosts.Earn(box.Multiplier, box.DurationTicks, tick, true);
                boxPromise = false;
                Publish(candidate.Time, V12TrialEventKind.RewardEarned, bot.Id, 1, box.Multiplier);
                for (int i = 0; i < bots.Count; i++) bots[i].BoxDirected = false;
            }
            RefreshBoxPromise();
        }

        private bool WasContactAtSameTime(Bot bot, int tick, V12TickTime time)
        {
            // The current contact anchor is precisely the candidate time;
            // same-time combat has already marked Stopped. Other contacts are
            // blocked by their ResumeTick until the next tick.
            return bot.Stopped && bot.LastContactTick == tick && bot.LastContactTime.Equals(time);
        }

        private void FortressImpact(Candidate candidate, int tick)
        {
            Bot bot = candidate.Bot;
            if (!bot.Alive || bot.Stopped || bot.EngagedEnemyId != 0) return;
            fortressHp = Math.Max(0, fortressHp - 1);
            Publish(candidate.Time, V12TrialEventKind.FortressDamaged, bot.Id, 0, fortressHp);
            KillBot(bot, tick, candidate.Time);
        }

        private void BaseBreach(Candidate candidate, int tick)
        {
            Enemy enemy = candidate.Enemy;
            if (!enemy.Alive || enemy.Stopped || HasEngagedBot(enemy.Id)) return;
            baseHp = Math.Max(0, baseHp - 1);
            enemy.Alive = false;
            Publish(candidate.Time, V12TrialEventKind.BaseBreached, enemy.Id, 0, baseHp);
        }

        private void KillBot(Bot bot, int tick, V12TickTime time)
        {
            if (!bot.Alive) return;
            bot.Alive = false;
            Publish(time, V12TrialEventKind.BotDestroyed, bot.Id, 0, 0);
        }

        private void AnchorBot(Bot bot, int tick, V12TickTime time)
        {
            if (bot.Stopped) return;
            bot.Z = checked(bot.StartZ + cfg.MiniBotSpeedPerTick * time.Numerator / time.Denominator);
            bot.Stopped = true;
            bot.LastContactTick = tick;
            bot.LastContactTime = time;
        }

        private void AnchorEnemy(Enemy enemy, int tick, V12TickTime time)
        {
            if (enemy.Stopped) return;
            // A bot is marked engaged immediately before this call. Do not
            // consult HasEngagedBot here, or the enemy would fail to move to
            // the actual contact point while the bot is anchored correctly.
            if (enemy.BornTick < tick && enemy.ResumeTick <= tick)
                enemy.Z = checked(enemy.StartZ - enemy.Speed * time.Numerator / time.Denominator);
            enemy.Stopped = true;
        }

        private long ContactDistanceSquared(Bot bot, Enemy enemy, int tick, V12TickTime time)
        {
            long botZ = bot.EngagedEnemyId == 0 && bot.BornTick < tick ?
                bot.StartZ + cfg.MiniBotSpeedPerTick * time.Numerator / time.Denominator : bot.Z;
            long enemyZ = !enemy.Stopped && enemy.BornTick < tick && enemy.ResumeTick <= tick ?
                enemy.StartZ - enemy.Speed * time.Numerator / time.Denominator : enemy.Z;
            long dx = bot.X - (long)enemy.X;
            long dz = botZ - enemyZ;
            return checked(dx * dx + dz * dz);
        }

        private bool HasEngagedBot(long enemyId)
        {
            for (int i = 0; i < bots.Count; i++)
                if (bots[i].Alive && bots[i].EngagedEnemyId == enemyId) return true;
            return false;
        }

        private bool CanMove(Bot bot, int tick)
        {
            return bot.BornTick < tick && bot.ResumeTick <= tick && bot.EngagedEnemyId == 0;
        }

        private bool CanMove(Enemy enemy, int tick)
        {
            return enemy.BornTick < tick && enemy.ResumeTick <= tick && !HasEngagedBot(enemy.Id);
        }

        private static V12TickTime ReadyAt(bool hasStruck, V12TickTime last, int cooldown, int tick)
        {
            V12TickTime boundary = new V12TickTime(tick);
            if (!hasStruck) return boundary;
            V12TickTime eligible = last.AfterCooldown(cooldown);
            return eligible.CompareTo(boundary) < 0 ? boundary : eligible;
        }

        private static bool IsReady(bool hasStruck, V12TickTime last, int cooldown, V12TickTime at)
        {
            return !hasStruck || at.CompareTo(last.AfterCooldown(cooldown)) >= 0;
        }

        private static bool TryCrossForward(long pos, long speed, long threshold, int tick, out V12TickTime time)
        {
            if (pos > threshold || pos + speed < threshold)
            { time = default(V12TickTime); return false; }
            time = new V12TickTime(tick, Math.Max(0, threshold - pos), speed);
            return time.CompareTo(new V12TickTime(tick + 1)) < 0;
        }

        private static bool TryCrossBackward(long pos, long speed, long threshold, int tick, out V12TickTime time)
        {
            if (pos < threshold || pos - speed > threshold)
            { time = default(V12TickTime); return false; }
            time = new V12TickTime(tick, Math.Max(0, pos - threshold), speed);
            return time.CompareTo(new V12TickTime(tick + 1)) < 0;
        }

        private int CountApproachingBox()
        {
            int count = 0;
            for (int i = 0; i < bots.Count; i++)
                if (bots[i].Alive && bots[i].BoxDirected) count++;
            for (int i = 0; i < pendingFollowups.Count; i++)
                if (pendingFollowups[i].BoxDirected) count++;
            return count;
        }

        private void RefreshBoxPromise()
        {
            if (boxHp <= 0) return;
            if (boxPromise && CountApproachingBox() < boxHp)
            {
                boosts.ReleaseTerminal();
                boxPromise = false;
            }
        }

        private void Publish(V12TickTime time, V12TrialEventKind kind, long actor, long target, int value)
        {
            var entry = new V12TrialEvent(time, kind, actor, target, value);
            tickEvents.Add(entry);
            history.Add(entry);
            Hash(ref eventHash, time.Tick);
            Hash(ref eventHash, time.Numerator);
            Hash(ref eventHash, time.Denominator);
            Hash(ref eventHash, (int)kind);
            Hash(ref eventHash, actor);
            Hash(ref eventHash, target);
            Hash(ref eventHash, value);
        }

        private V12TrialSnapshot MakeSnapshot(int tick)
        {
            var liveBots = new List<V12TrialUnitView>();
            var liveEnemies = new List<V12TrialUnitView>();
            ulong hash = 14695981039346656037UL;
            Hash(ref hash, match.ConfigHash);
            Hash(ref hash, tick);
            Hash(ref hash, cannonX);
            Hash(ref hash, baseHp);
            Hash(ref hash, fortressHp);
            Hash(ref hash, boxHp);
            Hash(ref hash, rawShots);
            Hash(ref hash, botsProduced);
            Hash(ref hash, enemiesSpawned);
            Hash(ref hash, enemiesKilled);
            Hash(ref hash, groupsSpawned);
            Hash(ref hash, boosts.ActiveMultiplier);
            Hash(ref hash, boosts.ActiveEndsAtTick);
            Hash(ref hash, boosts.PendingMultiplier);
            Hash(ref hash, boosts.PendingDurationTicks);
            Hash(ref hash, boosts.PendingEarliestTick);
            Hash(ref hash, boosts.TerminalPromises);
            Hash(ref hash, (int)outcome);
            Hash(ref hash, eventHash);
            Hash(ref hash, commandHash);
            // Pending followers are authoritative scheduled entities. Include
            // their immutable capture in replay/state hashes BEFORE emission.
            Hash(ref hash, pendingFollowups.Count);
            for (int i = 0; i < pendingFollowups.Count; i++)
            {
                PendingFollowup pending = pendingFollowups[i];
                Hash(ref hash, pending.DueTick);
                Hash(ref hash, pending.CapturedX);
                Hash(ref hash, pending.BoxDirected ? 1 : 0);
            }
            for (int i = 0; i < bots.Count; i++)
            {
                Bot b = bots[i];
                if (!b.Alive) continue;
                liveBots.Add(new V12TrialUnitView(b.Id, b.X, (int)b.Z, b.HP, b.MaxHP, b.BoxDirected));
                Hash(ref hash, b.Id); Hash(ref hash, b.X); Hash(ref hash, b.Z);
                Hash(ref hash, b.HP); Hash(ref hash, b.EngagedEnemyId);
                Hash(ref hash, b.BornTick); Hash(ref hash, b.ResumeTick);
                Hash(ref hash, b.HasStruck ? 1 : 0);
                if (b.HasStruck) { Hash(ref hash, b.LastStrike.Tick); Hash(ref hash, b.LastStrike.Numerator); Hash(ref hash, b.LastStrike.Denominator); }
            }
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (!e.Alive) continue;
                liveEnemies.Add(new V12TrialUnitView(e.Id, e.X, (int)e.Z, e.HP, e.MaxHP, false));
                Hash(ref hash, e.Id); Hash(ref hash, e.X); Hash(ref hash, e.Z);
                Hash(ref hash, e.HP); Hash(ref hash, e.BornTick); Hash(ref hash, e.ResumeTick);
                Hash(ref hash, e.HasStruck ? 1 : 0);
                if (e.HasStruck) { Hash(ref hash, e.LastStrike.Tick); Hash(ref hash, e.LastStrike.Numerator); Hash(ref hash, e.LastStrike.Denominator); }
            }
            return new V12TrialSnapshot(tick, nextTick, cannonX, boxHp, box.MaxHP,
                baseHp, fortressHp, rawShots, botsProduced, enemiesSpawned,
                enemiesKilled, groupsSpawned, boxHits, boosts, outcome, match.ConfigHash,
                eventHash, commandHash, hash, liveBots, liveEnemies, tickEvents);
        }

        private static int Clamp(int value, int low, int high)
        {
            return value < low ? low : value > high ? high : value;
        }

        private static void Hash(ref ulong hash, long number)
        {
            // Signed values (including negative sentinel ticks) use the same
            // canonical little-endian two's-complement bytes as before.
            Hash(ref hash, unchecked((ulong)number));
        }

        private static void Hash(ref ulong hash, ulong bits)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                { hash ^= (byte)(bits >> (i * 8)); hash *= 1099511628211UL; }
            }
        }
    }
}
