// Dumps the web resolver's and generator's output for the Unity port's golden test.
import { writeFileSync } from 'node:fs';
import { buildBoard, generateEnemy, resolveBattle, villainFighter, type BattleEvent, type FighterSpec, type SquadSpec } from '../src/sim/battle';
import { LAIRS, HEROES, COMBAT } from '../src/sim/data/definitions';
import { heroBody } from '../src/sim/heroLadder';
import { slotSkill } from '../src/sim/skills';

const out: string[] = [];
const ref = (r: { side: string; id: number }) => `${r.side}:${r.id}`;
const line = (e: BattleEvent): string => {
  switch (e.kind) {
    case 'start': return 'start|' + [...e.ours.map((s) => 'o'), ...e.theirs.map(() => 't')].length + '|'
      + ['ours', 'theirs'].map((side) => (side === 'ours' ? e.ours : e.theirs)
        .map((s) => `${s.id},${s.kind},${s.row},${s.type},${s.unitId ?? '-'},${s.count},${s.frontage},${s.atk},${s.dmg},${s.def},${s.hpUnit},${s.hpPool},${s.cooldown},${s.power},${s.x},${s.y}`).join(';')).join('/');
    case 'move': return `move|${e.tick}|${ref(e.at)}|${e.x}|${e.y}`;
    case 'attack': return `attack|${e.tick}|${ref(e.from)}|${ref(e.to)}|${e.hits}|${e.dealt}|${e.skill ?? '-'}|${e.absorbed ?? 0}|${e.edge ?? '-'}`;
    case 'skill': return `skill|${e.tick}|${ref(e.from)}|${e.skill}`;
    case 'healed': return `healed|${e.tick}|${ref(e.at)}|${e.amount}|${e.alive}|${e.hpPool}`;
    case 'shielded': return `shielded|${e.tick}|${ref(e.at)}|${e.amount}`;
    case 'dazed': return `dazed|${e.tick}|${ref(e.at)}|${e.ticks}`;
    case 'troops_lost': return `lost|${e.tick}|${ref(e.at)}|${e.alive}|${e.hpPool}`;
    case 'slot_wiped': return `wiped|${e.tick}|${ref(e.at)}`;
    case 'end': return `end|${e.tick}|${e.winner}|${e.reason}`;
  }
};
const fighter = (over: Partial<FighterSpec>): FighterSpec => ({
  id: 'probe', name: 'Probe', type: 'Warrior', atk: 0, dmg: 10, def: 0, hp: 100, cooldown: 10, power: 5,
  troopDmgMult: 1, troopHpMult: 1, troopDefBonus: 0, ...over,
});
const fight = (name: string, ours: { squads: SquadSpec[]; fighters?: FighterSpec[] }, theirs: { squads: SquadSpec[]; fighters?: FighterSpec[] }) => {
  out.push(`# fight ${name}`);
  const log = resolveBattle(buildBoard(ours.squads, ours.fighters ?? []), buildBoard(theirs.squads, theirs.fighters ?? []));
  for (const e of log.events) out.push(line(e));
};
const plan = (name: string, opts: Parameters<typeof generateEnemy>[0]) => {
  const p = generateEnemy(opts);
  out.push(`# plan ${name}`);
  out.push('squads|' + p.squads.map((s) => `${s.unitId}x${s.count}`).join(';'));
  out.push('fighters|' + p.fighters.map((f) => `${f.id},${f.hp},${f.dmg},${f.power},${f.atk},${f.def}`).join(';'));
  return p;
};

const orcs = plan('orcs-last', { seed: 42, parts: ['HollowBarrow', 'gate'], budget: 60, affinity: 'Warrior', mix: LAIRS.Orcs.guard.mix });
plan('orcs-0', { seed: 42, parts: ['HollowBarrow', 'gate', 0], budget: 30, affinity: 'Warrior', mix: LAIRS.Orcs.guard.mix });
plan('harpies', { seed: 7, parts: ['SunkenChapel', 'gate'], budget: 300, affinity: 'Archer', mix: LAIRS.Harpies.guard.mix });
plan('goblins', { seed: 7, parts: ['DrownedIronworks', 'gate'], budget: 440, affinity: 'Lancer' });
plan('drake', { seed: 99, parts: ['StarObservatory', 'gate'], budget: 1000, affinity: 'Any' });
plan('evolved', { seed: 3, parts: ['deep', 3, 1], budget: 9000, affinity: 'Cavalry' });
plan('huge', { seed: 3, parts: ['deep', 9], budget: 40000, affinity: 'Archer' });
plan('tiny', { seed: 5, parts: ['x'], budget: 1, affinity: 'Lancer' });
const pool = ['BarrowThane', 'DrownedChoir'] as const;
for (const v of pool) {
  const f = villainFighter(v);
  out.push(`villain|${f.id}|${f.type}|${f.atk}|${f.dmg}|${f.def}|${f.hp}|${f.cooldown}|${f.power}|${f.troopDmgMult}|${f.troopHpMult}|${f.troopDefBonus}|${f.skill?.id ?? '-'}|${f.skill?.amount ?? 0}|${f.skill?.every ?? 0}`);
}
plan('villains', { seed: 11, parts: ['portal', 20], budget: 3000, affinity: 'Warrior', villainPool: [...pool] });
plan('scaled', { seed: 11, parts: ['portal', 40], budget: 30000, affinity: 'Warrior', villainPool: [...pool] });
plan('boss', { seed: 11, parts: ['room', 6], budget: 2000, affinity: 'Archer', boss: 'DrownedChoir' });

fight('chain-vs-orcs', { squads: [{ unitId: 'Warrior', count: 30 }] }, { squads: orcs.squads });
fight('mixed', { squads: [{ unitId: 'Warrior_e2', count: 50 }, { unitId: 'Archer', count: 40 }, { unitId: 'Cavalry', count: 20 }, { unitId: 'Lancer', count: 30 }] },
  plan('mixed-enemy', { seed: 7, parts: ['DrownedIronworks', 'gate'], budget: 440, affinity: 'Lancer' }));
fight('skills', {
  squads: [{ unitId: 'Warrior', count: 60 }, { unitId: 'Archer', count: 50 }],
  fighters: [
    fighter({ id: 'medic', type: 'Archer', hp: 300, dmg: 12, troopDmgMult: 1.2, skill: { id: 'Mend', amount: 150, every: 30 } }),
    fighter({ id: 'wall', type: 'Warrior', hp: 500, troopHpMult: 1.3, troopDefBonus: 2, skill: { id: 'Shield', amount: 400, every: 50 } }),
    fighter({ id: 'cry', type: 'Cavalry', hp: 200, skill: { id: 'WarCry', amount: 100, every: 0 } }),
  ],
}, {
  squads: [{ unitId: 'Lancer', count: 70 }, { unitId: 'Cavalry', count: 30 }],
  fighters: [
    fighter({ id: 'volley', type: 'Archer', hp: 400, dmg: 20, skill: { id: 'Volley', amount: 500, every: 45 } }),
    fighter({ id: 'daze', type: 'Lancer', hp: 400, skill: { id: 'Daze', amount: 20, every: 35 } }),
    fighter({ id: 'wave', type: 'Warrior', hp: 400, troopDefBonus: 3, skill: { id: 'Wave', amount: 100, every: 40 } }),
  ],
});
fight('strikes', {
  squads: [{ unitId: 'Lancer_e3', count: 40 }],
  fighters: [fighter({ id: 'a', type: 'Cavalry', hp: 400, skill: { id: 'Ambush', amount: 900, every: 25 } }),
    fighter({ id: 'c', type: 'Warrior', hp: 400, hpNow: 250, skill: { id: 'Crush', amount: 700, every: 30 } }),
    fighter({ id: 's', type: 'Archer', hp: 400, skill: { id: 'Sharpshot', amount: 1200, every: 20 } })],
}, { squads: [{ unitId: 'Archer_e2', count: 60 }, { unitId: 'Warrior', count: 80 }], fighters: [fighter({ id: 'b', type: 'Archer', skill: { id: 'Bulwark', amount: 4, every: 0 } }), fighter({ id: 'v', skill: { id: 'Vigour', amount: 200, every: 0 } })] });
fight('timeout', { squads: [], fighters: [fighter({ id: 'x', hp: 100000, dmg: 1, def: 200, cooldown: 50 })] },
  { squads: [], fighters: [fighter({ id: 'y', type: 'Lancer', hp: 100000, dmg: 1, def: 200, cooldown: 60 })] });
fight('empty', { squads: [] }, { squads: [{ unitId: 'Warrior', count: 5 }] });

// Heroes at a level and an ascension, one wounded, with their skills at a rank — as partyBoard builds them.
const heroSpec = (id: keyof typeof HEROES, level: number, ascension: number, rank: number, hpNow?: number): FighterSpec => {
  const def = HEROES[id];
  const body = heroBody(def, level, ascension);
  out.push(`hero|${id}|${level}|${ascension}|${rank}|${hpNow ?? '-'}`);
  return {
    id, name: def.name, type: def.unitType, atk: body.atk, dmg: body.dmg, def: body.def, hp: body.hp,
    ...(hpNow === undefined ? {} : { hpNow }), cooldown: def.cooldown, power: Math.round(body.dmg * COMBAT.heroPowerPerDmg),
    troopDmgMult: def.troopDmgMult, troopHpMult: def.troopHpMult, troopDefBonus: def.troopDefBonus, skill: slotSkill(def.skill, rank),
  };
};
out.push('# heroes');
const party = [heroSpec('Warden', 37, 7, 2, 900), heroSpec('Rogue', 12, 1, 1), heroSpec('Bard', 71, 12, 3)];
const foes = [heroSpec('Cleric', 25, 4, 1), heroSpec('Joker', 50, 9, 2)];
fight('heroes', { squads: [{ unitId: 'Warrior', count: 80 }, { unitId: 'Archer', count: 60 }], fighters: party },
  { squads: [{ unitId: 'Lancer_e2', count: 70 }, { unitId: 'Cavalry', count: 40 }], fighters: foes });

writeFileSync(process.argv[2], out.join('\n') + '\n');
console.log('lines', out.length);
