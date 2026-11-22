import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Static, deterministic source/profile consistency check. It does not compile
// C# or stand in for the Unity EditMode/PlayMode Test Runner.
const read = path => readFileSync(path, 'utf8');
const hp2 = read('Assets/Swarmforge/Configs/Trials/P2_HP2.asset');
const hp3 = read('Assets/Swarmforge/Configs/Trials/P2_HP3.asset');
const preset = read('Assets/Swarmforge/Scripts/Simulation/Core/V12P2Presets.cs');
const oldPreset = read('Assets/Swarmforge/Scripts/Simulation/Core/V12BalanceDefinition.cs');
const simulation = read('Assets/Swarmforge/Scripts/Simulation/Core/V12TrialSimulation.cs');
const entrypoint = read('Assets/Swarmforge/Scripts/Presentation/V12TrialEntryPoint.cs');
const tests = read('Assets/Swarmforge/Tests/EditMode/V12P2PlayableTests.cs');
const playTests = read('Assets/Swarmforge/Tests/PlayMode/V12TrialSceneSmokeTests.cs');

function field(src, name) {
    const match = src.match(new RegExp(`^\\s+${name}: (\\d+)$`, 'm'));
    assert.ok(match, `Missing asset field ${name}`);
    return Number(match[1]);
}

for (const [name, content, hp] of [['HP2', hp2, 2], ['HP3', hp3, 3]]) {
    for (const [key, value] of Object.entries({
        TickRate: 30, MatchTicks: 1800, FirstShotTick: 0, ShotPeriodTicks: 30,
        BaseHP: 3, FortressHP: 40, MiniBotMaxHP: hp,
        MiniBotSpeedPerTick: 340, AttackPathLength: 51000,
        DurationTicks: 360, Multiplier: 2,
    })) assert.equal(field(content, key), value, `P2 ${name} field ${key}`);
    assert.ok(content.includes(`ProfileId: P2_${name}`));
}
const normalizedProfile = (source, hp) => source
    .replaceAll(`P2_HP${hp}`, 'P2_HP_TEST')
    .replace(`MiniBotMaxHP: ${hp}`, 'MiniBotMaxHP: TEST');
assert.equal(normalizedProfile(hp2, 2), normalizedProfile(hp3, 3),
    'Authored HP2/HP3 profiles must differ only by their identity and bot HP.');
assert.match(preset, /value\.ShotPeriodTicks\s*=\s*30\s*;/);
assert.match(preset, /value\.FortressHP\s*=\s*40\s*;/);
assert.match(oldPreset, /public int ShotPeriodTicks = 10;/,
    'The inherited P1 cadence must remain unchanged.');
assert.match(oldPreset, /public int FortressHP = 100;/,
    'The inherited P1 fortress HP must remain unchanged.');
assert.match(simulation, /\(tick - cfg\.FirstShotTick\) % cfg\.ShotPeriodTicks == 0/);
assert.match(simulation, /int copies = boosts\.ActiveMultiplier;/);
assert.match(entrypoint, /AUTO-FIRE: x1 1\/s    x2 2\/s/);
for (const name of [
    'FireRate_X1_ExactlyOneBotEveryThirtyTicksFromTickZero',
    'FireRate_X2_TwoBotsAtEachShot_ThenReturnsToOne',
    'Restart_NewSimulationResetsOnePerSecondScheduleAndReplayState',
    'Terminal_HighFortressAndTenBaseHp_ExpiresAsTimeoutAfterExactly1800Ticks',
    'Balance_CanonicalFortress40FitsOptimisticDamageBudget',
    'CanonicalP2_SideBoxThenCentralGroupCombat_ReachableVictory',
    'Replay_IdenticalCommandsProduceIdenticalEventAndStateHashes',
    'Terminal_AllFourEnemiesBreachMeansDefeat',
    'Terminal_FortressZeroMeansVictory'])
    assert.ok(tests.includes(name), `Missing P2 regression ${name}`);
for (const command of [
    'SubmitAim(1, 1, -950)', 'SubmitAim(151, 2, 0)',
    'terminal.Outcome, Is.EqualTo(MatchOutcome.Win)',
    'terminal.EnemiesKilled, Is.EqualTo(4)',
    'terminal.BaseHP, Is.EqualTo(3)'])
    assert.ok(tests.includes(command), `Missing canonical victory proof step: ${command}`);
assert.ok(playTests.includes('new V12MatchConfig(V12P2Presets.Create(2)).ConfigHash'));
assert.ok(playTests.includes('new V12MatchConfig(V12P2Presets.Create(3)).ConfigHash'));

const shots = [...Array(1800).keys()].filter(tick => tick % 30 === 0);
assert.equal(shots.length, 60, '60 seconds at 30 Hz must schedule 60 raw shots');
const travelTicks = Math.ceil(51000 / 340);
const eligibleShots = shots.filter(tick => tick + travelTicks < 1800);
assert.equal(eligibleShots.length, 55);
const maxBoostedShots = Math.ceil(360 / 30);
assert.equal(maxBoostedShots, 12);
const maximumDamage = eligibleShots.length + maxBoostedShots - 5;
assert.equal(maximumDamage, 62);
assert.ok(maximumDamage >= field(hp2, 'FortressHP'));

console.log('P2 ×1=1 bot/sec and ×2=2 bots/sec source/assets + test presence: PASS');
console.log('P1 ShotPeriodTicks=10 untouched; P2 HP2/HP3 ShotPeriodTicks=30; first shot tick0: PASS');
console.log('Owner HP40 approved: ideal damage upper bound 62 >= 40 fortress HP, leaving 22-bot allowance.');
console.log('Authored canonical win/lose + timeout-fixture tests are present; actual C# execution requires Unity.');
console.log('Unity compilation, EditMode, PlayMode, device test: NOT RUN by this static guard.');
