#!/usr/bin/env node
// GITHUB-006.2: read-only evidence collection. No Unity AssetDatabase writes.
import { existsSync, readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const hostMismatch = /Host type is not matching any asset type at Path\s+(Packages\/\S+)/;

export function analyzeEditorLog(content) {
  const lines = content.split(/\r?\n/);
  const paths = new Map();
  const otherImportFailures = [];
  let editorVersion = null;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    if (!editorVersion) {
      const version = line.match(/(?:Initialize engine version|Unity version|Editor version)\s*:?\s*(6000\.\d+\.\d+[a-z]\d+)/i);
      if (version) editorVersion = version[1];
    }
    const match = line.match(hostMismatch);
    if (match) {
      const path = match[1].replace(/[.,;]+$/, '');
      const entry = paths.get(path) ?? { path, occurrences: 0, firstLine: i + 1, callSites: [] };
      entry.occurrences++;
      for (let j = i + 1; j < Math.min(i + 5, lines.length); j++) {
        const call = lines[j].trim();
        // Only retain frame function names; never expose local usernames/paths.
        const frame = call.match(/^((?:UnityEditor|UnityEngine)\.[\w.+`]+:[\w.+`]+)\s*\(/);
        if (frame && !entry.callSites.includes(frame[1])) entry.callSites.push(frame[1]);
        if (lines[j].includes('Host type is not matching')) break;
      }
      paths.set(path, entry);
    } else if (/\b(?:Asset import failed|DirectoryNotFoundException|Burst could not load)\b/i.test(line)) {
      otherImportFailures.push({ kind: line.match(/Asset import failed|DirectoryNotFoundException|Burst could not load/i)[0], line: i + 1 });
    }
  }

  return {
    unityEditorVersionInLog: editorVersion ?? 'NOT FOUND',
    hostMismatchTotal: [...paths.values()].reduce((sum, p) => sum + p.occurrences, 0),
    paths: [...paths.values()].sort((a, b) => a.path.localeCompare(b.path)),
    otherImportFailureMarkers: otherImportFailures.slice(0, 30),
    importFailureMarkerTotal: otherImportFailures.length,
    note: 'Path and frame counters are evidence, not proof of root cause or build impact.'
  };
}

export function inspectManifest(root = projectRoot) {
  const manifestPath = resolve(root, 'Packages/manifest.json');
  const lockPath = resolve(root, 'Packages/packages-lock.json');
  const versionPath = resolve(root, 'ProjectSettings/ProjectVersion.txt');
  if (!existsSync(manifestPath)) throw new Error(`Missing project manifest: ${manifestPath}`);
  const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
  const dependencies = manifest.dependencies ?? {};
  const requested = {
    universal: dependencies['com.unity.render-pipelines.universal'] ?? 'NOT DIRECTLY PINNED',
    shadergraph: dependencies['com.unity.shadergraph'] ?? 'TRANSITIVE / NOT DIRECTLY PINNED',
    core: dependencies['com.unity.render-pipelines.core'] ?? 'TRANSITIVE / NOT DIRECTLY PINNED',
    urpConfig: dependencies['com.unity.render-pipelines.universal-config'] ?? 'TRANSITIVE / NOT DIRECTLY PINNED'
  };
  let locked = null;
  if (existsSync(lockPath)) {
    const lock = JSON.parse(readFileSync(lockPath, 'utf8'));
    locked = Object.fromEntries([
      'com.unity.render-pipelines.universal',
      'com.unity.render-pipelines.core',
      'com.unity.shadergraph',
      'com.unity.render-pipelines.universal-config'
    ].map(k => [k, lock.dependencies?.[k]?.version ?? 'NOT RECORDED']));
  }
  return {
    editorProjectVersion: existsSync(versionPath) ? readFileSync(versionPath, 'utf8').trim().split(/\r?\n/)[0] : 'MISSING',
    requestedPackageVersions: requested,
    localPackagesLockExists: existsSync(lockPath),
    locallyResolvedVersions: locked,
    graphicsSettingsTrackedInThisCopy: existsSync(resolve(root, 'ProjectSettings/GraphicsSettings.asset')),
    qualitySettingsTrackedInThisCopy: existsSync(resolve(root, 'ProjectSettings/QualitySettings.asset')),
    note: 'An absent packages-lock.json prevents independent verification of transitive package resolution from repository contents alone.'
  };
}

function main(args) {
  const json = args.includes('--json');
  const logIndex = args.indexOf('--editor-log');
  if (args.includes('--help') || (logIndex >= 0 && !args[logIndex + 1])) {
    process.stdout.write('Usage: node tools/diagnose-urp-import.mjs [--editor-log /path/to/Editor.log] [--json]\n');
    return;
  }
  const report = { kind: 'SWARMFORGE_URP_IMPORT_FORENSICS', writesPerformed: false, project: inspectManifest() };
  if (logIndex >= 0) {
    const path = resolve(args[logIndex + 1]);
    report.editorLog = analyzeEditorLog(readFileSync(path, 'utf8'));
  } else {
    report.editorLog = 'NOT PROVIDED — cannot establish importer root cause from static files';
  }
  if (json) process.stdout.write(`${JSON.stringify(report, null, 2)}\n`);
  else {
    process.stdout.write('SWARMFORGE GITHUB-006.2 / READ-ONLY URP DIAGNOSIS\n');
    process.stdout.write(`${JSON.stringify(report, null, 2)}\n`);
    if (logIndex < 0) process.stdout.write('For useful evidence, supply --editor-log on Owner machine.\n');
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try { main(process.argv.slice(2)); }
  catch (err) { process.stderr.write(`Diagnosis failed without project changes: ${err.message}\n`); process.exitCode = 1; }
}
