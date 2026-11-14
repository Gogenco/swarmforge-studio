import fs from 'node:fs';
import path from 'node:path';

// Dependency-free static assertions for CI environments without Unity/C#.
// This is NOT a C# compiler or a substitute for Unity EditMode tests.
const root = path.resolve(import.meta.dirname, '..');
const read = (name) => fs.readFileSync(path.join(root, name), 'utf8');
const errors = [];
const check = (condition, message) => { if (!condition) errors.push(message); };
const folder = 'Assets/Swarmforge/Configs/Trials';
const two = read(`${folder}/V12_HP2_Trial.asset`);
const three = read(`${folder}/V12_HP3_Trial.asset`);

function simpleField(source, name) {
  const match = source.match(new RegExp(`^\\s*${name}: (.+)$`, 'm'));
  return match?.[1];
}

for (const [name, asset, hp] of [['HP2', two, 2], ['HP3', three, 3]]) {
  check(simpleField(asset, 'SchemaVersion') === '2', `${name}: wrong schema`);
  check(simpleField(asset, 'RulesetId') === 'SWARMFORGE_V12_TRIAL', `${name}: wrong opt-in ruleset`);
  check(simpleField(asset, 'TickRate') === '30', `${name}: simulation must stay 30Hz`);
  check(simpleField(asset, 'MatchTicks') === '1800', `${name}: match length changed`);
  check(simpleField(asset, 'MiniBotMaxHP') === String(hp), `${name}: wrong MiniBot HP`);
  check(simpleField(asset, 'ShotPeriodTicks') === '10', `${name}: fire cadence changed`);
  check(/MaxHP: 5\s+Multiplier: 2\s+DurationTicks: 360/.test(asset), `${name}: x2 box rules mismatch`);
  check(/MaxHP: 10\s+Multiplier: 3\s+DurationTicks: 300/.test(asset), `${name}: x3 box rules mismatch`);
  check(/MaxHP: 25\s+Multiplier: 5\s+DurationTicks: 180/.test(asset), `${name}: x5 box rules mismatch`);
  check(/Spawns:\s+- EnemyKey: SCOUT\s+Lane: -1\s+Count: 2\s+- EnemyKey: SCOUT\s+Lane: 1\s+Count: 2/.test(asset), `${name}: grouped wave fixture mismatch`);
}

// A matched HP2-vs-HP3 trial may differ only in its profile ID / robot HP.
const normalize = (source) => source
  .replace(/m_Name: V12_HP[23]_Trial/g, 'm_Name: V12_HPX_Trial')
  .replace(/ProfileId: HP[23]_TRIAL/g, 'ProfileId: HPX_TRIAL')
  .replace(/MiniBotMaxHP: [23]/g, 'MiniBotMaxHP: X');
check(normalize(two) === normalize(three), 'Presets are not identical except name/profile/HP');

const infra = read('Assets/Swarmforge/Scripts/Infrastructure/V12BalanceConfigAsset.cs');
const sim = read('Assets/Swarmforge/Scripts/Simulation/Core/V12BalanceDefinition.cs');
const match = read('Assets/Swarmforge/Scripts/Simulation/Core/V12MatchConfig.cs');
const test = read('Assets/Swarmforge/Tests/EditMode/V12BalanceConfigurationTests.cs');
const guid = read('Assets/Swarmforge/Scripts/Infrastructure/V12BalanceConfigAsset.cs.meta')
  .match(/guid: ([0-9a-f]{32})/)?.[1];
check(Boolean(guid && two.includes(`guid: ${guid}`) && three.includes(`guid: ${guid}`)),
  'Trial assets do not reference the new ScriptableObject GUID');
check(infra.includes('CreateMatchConfig()') && infra.includes('ScriptableObject'),
  'Explicit ScriptableObject opt-in bridge missing');
check(sim.includes('new V12BalanceDefinition') && sim.includes('V12WaveBalance') && sim.includes('Validate()'),
  'Editable prototype schema/validation missing');
check(match.includes('settings = definition.Copy()') && match.includes('ConfigHash') &&
  match.includes('LegacyUnversioned') && match.includes('RequireCompatible'),
  'Snapshot and replay compatibility seam missing');
check(!/\bUnityEngine\b/.test(sim + match), 'Simulation P1 classes depend on UnityEngine');
check(test.match(/\[Test\]/g)?.length >= 9, 'Focused EditMode regressions missing');

// The approved legacy Battle scene must not load any v1.2 opt-in profile.
const scene = read('Assets/Swarmforge/Scenes/Battle.unity');
check(!scene.includes(guid), 'Legacy Battle scene references an opt-in v1.2 profile');

console.log(`P1 configurable balance profile static checks: ${errors.length ? 'FAIL' : 'PASS'}`);
console.log('Presets: HP2 / HP3 matched except profile identity and MiniBot HP');
console.log('Approved box HP/tick durations: 5/360, 10/300, 25/180');
console.log('Unity compilation, EditMode/PlayMode, gameplay/QA: NOT RUN');
for (const issue of errors) console.error('ERROR:', issue);
process.exitCode = errors.length ? 1 : 0;
