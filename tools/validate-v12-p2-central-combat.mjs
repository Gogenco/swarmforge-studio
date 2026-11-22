import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Geometry/model-contract source guard. Pure JS numeric checks are not Unity
// EditMode/PlayMode or authoritative C# execution.
const read = p => readFileSync(p, 'utf8');
const sim = read('Assets/Swarmforge/Scripts/Simulation/Core/V12TrialSimulation.cs');
const preset = read('Assets/Swarmforge/Scripts/Simulation/Core/V12P2Presets.cs');
const tests = read('Assets/Swarmforge/Tests/EditMode/V12P2PlayableTests.cs');
const playTests = read('Assets/Swarmforge/Tests/PlayMode/V12TrialSceneSmokeTests.cs');
const hp2 = read('Assets/Swarmforge/Configs/Trials/P2_HP2.asset');
const hp3 = read('Assets/Swarmforge/Configs/Trials/P2_HP3.asset');
const defaultP1 = read('Assets/Swarmforge/Scripts/Simulation/Core/V12BalanceDefinition.cs');

const assetField = (text, name) => {
  const m = text.match(new RegExp(`^\\s+${name}: (-?\\d+)$`, 'm'));
  assert.ok(m, `Missing ${name}`);
  return Number(m[1]);
};
for (const [label, asset, hp] of [['HP2', hp2, 2], ['HP3', hp3, 3]]) {
  const centerX = (assetField(asset, 'CannonMinX') + assetField(asset, 'CannonMaxX')) / 2;
  const radius = assetField(asset, 'ContactRadius');
  const boxX = assetField(asset, 'X');
  const boxRadius = assetField(asset, 'Radius');
  assert.equal(centerX, 0, `${label}: center combat corridor must be at X0`);
  assert.equal(boxX, -950, `${label}: P2 left chest must be shifted into the side band`);
  assert.equal(assetField(asset, 'MiniBotMaxHP'), hp);
  assert.equal(assetField(asset, 'ShotPeriodTicks'), 30);
  assert.equal(assetField(asset, 'FortressHP'), 40);
  assert.equal(assetField(asset, 'TickRate'), 30);
  assert.equal(assetField(asset, 'DurationTicks'), 360);
  assert.match(asset, /- Key: X2\n\s+X: -950\n\s+Position: 9000\n\s+Radius: 450\n\s+MaxHP: 5/); // Exactly one 5HP box
  assert.equal(asset.match(/\n    - Key: X2\n/g)?.length, 1);
  const separation = Math.abs(centerX - boxX);
  assert.ok(separation > radius + boxRadius, `${label}: chest/combat bands overlap`);
  assert.ok(Math.abs(centerX - centerX) <= radius, `${label}: central green/red must contact`);
  assert.ok(Math.abs(boxX - centerX) > radius, `${label}: side bot must not hit center enemy`);
  assert.ok(Math.abs(centerX - boxX) > boxRadius, `${label}: center aim must not hit chest`);
  // Fixed-point sweep contact: central bot and enemy approach one another at
  // 340+230 fixed units/tick and must meet before the enemy reaches the base.
  const contact = (assetField(asset, 'EnemyPathLength') - radius) /
    (assetField(asset, 'MiniBotSpeedPerTick') + 230);
  assert.ok(contact > 0 && contact < 222, `${label}: no attainable central intercept`);
}
assert.match(sim, /X = centerX,/);
assert.doesNotMatch(sim, /X = spawn\.Lane\s*\*\s*800/);
assert.match(sim, /Math\.Abs\(bot\.X - e\.X\) > cfg\.ContactRadius/);
assert.match(sim, /Math\.Abs\(\(long\)cfg\.Boxes\[0\]\.X - centerX\)/);
assert.match(sim, /\(long\)cfg\.ContactRadius \+ cfg\.Boxes\[0\]\.Radius/);
assert.match(sim, /BoxDirected = targetsBox/);
assert.match(sim, /CombatBatch\(candidates, i, end, tick, candidate\.Time\)/);
assert.match(sim, /candidate\.Kind == CandidateKind\.Box\) BoxImpact/);
assert.match(preset, /new V12BoxBalance\("X2", -950, 9000, 450, 5, 2, 360\)/);
assert.match(defaultP1, /new V12BoxBalance\("X2", -800, 9000, 450, 5, 2, 360\)/,
  'P1 three-box fixture must be preserved');
for (const name of [
  'Geometry_CentralEnemyCorridorAndSideBoxCannotOverlap',
  'CenterAimedMiniBots_FightCentralEnemies_WithRealDamageAndRetaliation',
  'SideAim_FiveBoxHitsEarnBoostWithoutInterceptingCentralEnemies',
  'CenterAim_DoesNotDamageOrAwardSideBox',
  'InvalidOverlappingP2BoxGeometry_IsRejectedBeforeMatchStarts',
  'CanonicalP2_SideBoxThenCentralGroupCombat_ReachableVictory',
  'Terminal_AllFourEnemiesBreachMeansDefeat',
  'Replay_IdenticalCommandsProduceIdenticalEventAndStateHashes'])
  assert.ok(tests.includes(name), `Missing ${name} regression`);
assert.ok(playTests.includes('V12Trial_SideBoxVisualIsClearOfCentralCombatAndSceneKeepsOneChest'));
console.log('P2 central combat versus lateral box authored geometry and source guard: PASS');
console.log('Actual Unity compilation, runtime C# collisions, HP exchange, scene render: NOT RUN.');
