// The derived definitions, as the web sim reads them: the C# GameData must build the very same.
//   cd ~/Proyectos/Codigames/kingdom && npx tsx ../kingdom-unity/Tools/Parity/export-definitions.ts
import * as D from '../../../kingdom/src/sim/data/definitions';
import { writeGolden } from './common';

const map = <K extends string, V>(keys: readonly K[], f: (k: K) => V): Record<string, V> =>
  Object.fromEntries(keys.map((k) => [k, f(k)]));

writeGolden('definitions.json', {
  techOrder: D.TECH_ORDER,
  technologies: D.TECHNOLOGIES,
  districts: D.DISTRICTS,
  buildableDistricts: D.BUILDABLE_DISTRICTS,
  workshops: D.WORKSHOPS,
  decorations: D.DECORATIONS,
  harvest: D.HARVEST,
  currencies: D.CURRENCIES,
  goods: D.GOODS,
  goodOrder: D.GOOD_ORDER,
  features: D.FEATURES,
  units: D.UNITS,
  unitOrder: D.UNIT_ORDER,
  troops: D.TROOPS,
  troopOrder: D.TROOP_ORDER,
  tomeOrder: D.TOME_ORDER,
  eraCount: D.ERA_COUNT,
  eraUnlockCells: D.ERA_UNLOCK_CELLS,
  eraRewards: D.ERA_REWARDS,
  techsInTome: map(D.TOME_ORDER, D.techsInTome),
  terrainGates: map(['Grassland', 'Plains', 'Desert', 'Snow', 'Tundra', 'Water'], D.terrainGate),
  worldUpgradeGates: map(['Fortress', 'Chapel'], D.worldUpgradeGate),
  landmarks: D.LANDMARKS,
  abandoned: D.ABANDONED,
  lairOrder: D.LAIR_ORDER,
  lairs: D.LAIRS,
  artifactOrder: D.ARTIFACT_ORDER,
  artifacts: D.ARTIFACTS,
  relicKinds: map(D.ARTIFACT_ORDER, D.relicKind),
  relicDoors: map(D.ARTIFACT_ORDER, D.relicDoor),
  heroOrder: D.HERO_ORDER,
  heroes: D.HEROES,
  villainOrder: D.VILLAIN_ORDER,
  villains: D.VILLAINS,
  bannerOrder: D.BANNER_ORDER,
  banners: D.BANNERS,
  garrisonsByTier: map(['1', '2', '3', '4', '5', '6'], (t) => D.garrisonForTier(Number(t))),
  storeOrder: D.STORE_ORDER,
  gemPackOrder: D.GEM_PACK_ORDER,
  itemBundleOrder: D.ITEM_BUNDLE_ORDER,
  offerOrder: D.OFFER_ORDER,
  dailyPool: D.DAILY_POOL,
  itemOrder: D.ITEM_ORDER,
});
