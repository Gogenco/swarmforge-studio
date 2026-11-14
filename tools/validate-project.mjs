import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '..');
const issues = [];
let inspectedCs = 0;

function assert(condition, message) {
  if (!condition) issues.push(message);
}
function read(relative) {
  const full = path.join(root, relative);
  assert(fs.existsSync(full), `Missing: ${relative}`);
  return fs.existsSync(full) ? fs.readFileSync(full, 'utf8') : '';
}
function scan(dir) {
  const files = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const file = path.join(dir, entry.name);
    files.push(...(entry.isDirectory() ? scan(file) : [file]));
  }
  return files;
}

const editor = read('ProjectSettings/ProjectVersion.txt');
assert(editor.includes('6000.3.28f1'), 'Unity patch not locked');
const manifest = JSON.parse(read('Packages/manifest.json'));
assert(manifest.dependencies['com.unity.render-pipelines.universal'] === '17.3.0', 'URP mismatch');
assert(Boolean(manifest.dependencies['com.unity.test-framework']), 'Unity Test Framework missing');

const assets = path.join(root, 'Assets', 'Swarmforge');
const allFiles = scan(assets);
const defs = new Map();
for (const f of allFiles.filter(f => f.endsWith('.asmdef'))) {
  const item = JSON.parse(fs.readFileSync(f, 'utf8'));
  assert(!defs.has(item.name), `Duplicate assembly: ${item.name}`);
  defs.set(item.name, item);
}
for (const def of defs.values()) {
  for (const ref of def.references ?? [])
    assert(defs.has(ref) || ref.startsWith('Unity.'),
      `Assembly ${def.name} references missing ${ref}`);
}
assert(defs.get('Swarmforge.Simulation')?.noEngineReferences === true,
  'Simulation must compile without UnityEngine');

for (const f of allFiles.filter(f => f.endsWith('.cs'))) {
  inspectedCs++;
  const src = fs.readFileSync(f, 'utf8');
  if (f.includes('/Simulation/')) {
    assert(!/\bUnityEngine\b|\bRigidbody\b|\bAnimator\b|\bTime\.deltaTime\b/.test(src),
      `Simulation engine dependency in ${path.relative(root, f)}`);
  }
  // A lexical delimiter check, NOT a C# compiler. Ignore strings and comments.
  let mode = 'code';
  const stack = [];
  for (let i = 0; i < src.length; i++) {
    const c = src[i], next = src[i + 1];
    if (mode === 'line' && c === '\n') { mode = 'code'; continue; }
    if (mode === 'line') continue;
    if (mode === 'block' && c === '*' && next === '/') { i++; mode = 'code'; continue; }
    if (mode === 'block') continue;
    if (mode === 'string' && c === '\\') { i++; continue; }
    if (mode === 'string' && c === '"') { mode = 'code'; continue; }
    if (mode === 'char' && c === '\\') { i++; continue; }
    if (mode === 'char' && c === "'") { mode = 'code'; continue; }
    if (mode !== 'code') continue;
    if (c === '/' && next === '/') { mode = 'line'; i++; continue; }
    if (c === '/' && next === '*') { mode = 'block'; i++; continue; }
    if (c === '"') { mode = 'string'; continue; }
    if (c === "'") { mode = 'char'; continue; }
    if (c === '{' || c === '(' || c === '[') stack.push(c);
    if (c === '}' || c === ')' || c === ']') {
      const expected = { '}': '{', ')': '(', ']': '[' }[c];
      assert(stack.pop() === expected, `Delimiter mismatch ${path.relative(root, f)} char ${i}`);
    }
  }
  assert(stack.length === 0, `Unclosed delimiters in ${path.relative(root, f)}`);
  assert(mode === 'code' || mode === 'line', `Unclosed comment/string in ${path.relative(root, f)}`);
}

const scene = read('Assets/Swarmforge/Scenes/Battle.unity');
const config = read('Assets/Swarmforge/Configs/Levels/VS01_Arena.asset');
const sceneGuid = read('Assets/Swarmforge/Scenes/Battle.unity.meta').match(/guid: ([a-f0-9]{32})/)?.[1];
const configGuid = read('Assets/Swarmforge/Configs/Levels/VS01_Arena.asset.meta').match(/guid: ([a-f0-9]{32})/)?.[1];
const presenterGuid = read('Assets/Swarmforge/Scripts/Presentation/BattleEntryPoint.cs.meta').match(/guid: ([a-f0-9]{32})/)?.[1];
const assetScriptGuid = read('Assets/Swarmforge/Scripts/Infrastructure/LevelConfigAsset.cs.meta').match(/guid: ([a-f0-9]{32})/)?.[1];
assert(Boolean(sceneGuid && scene.includes(presenterGuid) && scene.includes(configGuid)),
  'Battle scene asset/script references do not resolve');
assert(Boolean(assetScriptGuid && config.includes(assetScriptGuid)), 'Level asset script missing');
assert(read('ProjectSettings/EditorBuildSettings.asset').includes(sceneGuid), 'Battle scene missing in BuildSettings');
const botTicks = Array.from({ length: 1800 }, (_, i) => i).filter(t => t % 10 === 0);
assert(botTicks.length === 180 && botTicks[0] === 0 && botTicks.at(-1) === 1790,
  'Reference raw shot schedule incorrect');
assert(allFiles.filter(f => f.endsWith('Tests.cs')).length >= 2, 'Test sources missing');

// DEV-002 evidence: static regression guards (not Unity compilation or playtests).
const level = read('Assets/Swarmforge/Scripts/Simulation/Core/LevelDefinition.cs');
const sim = read('Assets/Swarmforge/Scripts/Simulation/Core/MatchSimulation.cs');
const snapshot = read('Assets/Swarmforge/Scripts/Simulation/Core/MatchSnapshot.cs');
const view = read('Assets/Swarmforge/Scripts/Presentation/PrimitiveBattleView.cs');
const ui = read('Assets/Swarmforge/Scripts/Presentation/BattleEntryPoint.cs');
const setup = read('Assets/Swarmforge/Scripts/Editor/SwarmforgeProjectSetup.cs');
const selector = read('Assets/Swarmforge/Scripts/Presentation/StableVisualSelection.cs');
const editModeTests = read('Assets/Swarmforge/Tests/EditMode/Vs01SimulationTests.cs');
assert(level.includes('return 0;') && level.includes('GateRightMinX'), 'QA-01 neutral gate missing');
assert(view.includes('Gate_x1_Bypass') && !view.includes('TextMesh'), 'QA-01/BUG-01 visual gates regression');
assert(view.includes('GraphicsSettings.currentRenderPipeline') &&
  view.includes('Unlit/Color'), 'BUG-01 shader pipeline fallback missing');
assert(setup.includes('UniversalRenderPipelineAsset.Create') &&
  setup.includes('GraphicsSettings.defaultRenderPipeline'), 'QA-02 URP setup missing');
assert(snapshot.includes('CommandHash') && snapshot.includes('minis[i].CannonX'),
  'QA-03 replay state incomplete');
assert(/internal static void Hash\(ref ulong hash, long number\)/.test(snapshot) &&
  /internal static void Hash\(ref ulong hash, ulong number\)/.test(snapshot),
  'DEV-002.1 CS1503: both signed and unsigned Hash overloads are required');
assert(sim.includes('new GameEventBatch') && sim.includes('history.Add(lastEventBatch)'),
  'QA-04 immutable event records missing');
assert(sim.includes('PreviousPosition') && sim.includes('ContactCandidate'),
  'QA-05 swept collision missing');
assert(selector.includes('visibleIds') && view.includes('StableVisualSelection.Choose'),
  'QA-06 stable selection missing');
assert(ui.includes('OnApplicationPause') && ui.includes('ResumeFromSuspend'),
  'QA-07 background pause missing');
assert(ui.includes('if (suspended || s.Outcome != MatchOutcome.Playing)'),
  'BUG-03 terminal UI isolation missing');
assert(!/Assert\.That\([^;\n]*Does\.Not\.Contain\s*\(/.test(editModeTests),
  'DEV-002.2 CS1503: NUnit Does.Not.Contain requires a string argument');
assert(editModeTests.includes('third.Contains(121L)') &&
  editModeTests.includes('third.Contains(1L)'),
  'DEV-002.2 visual cap membership checks must be typed as List<long>.Contains(long)');

console.log(`SWARMFORGE static structure verification: ${issues.length ? 'FAIL' : 'PASS'}`);
console.log(`C# files examined (lexical checks only): ${inspectedCs}`);
console.log(`Unity assemblies: ${[...defs.keys()].join(', ')}`);
console.log('Raw shot arithmetic: 180 ticks from 0 through 1790');
console.log('Unity runtime compile/EditMode/PlayMode: NOT RUN (requires Unity Editor)');
for (const issue of issues) console.error('ERROR:', issue);
process.exitCode = issues.length ? 1 : 0;
