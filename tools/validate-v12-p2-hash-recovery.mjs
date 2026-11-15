import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

// Static source guard, NOT a C# compiler or Unity Test Runner substitute.
// Prevents reintroduction of the three Owner-confirmed CS1503 errors.
const source = readFileSync('Assets/Swarmforge/Scripts/Simulation/Core/V12TrialSimulation.cs', 'utf8');
const tests = readFileSync('Assets/Swarmforge/Tests/EditMode/V12P2PlayableTests.cs', 'utf8');
const scene = readFileSync('Assets/Swarmforge/Scenes/V12_Trial.unity', 'utf8');
const build = readFileSync('ProjectSettings/EditorBuildSettings.asset', 'utf8');

assert.match(source, /private static void Hash\(ref ulong hash, long number\)/);
assert.match(source, /Hash\(ref hash, unchecked\(\(ulong\)number\)\)/);
assert.match(source, /private static void Hash\(ref ulong hash, ulong bits\)/);
for (const value of ['match.ConfigHash', 'eventHash', 'commandHash'])
{
    assert.ok(source.includes(`Hash(ref hash, ${value});`), `Missing unsigned hash field: ${value}`);
}
assert.match(source, /bits >> \(i \* 8\)/);
assert.match(source, /hash \*= 1099511628211UL/);
for (const test of [
    'Hash_UnsignedHighBit_UsesEveryByteInCanonicalLittleEndianOrder',
    'Hash_SignedNegativeSentinel_PreservesOriginalEightBytes',
    'SnapshotHash_IncludesUnsignedConfigEventAndCommandFingerprints'])
{
    assert.ok(tests.includes(`void ${test}()`), `Missing regression: ${test}`);
}
assert.ok(scene.includes('guid: 9b9ac623c47b7790d477e7a1209f9cf1'));
assert.ok(build.includes('Assets/Swarmforge/Scenes/Battle.unity'));
assert.ok(build.includes('Assets/Swarmforge/Scenes/V12_Trial.unity'));

console.log('P2 Owner CS1503 source regression guard: PASS');
console.log('Unsigned overload + legacy signed bit pattern + 3 regression tests: PRESENT');
console.log('This check is lexical only; Unity compile, EditMode, PlayMode: NOT RUN');
