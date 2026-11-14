import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';

// Stable Unity GUID metadata for assets without an existing .meta.
const project = path.resolve(import.meta.dirname, '..');
const root = path.join(project, 'Assets');
let count = 0;
function visit(dir) {
  for (const item of fs.readdirSync(dir, { withFileTypes: true })) {
    if (item.name.endsWith('.meta')) continue;
    const full = path.join(dir, item.name);
    const relative = path.relative(project, full).replaceAll('\\', '/');
    const meta = `${full}.meta`;
    if (!fs.existsSync(meta)) {
      const guid = crypto.createHash('sha256').update(`SWARMFORGE/${relative}`).digest('hex').slice(0, 32);
      const type = item.isDirectory() ? 'DefaultImporter' : item.name.endsWith('.cs') ?
        'MonoImporter' : item.name.endsWith('.asmdef') ? 'AssemblyDefinitionImporter' :
          item.name.endsWith('.asset') ? 'NativeFormatImporter' : 'DefaultImporter';
      const folder = item.isDirectory() ? 'folderAsset: yes\n' : '';
      const importer = type === 'MonoImporter' ?
        '  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n' :
        type === 'NativeFormatImporter' ? '  mainObjectFileID: 11400000\n' : '';
      fs.writeFileSync(meta, `fileFormatVersion: 2\nguid: ${guid}\n${folder}${type}:\n  externalObjects: {}\n${importer}  userData:\n  assetBundleName:\n  assetBundleVariant:\n`);
      count++;
    }
    if (item.isDirectory()) visit(full);
  }
}
visit(root);
console.log(`Added ${count} stable Unity .meta files; existing GUIDs preserved.`);
