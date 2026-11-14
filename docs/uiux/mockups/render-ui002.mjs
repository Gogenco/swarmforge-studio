#!/usr/bin/env node
// UI-002 visual acceptance mockups only. No Unity runtime code or simulation data.
// Run: node docs/uiux/mockups/render-ui002.mjs
import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const outputDir = dirname(fileURLToPath(import.meta.url));

const C = {
  bg: '#071320', floor: '#14283D', lane: '#203D55', laneEdge: '#426280',
  surface: '#101E34', cell: '#1C3450', text: '#F9FBFF', secondary: '#D5E3F5',
  x2: '#076079', x1: '#3D465A', x3: '#865400', action: '#F7BE35',
  bot: '#41E7B0', enemy: '#F46679', fortress: '#A2AFC5',
};

const esc = v => String(v).replace(/&/g, '&amp;').replace(/</g, '&lt;')
  .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
const f = n => Number(n.toFixed(2));
const rect = (x, y, w, h, fill, r = 0, extra = '') =>
  `<rect x="${f(x)}" y="${f(y)}" width="${f(w)}" height="${f(h)}" rx="${f(r)}" fill="${fill}" ${extra}/>`;
const tx = (x, y, label, size, fill, weight = 500, anchor = 'middle', other = '') =>
  `<text x="${f(x)}" y="${f(y)}" font-family="Arial, Helvetica, sans-serif" font-size="${f(size)}" font-weight="${weight}" fill="${fill}" text-anchor="${anchor}" ${other}>${esc(label)}</text>`;
const line = (x1, y1, x2, y2, color, opacity = 1, stroke = 1) =>
  `<line x1="${f(x1)}" y1="${f(y1)}" x2="${f(x2)}" y2="${f(y2)}" stroke="${color}" stroke-width="${stroke}" opacity="${opacity}"/>`;

function wrap(W, H, markup, title) {
  return `<?xml version="1.0" encoding="UTF-8"?>\n` +
    `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}" role="img" aria-label="${esc(title)}">\n` +
    `<title>${esc(title)}</title><defs>` +
    `<linearGradient id="background" x1="0" y1="0" x2="0" y2="1"><stop stop-color="#091522"/><stop offset="1" stop-color="#050B13"/></linearGradient>` +
    `<linearGradient id="actionfill" x1="0" y1="0" x2="1" y2="1"><stop stop-color="#FFD876"/><stop offset="1" stop-color="${C.action}"/></linearGradient>` +
    `</defs>\n${markup}\n</svg>\n`;
}

function geometry(W, H, cutout) {
  const safe = cutout ? { left: 0, top: W > 400 ? 56 : 54, right: 0, bottom: W > 400 ? 34 : 26 } :
    { left: 0, top: 0, right: 0, bottom: 0 };
  const sw = W - safe.left - safe.right;
  const s = Math.min(1.1, sw / 390);
  const panel = { x: safe.left + 16 * s, y: safe.top + 12 * s,
    w: sw - 32 * s, h: 150 * s };
  const hint = { x: panel.x, w: panel.w, h: 76 * s,
    y: H - safe.bottom - 12 * s - 76 * s };
  return { W, H, safe, sw, s, panel, hint };
}

function world(g) {
  const { W, H, s, panel, hint } = g;
  const y1 = panel.y + panel.h + 15 * s;
  const y2 = hint.y - 22 * s;
  const h = Math.max(120, y2 - y1);
  const centers = [W * .22, W * .5, W * .78];
  let a = rect(0, 0, W, H, 'url(#background)');
  a += rect(10 * s, y1, W - 20 * s, h, C.floor, 20 * s, 'opacity="0.88"');
  for (let i = 0; i < centers.length; i++) {
    a += rect(centers[i] - W * .127, y1 + 8 * s, W * .254, h - 16 * s,
      i === 1 ? '#1B2A3D' : C.lane, 12 * s, 'opacity="0.80"');
    a += line(centers[i], y1 + 16 * s, centers[i], y2 - 16 * s,
      C.laneEdge, .3, 1.5);
  }
  const gateY = y1 + h * .37;
  const labels = ['×2', '×1', '×3'];
  const bg = [C.x2, C.x1, C.x3];
  for (let i = 0; i < 3; i++) {
    const stripW = W * .245;
    a += rect(centers[i] - stripW / 2, gateY + 17 * s,
      stripW, 10 * s, bg[i], 4 * s, 'opacity="0.94"');
    a += rect(centers[i] - 30 * s, gateY - 30 * s,
      60 * s, 40 * s, bg[i], 13 * s, 'stroke="#D5E3F5" stroke-width="1.2"');
    a += tx(centers[i], gateY - 3 * s, labels[i], 22 * s, '#FFFFFF', 800);
  }
  const fortressY = y1 + 31 * s;
  a += rect(W / 2 - 50 * s, fortressY - 19 * s,
    100 * s, 28 * s, C.fortress, 6 * s, 'opacity="0.72"');
  a += rect(W / 2 - 31 * s, fortressY - 32 * s,
    62 * s, 20 * s, '#D4DBE8', 6 * s, 'opacity="0.65"');
  const botY = y1 + h * .68;
  for (let i = 0; i < 3; i++) {
    const botX = centers[i] + (i % 2 ? -7 : 6) * s;
    a += `<circle cx="${f(botX)}" cy="${f(botY + i * 8 * s)}" r="${f(7 * s)}" fill="${C.bot}" stroke="#D8FFF2" stroke-width="1"/>`;
    a += `<circle cx="${f(centers[i] - 11 * s)}" cy="${f(y1 + h * .23 + i * 10 * s)}" r="${f(6 * s)}" fill="${C.enemy}" stroke="#FFC3CC" stroke-width="1"/>`;
  }
  a += rect(W / 2 - 36 * s, y2 - 6 * s, 72 * s, 20 * s, '#4F879D', 5 * s);
  a += rect(W / 2 - 15 * s, y2 - 28 * s, 30 * s, 31 * s, C.bot, 6 * s);
  return a;
}

function hud(g) {
  const { s, panel: p } = g;
  let a = rect(p.x, p.y, p.w, p.h, C.surface, 17 * s,
    'stroke="#6389AE" stroke-width="1.3"');
  a += tx(p.x + 14 * s, p.y + 23 * s, 'SWARMFORGE', 19 * s, C.text, 800, 'start');
  a += rect(p.x + p.w - 72 * s, p.y + 7 * s, 62 * s, 23 * s,
    '#2A4765', 11 * s);
  a += tx(p.x + p.w - 41 * s, p.y + 24 * s, 'VS-01', 15 * s, C.secondary, 700);
  const statGrid = [
    [['BASE', '3'], ['FORT', '12'], ['TIME', '30s']],
    [['SHOTS', '40'], ['BOTS', '57'], ['KILLS', '8']],
  ];
  const inset = 9 * s, gap = 6 * s;
  const cellWidth = (p.w - 2 * inset - 2 * gap) / 3;
  for (let row = 0; row < 2; row++) {
    const cy = p.y + (row === 0 ? 33 : 90) * s;
    for (let col = 0; col < 3; col++) {
      const cx = p.x + inset + col * (cellWidth + gap);
      const center = cx + cellWidth / 2;
      a += rect(cx, cy, cellWidth, 50 * s, C.cell, 9 * s);
      a += tx(center, cy + 18 * s, statGrid[row][col][0], 18 * s,
        C.secondary, 650);
      a += tx(center, cy + 41 * s, statGrid[row][col][1], 22 * s,
        C.text, 800);
    }
  }
  return a;
}

function hint(g) {
  const { s, hint: p } = g;
  let a = rect(p.x, p.y, p.w, p.h, C.surface, 17 * s,
    'stroke="#6389AE" stroke-width="1.3"');
  a += tx(p.x + p.w / 2, p.y + 31 * s, '⇆  DRAG TO AIM  ⇆', 21 * s, C.text, 800);
  a += tx(p.x + p.w / 2, p.y + 58 * s, 'AUTO-FIRE  ·  ATTACK', 18 * s,
    C.secondary, 650);
  return a;
}

function notch(g) {
  const { W, H, safe } = g;
  if (!safe.top) return '';
  return rect(W / 2 - 74, 0, 148, 30, '#020508', 16) +
    rect(0, H - 11, W, 11, '#020508', 0, 'opacity="0.45"');
}

function game(W, H, cutout = false) {
  const g = geometry(W, H, cutout);
  const a = world(g) + hud(g) + hint(g) + notch(g);
  return wrap(W, H, a, `UI-002 VS-01 gameplay ${W}×${H}${cutout ? ' safe-area notch' : ''}`);
}

function terminal(kind) {
  const W = 390, H = 844;
  const g = geometry(W, H, true);
  const { s, safe, sw } = g;
  const defs = {
    VICTORY: { title: 'VICTORY!', action: 'RESTART', subtitle: 'FORTRESS CLEARED' },
    BASE_DESTROYED: { title: 'BASE DESTROYED', action: 'RESTART', subtitle: 'DEFENSE FAILED' },
    TIME_UP: { title: 'TIME UP', action: 'RESTART', subtitle: 'MATCH COMPLETE' },
    PAUSED: { title: 'PAUSED', action: 'RESUME', subtitle: 'READY WHEN YOU ARE' },
  };
  const d = defs[kind];
  let a = world(g) + rect(0, 0, W, H, '#050B17', 0, 'opacity="0.91"');
  const width = Math.min(350 * s, sw - 48 * s);
  const height = 325 * s;
  const x = safe.left + (sw - width) / 2;
  const safeH = H - safe.top - safe.bottom;
  const y = safe.top + (safeH - height) / 2;
  a += rect(x, y, width, height, C.surface, 22 * s,
    'stroke="#799AB7" stroke-width="1.8"');
  a += rect(x + 28 * s, y + 14 * s, width - 56 * s, 4 * s,
    kind === 'VICTORY' ? C.bot : kind === 'PAUSED' ? '#67BCE8' : C.enemy, 2 * s);
  a += tx(x + width / 2, y + 61 * s, d.title,
    kind === 'BASE_DESTROYED' ? 26 * s : 28 * s, C.text, 800);
  a += tx(x + width / 2, y + 90 * s, d.subtitle, 16 * s, C.secondary, 650);
  const cells = [
    ['BASE', kind === 'BASE_DESTROYED' ? '0' : '3'],
    ['FORT', kind === 'VICTORY' ? '0' : '12'],
    ['SHOTS', '40'], ['KILLS', '8'],
  ];
  const cellWidth = (width - 54 * s) / 2, cellH = 54 * s;
  for (let i = 0; i < 4; i++) {
    const row = Math.floor(i / 2), col = i % 2;
    const cx = x + 22 * s + col * (cellWidth + 10 * s);
    const cy = y + (112 + 62 * row) * s;
    a += rect(cx, cy, cellWidth, cellH, C.cell, 10 * s);
    a += tx(cx + cellWidth / 2, cy + 21 * s, cells[i][0], 18 * s,
      C.secondary, 650);
    a += tx(cx + cellWidth / 2, cy + 44 * s, cells[i][1], 22 * s, C.text, 800);
  }
  const bx = x + 28 * s, by = y + 249 * s;
  a += rect(bx, by, width - 56 * s, 64 * s, 'url(#actionfill)', 14 * s);
  a += tx(x + width / 2, by + 42 * s, d.action, 22 * s, C.surface, 800);
  a += notch(g);
  return wrap(W, H, a, `UI-002 VS-01 ${d.title} terminal overlay ${W}×${H}`);
}

await mkdir(outputDir, { recursive: true });
const files = [
  ['UI002_GAME_320x568.svg', game(320, 568)],
  ['UI002_GAME_360x640.svg', game(360, 640)],
  ['UI002_GAME_390x844_NOTCH.svg', game(390, 844, true)],
  ['UI002_GAME_412x915_NOTCH.svg', game(412, 915, true)],
  ['UI002_TERMINAL_VICTORY.svg', terminal('VICTORY')],
  ['UI002_TERMINAL_BASE_DESTROYED.svg', terminal('BASE_DESTROYED')],
  ['UI002_TERMINAL_TIME_UP.svg', terminal('TIME_UP')],
  ['UI002_TERMINAL_PAUSED.svg', terminal('PAUSED')],
];
for (const [name, content] of files) {
  await writeFile(join(outputDir, name), content, 'utf8');
  process.stdout.write(`${name}\n`);
}
