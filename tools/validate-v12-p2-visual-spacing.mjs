import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Source + geometry guard only. Unity rendering/device screenshots still need QA.
const source = readFileSync('Assets/Swarmforge/Scripts/Presentation/V12TrialView.cs', 'utf8');
const formation = readFileSync('Assets/Swarmforge/Scripts/Presentation/V12FormationLayout.cs', 'utf8');
const tests = readFileSync('Assets/Swarmforge/Tests/EditMode/V12FormationLayoutTests.cs', 'utf8');

assert.ok(source.includes('V12FormationLayout.ForId(u.Id)'), 'View must use stable visual-only formation');
assert.ok(!source.includes('((u.Id % 3) - 1) * .10f'), 'Old visibly overlapping jitter must be removed');
assert.match(formation, /\(unitId - 1\) % 4/);
assert.match(formation, /\.52f/);
assert.match(formation, /\.45f/);

const offset = id => {
    const slot = (id - 1) % 4;
    return [(slot & 1) === 0 ? -.52 : .52, slot < 2 ? -.45 : .45];
};
for (let id = 1; id < 100; id++) {
    const [ax, az] = offset(id), [bx, bz] = offset(id + 1);
    assert.ok(Math.hypot(ax - bx, az - bz) > .88, `Adjacent IDs ${id}/${id+1} overlap`);
}
for (const name of [
    'ConsecutiveIds_StayVisuallySeparateWithinP2Group',
    'FourSlotFormation_IsBoundedAndPairwiseSeparated',
    'Formation_IsStableIndependentOfQueryOrderAndFrame',
    'PresentationOnlyOffsets_CannotChangeAuthoritativeStateHash'])
    assert.ok(tests.includes(name), `Missing EditMode regression ${name}`);

console.log('P2 stable formation geometry/source regression: PASS');
console.log('Visual-only offsets: bounded X +/-0.52, Z +/-0.45, adjacent-ID separation >=0.90 world units');
console.log('This is not a Unity render/PlayMode test: actual screenshots still required.');
