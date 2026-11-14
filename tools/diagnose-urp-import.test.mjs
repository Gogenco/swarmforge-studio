import assert from 'node:assert/strict';
import { test } from 'node:test';
import { analyzeEditorLog, inspectManifest } from './diagnose-urp-import.mjs';

test('groups repeated host mismatches by actual package asset path', () => {
  const log = [
    'Initialize engine version: 6000.3.28f1',
    'Host type is not matching any asset type at Path Packages/com.unity.render-pipelines.universal/Shaders/AutodeskInteractive/AutodeskInteractiveTransparent.shadergraph.',
    'UnityEditor.AssetPostprocessingInternal:PostprocessAllAssets (string,string)',
    'Host type is not matching any asset type at Path Packages/com.unity.render-pipelines.universal/Shaders/AutodeskInteractive/AutodeskInteractiveTransparent.shadergraph.',
    'Host type is not matching any asset type at Path Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/RenderingLayerMask/TraceRenderingLayerMask.urtshader.',
    'UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)',
  ].join('\n');
  const r = analyzeEditorLog(log);
  assert.equal(r.unityEditorVersionInLog, '6000.3.28f1');
  assert.equal(r.hostMismatchTotal, 3);
  assert.equal(r.paths.length, 2);
  assert.deepEqual(r.paths.map(x => x.occurrences).sort(), [1, 2]);
  assert.ok(r.paths.some(x => x.path.endsWith('.shadergraph')));
  assert.ok(r.paths.some(x => x.path.endsWith('.urtshader')));
});

test('does not mistake unrelated import failures for package host mismatch', () => {
  const r = analyzeEditorLog('DirectoryNotFoundException: a path\nBurst could not load a cache\nAsset import failed');
  assert.equal(r.hostMismatchTotal, 0);
  assert.equal(r.importFailureMarkerTotal, 3);
  assert.equal(r.unityEditorVersionInLog, 'NOT FOUND');
});

test('inspects source manifest without inventing an absent lockfile', () => {
  const project = inspectManifest();
  assert.equal(project.requestedPackageVersions.universal, '17.3.0');
  assert.equal(project.localPackagesLockExists, false);
  assert.equal(project.locallyResolvedVersions, null);
});
