// Dumps the web's gacha for the Unity port's golden test: the heroes and banners it rolled against, then a run of
// calls — single, batched and free — with everything each paid, and the kingdom they left.
// Run from the web repo: npx vite-node <this file> <out>.
import { writeFileSync } from 'node:fs';
import { grantItem } from '../src/sim/bag';
import { claimFreePull, pull, pullMany, type PullResult } from '../src/sim/heroes';
import { BANNERS, BANNER_ORDER, HERO_LADDER, HERO_ORDER, HEROES } from '../src/sim/data/definitions';
import { getWallet } from '../src/sim/state';
import { firstGame, T0 } from '../tests/helpers';

const out: string[] = [];
for (const id of HERO_ORDER) out.push(`hero|${id}|${HEROES[id].rarity}|${HEROES[id].bagRank ?? '-'}`);
for (const id of BANNER_ORDER) out.push(`banner|${id}|${JSON.stringify(BANNERS[id])}`);
out.push(`ladder|${HERO_LADDER.bagOpen.Common},${HERO_LADDER.bagOpen.Rare},${HERO_LADDER.bagOpen.Legendary}|${HERO_LADDER.firstCallsNewHero}`);

const state = firstGame();
state.seed = 0x5eed;
out.push(`seed|${state.seed}|${T0}`);
grantItem(state, 'SilverKey', 400);
grantItem(state, 'GoldKey', 400);
out.push(`wallet0|${getWallet(state.kingdom.wallet, 'Stardust')}|${getWallet(state.kingdom.wallet, 'HeroXp')}`);

const loot = (p: PullResult): string => p.loot.map((l) =>
  l.kind === 'fragments' ? `f:${l.heroId}:${l.amount}` : l.kind === 'currency' ? `c:${l.currency}:${l.amount}` : `i:${l.item}:${l.amount}`).join(';');
const line = (tag: string, banner: string, p: PullResult): void => {
  out.push(`${tag}|${banner}|${p.result}|${p.heroId ?? '-'}|${p.rarity ?? '-'}|${p.duplicate}|${p.fragments}|${p.fragmentsOf ?? '-'}|${p.guaranteed}|${p.guaranteedLegendary}|${loot(p)}`);
};

line('pull', 'basic', pull(state, 'basic'));
line('pull', 'advanced', pull(state, 'advanced'));
for (const p of pullMany(state, 'basic', 10).pulls) line('many', 'basic', p);
// Free calls: the allowance, the cooldown, the day.
const free = (banner: string, at: number): void => {
  const r = claimFreePull(state, banner as never, at);
  if (r.result === 'Pulled') line(`free@${at - T0}`, banner, r.pull);
  else out.push(`free@${at - T0}|${banner}|${r.result}`);
};
for (const s of [0, 100, 301, 602, 903, 1204, 1505]) free('basic', T0 + s * 1000);
free('advanced', T0);
free('advanced', T0 + 1000);
free('advanced', T0 + 86_400_000);
for (let i = 0; i < 70; i += 1) line('pull', 'advanced', pull(state, 'advanced'));
for (let i = 0; i < 120; i += 1) line('pull', 'basic', pull(state, 'basic'));
for (const p of pullMany(state, 'advanced', 10).pulls) line('many', 'advanced', p);

out.push(`owned|${state.heroes.owned.join(',')}`);
out.push(`fragments|${Object.entries(state.heroes.fragments).filter(([, n]) => n > 0).sort(([a], [b]) => a.localeCompare(b)).map(([id, n]) => `${id}:${n}`).join(',')}`);
out.push(`wallet|${getWallet(state.kingdom.wallet, 'Stardust')}|${getWallet(state.kingdom.wallet, 'HeroXp')}`);
out.push(`bag|${Object.entries(state.bag.held).filter(([, n]) => n > 0).sort(([a], [b]) => a.localeCompare(b)).map(([id, n]) => `${id}:${n}`).join(',')}`);
out.push(`counters|${JSON.stringify(state.gacha.pullCounts)}|${JSON.stringify(state.gacha.pityCounters)}|${JSON.stringify(state.gacha.legendaryPity)}`);

writeFileSync(process.argv[2]!, out.join('\n') + '\n');
