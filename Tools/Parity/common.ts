// Shared by the parity exporters: where the goldens go, and what is presentation.
import { execSync } from 'node:child_process';
import { mkdirSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
export const GOLDEN = join(here, '../../Assets/Kingdom/Tests/Golden');
export const WEB = join(here, '../../../kingdom');

// Names, prose and art live in the Unity project's catalogs, not in the sim: a golden leaves them out.
const PRESENTATION = new Set([
  'name', 'title', 'description', 'promise', 'glyph', 'exhaustedGlyph', 'sprite', 'art', 'text',
  'passiveText', 'flavour', 'blurb', 'icon', 'crew', 'pending',
]);

export function strip(value: unknown): unknown {
  if (Array.isArray(value)) return value.map(strip);
  if (value !== null && typeof value === 'object') {
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(value)) {
      if (PRESENTATION.has(k) || v === undefined) continue;
      out[k] = strip(v);
    }
    return out;
  }
  if (typeof value === 'number' && !Number.isFinite(value)) return value > 0 ? 'Infinity' : '-Infinity';
  return value;
}

export function writeGolden(file: string, body: unknown): void {
  const commit = execSync('git rev-parse --short HEAD', { cwd: WEB }).toString().trim();
  const path = join(GOLDEN, file);
  mkdirSync(dirname(path), { recursive: true });
  writeFileSync(path, JSON.stringify({ webCommit: commit, body: strip(body) }, null, 1) + '\n');
  console.log(`wrote ${file} (web ${commit})`);
}
