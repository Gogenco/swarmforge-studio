import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Source + geometry guard only. Unity rendering/device screenshots still need QA.
const source = readFileSync('Assets/Swarmforge/Scripts/Presentation/V12TrialView.cs', 'utf8');
const formation = readFileSync('Assets/Swarmforge/Scripts/Presentation/V12FormationLayout.cs', 'utf8');
const tests = readFileSync('Assets/Swarmforge/Tests/EditMode/V12FormationLayoutTests.cs', 'utf8');
const playTests = readFileSync('Assets/Swarmforge/Tests/PlayMode/V12TrialSceneSmokeTests.cs', 'utf8');

assert.ok(source.includes('V12FormationLayout.ForId(u.Id)'), 'Red enemies must retain stable visual-only formation');
assert.ok(source.includes('V12FormationLayout.ForBot(u.Position, config.AttackPathLength,'),
    'Green bots must use travel-dependent muzzle-centered fan-out');
assert.ok(source.includes('botCohorts.GetOrAssign(u, units)'), 'Green cohort slots must be assigned once');
assert.ok(source.includes('if (greenBot) botCohorts.Forget(cleanup[i])'),
    'Per-bot presentation slots must be cleaned up with the visuals');
assert.ok(!source.includes('((u.Id % 3) - 1) * .10f'), 'Old visibly overlapping jitter must be removed');
assert.match(formation, /\(unitId - 1\) % 4/);
assert.match(formation, /\.52f/);
assert.match(formation, /\.45f/);
assert.match(formation, /public static Vector2 ForBot\(int position, int attackPathLength, float cohortSlot\)/);
assert.match(formation, /t \* t \* \(3f - 2f \* t\)/);
assert.match(formation, /public sealed class V12BotVisualCohorts/);
assert.match(formation, /if \(other\.X != bot\.X \|\| other\.Position != bot\.Position\) continue;/);
assert.match(formation, /if \(slots\.TryGetValue\(bot\.Id, out float assigned\)\) return assigned;/);

const offset = id => {
    const slot = (id - 1) % 4;
    return [(slot & 1) === 0 ? -.52 : .52, slot < 2 ? -.45 : .45];
};
for (let id = 1; id < 100; id++) {
    const [ax, az] = offset(id), [bx, bz] = offset(id + 1);
    assert.ok(Math.hypot(ax - bx, az - bz) > .88, `Adjacent IDs ${id}/${id+1} overlap`);
}
const botX = (position, pathLength, slot) => {
    if (!slot) return 0;
    const z = 18 * Math.max(0, position) / pathLength;
    const t = Math.min(1, Math.max(0, (z - .25) / (1.6 - .25)));
    return slot * .52 * t * t * (3 - 2 * t);
};
for (const position of [0, 340, 900, 2500, 5100, 51000])
    assert.equal(botX(position, 51000, 0), 0, `Solo green at ${position} must be on muzzle center`);
assert.ok(botX(0, 51000, -1) === 0);
assert.ok(botX(0, 51000, +1) === 0);
assert.ok(botX(600, 51000, -1) === 0, 'Paired robots must clear muzzle before splitting');
let previous = 0;
for (let position = 0; position <= 5100; position += 100) {
    const separation = botX(position, 51000, 1) - botX(position, 51000, -1);
    assert.ok(separation + 1e-8 >= previous, 'Twin fan-out must be monotonic');
    previous = separation;
}
assert.ok(Math.abs(previous - 1.04) < 1e-8, 'Twins must reach distinguishable spread');
for (const name of [
    'SoloMiniBot_NeverDeviatesFromExactCannonMuzzleLane',
    'BoostTwins_ShareExactMuzzleThenSmoothlyFanOutAfterTravel',
    'TwinSlot_RemainsStableWhenPartnerDisappearsOrFrameOrderChanges',
    'ConsecutiveIds_StayVisuallySeparateWithinP2Group',
    'FourSlotFormation_IsBoundedAndPairwiseSeparated',
    'Formation_IsStableIndependentOfQueryOrderAndFrame',
    'PresentationOnlyOffsets_CannotChangeAuthoritativeStateHash'])
    assert.ok(tests.includes(name), `Missing EditMode regression ${name}`);
assert.ok(playTests.includes('V12Trial_FirstSoloMiniBotRendersAtExactCannonMuzzleX'),
    'Missing Unity scene-level PlayMode smoke test for exact muzzle X');

console.log('P2 muzzle-centered green solo/twin geometry and red formation source regression: PASS');
console.log('Solo X=0 offset; twins X=0 at muzzle, smooth ±0.52 after travel; red pair formation preserved.');
console.log('This is not a Unity render/PlayMode test: actual screenshots still required.');
