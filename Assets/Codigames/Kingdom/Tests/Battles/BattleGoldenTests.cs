using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Codigames.Kingdom.Army;
using Codigames.Kingdom.Battles;
using Codigames.Modules.Core;
using NUnit.Framework;

namespace Codigames.Kingdom.Tests.Battles
{
    // The resolver and the generator against the web prototype's own output (Tools/WebData/battle-golden.ts wrote
    // battle-golden.txt): the same boards and rooms, rebuilt here, must give the same stream event for event and the
    // same squads squad for squad. A change to §7, §8, §10 or §11 rewrites the file — on the web first.
    public class BattleGoldenTests
    {
        public sealed class FakeUnit : IUnitDefinition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description => "";
            public IReadOnlyList<string> Tags { get; set; }
            public int SquadSize { get; set; }
            public int Frontage { get; set; }
            public int Cooldown { get; set; }
            public int Speed { get; set; }
            public int Range { get; set; }
            public IReadOnlyList<UnitRank> Ranks { get; set; }
        }

        public sealed class Settings : ICombatSettings
        {
            public int TickMs => 100;
            public int AttackStepPerMille => 50;
            public int AttackCapPerMille => 1500;
            public int DefenceStepPerMille => 25;
            public int DefenceCapPerMille => 750;
            public int TimeoutTicks => 1800;
            public int FieldGap => 360;
            public int FieldRowPitch => 100;
            public int FieldColPitch => 100;
            public int TypeAdvantageNum => 3;
            public int TypeAdvantageDen => 2;
            public int TypeDisadvantageNum => 3;
            public int TypeDisadvantageDen => 4;
            public int GenSlotsMin => 2;
            public int GenSlotsMax => 6;
            public int GenVillainThreshold => 400;
            public double GenVillainShare => 0.3;
            public int GenVillainSlots => 3;
            public double HeroPowerPerDmg => 1.67;
            public double FightMana => 20;
        }

        private static UnitRank R(int atk, int dmg, int def, int hp, int power) => new(atk, dmg, def, hp, power, new Dictionary<string, double>(), 0, 1);

        // The units at every rank, as combat.md §5–§6.3 has them.
        public static Catalog<IUnitDefinition> Units() => new(new IUnitDefinition[]
        {
            new FakeUnit
            {
                Id = "Warrior", Name = "Warrior", Tags = new[] { "Melee" }, SquadSize = 100, Frontage = 15, Cooldown = 10, Speed = 10, Range = 90,
                Ranks = new[] { R(4, 5, 6, 60, 3), R(6, 8, 8, 96, 5), R(8, 13, 10, 156, 8), R(10, 21, 12, 252, 13), R(12, 34, 14, 408, 20) },
            },
            new FakeUnit
            {
                Id = "Lancer", Name = "Lancer", Tags = new[] { "Melee" }, SquadSize = 100, Frontage = 15, Cooldown = 10, Speed = 10, Range = 120,
                Ranks = new[] { R(6, 8, 4, 48, 4), R(8, 13, 6, 77, 6), R(10, 21, 8, 125, 10), R(12, 34, 10, 202, 17), R(14, 54, 12, 326, 27) },
            },
            new FakeUnit
            {
                Id = "Archer", Name = "Archer", Tags = new[] { "Distance" }, SquadSize = 80, Frontage = 20, Cooldown = 12, Speed = 8, Range = 1000,
                Ranks = new[] { R(6, 6, 2, 30, 4), R(8, 10, 4, 48, 6), R(10, 16, 6, 78, 10), R(12, 25, 8, 126, 17), R(14, 41, 10, 204, 27) },
            },
            new FakeUnit
            {
                Id = "Cavalry", Name = "Cavalry", Tags = new[] { "Mounted", "Melee" }, SquadSize = 60, Frontage = 8, Cooldown = 15, Speed = 20, Range = 90,
                Ranks = new[] { R(8, 20, 3, 72, 7), R(10, 32, 5, 115, 11), R(12, 52, 7, 187, 18), R(14, 84, 9, 302, 29), R(16, 136, 11, 490, 48) },
            },
        });

        public sealed class FakeHero : Codigames.Kingdom.Heroes.IHeroDefinition
        {
            public string Id { get; set; }
            public string Name => Id;
            public string Title => "";
            public Codigames.Kingdom.Heroes.HeroRarity Rarity { get; set; }
            public int? BagRank { get; set; }
            public string UnitType { get; set; }
            public string Skill { get; set; }
            public double SkillValue { get; set; }
            public double SkillEvery { get; set; }
            public double Atk { get; set; }
            public double Dmg { get; set; }
            public double Def { get; set; }
            public double Hp { get; set; }
            public int Cooldown { get; set; }
            public double AtkPerLevel { get; set; }
            public double DmgPerLevel { get; set; }
            public double DefPerLevel { get; set; }
            public double HpPerLevel { get; set; }
            public double TroopDmgMult { get; set; }
            public double TroopHpMult { get; set; }
            public int TroopDefBonus { get; set; }
            public string BoonStat { get; set; }
            public double BoonValue { get; set; }
        }

        // The web's heroLadder.json.
        public sealed class Ladder : Codigames.Kingdom.Heroes.IHeroLadderSettings
        {
            public int AscensionStars => 5;
            public int AscensionStepsPerStar => 6;
            public int RecruitFragments(Codigames.Kingdom.Heroes.HeroRarity rarity) => rarity switch
            {
                Codigames.Kingdom.Heroes.HeroRarity.Common => 15, Codigames.Kingdom.Heroes.HeroRarity.Rare => 25, _ => 40,
            };
            public double FragmentsPerStepBase => 1;
            public double FragmentsPerStepGrowth => 2;
            public double AscensionStardustBase => 4;
            public double AscensionStardustGrowth => 2;
            public double XpLevelCostBase => 20;
            public double XpLevelCostGrowth => 1.0165;
            public int HeroLevelsPerStar => 0;
            public int HeroLevelsPerAscension => 10;
            public double StatsPerAscension => 0.02;
            public int HeroMaxLevel => 310;
            public IReadOnlyList<int> SkillRankLevels { get; } = new[] { 71, 131, 191, 251 };
            public IReadOnlyList<double> SkillRankStardust { get; } = new double[] { 100, 200, 400, 800 };
            public IReadOnlyList<double> SkillRankMaterial { get; } = new double[] { 2, 4, 8, 12 };
            public double SkillRankStep => 0.25;
            public int HeroSlots => 3;
            public double HeroSlotGemCostBase => 2500;
            public double HeroSlotGemCostGrowth => 2;
            public double HeroRecoverHours => 8;
            public int BagOpen(Codigames.Kingdom.Heroes.HeroRarity rarity) => rarity switch
            {
                Codigames.Kingdom.Heroes.HeroRarity.Common => 3, Codigames.Kingdom.Heroes.HeroRarity.Rare => 2, _ => 1,
            };
            public int FirstCallsNewHero => 2;
        }

        private static FakeHero H(string id, Codigames.Kingdom.Heroes.HeroRarity rarity, string type, string skill, double value, double every,
            double atk, double dmg, double def, double hp, int cooldown, double atkL, double dmgL, double defL, double hpL, double dm, double hm, int db)
            => new()
            {
                Id = id, Rarity = rarity, UnitType = type, Skill = skill, SkillValue = value, SkillEvery = every, Atk = atk, Dmg = dmg, Def = def, Hp = hp,
                Cooldown = cooldown, AtkPerLevel = atkL, DmgPerLevel = dmgL, DefPerLevel = defL, HpPerLevel = hpL, TroopDmgMult = dm, TroopHpMult = hm,
                TroopDefBonus = db,
            };

        // Five of the web's heroes.json, as they stand.
        public static Catalog<Codigames.Kingdom.Heroes.IHeroDefinition> Heroes() => new(new Codigames.Kingdom.Heroes.IHeroDefinition[]
        {
            H("Warden", Codigames.Kingdom.Heroes.HeroRarity.Common, "Warrior", "Shield", 15, 4.5, 3, 24, 3, 1152, 12, 0.04, 0.95, 0.04, 45.67, 1.1, 1.05, 1),
            H("Rogue", Codigames.Kingdom.Heroes.HeroRarity.Common, "Cavalry", "Sharpshot", 80, 3.5, 2, 42, 2, 816, 12, 0.019, 1.9, 0.019, 30.45, 1.1, 1.05, 1),
            H("Bard", Codigames.Kingdom.Heroes.HeroRarity.Common, "Archer", "WarCry", 5, 0, 1, 42, 1, 624, 12, 0.016, 1.43, 0.016, 22.83, 1.1, 1.05, 1),
            H("Cleric", Codigames.Kingdom.Heroes.HeroRarity.Common, "Warrior", "Mend", 10, 3.5, 3, 24, 3, 1152, 12, 0.04, 0.95, 0.04, 45.67, 1.1, 1.05, 1),
            H("Joker", Codigames.Kingdom.Heroes.HeroRarity.Common, "Archer", "Daze", 1, 4.5, 1, 42, 1, 624, 12, 0.016, 1.43, 0.016, 22.83, 1.1, 1.05, 1),
        });

        [Test]
        public void HeroesOnTheBoard_ShouldFightAsTheWebsDo()
        {
            var heroes = Heroes();
            var ladder = new Codigames.Kingdom.Heroes.HeroLadder(new Ladder());
            var specs = File.ReadAllLines(Golden()).Where(l => l.StartsWith("hero|")).Select(l =>
            {
                var p = l.Split('|');
                var hero = heroes.Get(p[1]);
                var body = ladder.Body(hero, int.Parse(p[2]), int.Parse(p[3]));
                return new FighterSpec
                {
                    Id = hero.Id, Name = hero.Name, Type = hero.UnitType, Atk = body.Atk, Dmg = body.Dmg, Def = body.Def, Hp = body.Hp,
                    HpNow = p[5] == "-" ? null : double.Parse(p[5], CultureInfo.InvariantCulture), Cooldown = hero.Cooldown,
                    Power = Combat.JsRound(body.Dmg * 1.67), TroopDmgMult = hero.TroopDmgMult, TroopHpMult = hero.TroopHpMult,
                    TroopDefBonus = hero.TroopDefBonus, Skill = ladder.SlotSkill(hero, int.Parse(p[4]), 100),
                };
            }).ToList();

            Fight("heroes", new[] { Q("Warrior", 80), Q("Archer", 60) }, specs.Take(3).ToList(), new[] { Q("Lancer_e2", 70), Q("Cavalry", 40) },
                specs.Skip(3).ToList());
        }

        private static FighterSpec F(string id = "probe", string type = "Warrior", int hp = 100, int dmg = 10, int def = 0, int cooldown = 10,
            double dmgMult = 1, double hpMult = 1, int defBonus = 0, SlotSkill skill = null, int? hpNow = null)
            => new()
            {
                Id = id, Name = "Probe", Type = type, Atk = 0, Dmg = dmg, Def = def, Hp = hp, HpNow = hpNow, Cooldown = cooldown, Power = 5,
                TroopDmgMult = dmgMult, TroopHpMult = hpMult, TroopDefBonus = defBonus, Skill = skill,
            };

        private static SquadSpec Q(string troop, int count) => new(troop, count);

        private Combat _combat;
        private EnemyGenerator _generator;
        private Dictionary<string, List<string>> _golden;

        [SetUp]
        public void SetUp()
        {
            _combat = new Combat(Units(), new Settings());
            _generator = new EnemyGenerator(_combat);
            _golden = new Dictionary<string, List<string>>();
            List<string> current = null;
            foreach (var line in File.ReadAllLines(Golden()))
            {
                if (line.StartsWith("hero|")) continue;
                if (line.StartsWith("# "))
                {
                    current = new List<string>();
                    _golden[line.Substring(2)] = current;
                }
                else if (current != null) current.Add(line);
                else _golden.TryAdd("header", new List<string>());
            }
        }

        private static readonly IReadOnlyDictionary<string, double> ORC_MIX = new Dictionary<string, double> { ["Warrior"] = 4, ["Lancer"] = 1 };
        private static readonly IReadOnlyDictionary<string, double> HARPY_MIX = new Dictionary<string, double> { ["Archer"] = 7, ["Cavalry"] = 3 };

        private IReadOnlyList<FighterSpec> Pool() => File.ReadAllLines(Golden()).Where(l => l.StartsWith("villain|")).Select(l =>
        {
            var p = l.Split('|');
            double D(int i) => double.Parse(p[i], CultureInfo.InvariantCulture);
            return new FighterSpec
            {
                Id = p[1], Name = p[1], Type = p[2], Atk = D(3), Dmg = D(4), Def = D(5), Hp = D(6), Cooldown = (int)D(7), Power = (int)D(8),
                TroopDmgMult = D(9), TroopHpMult = D(10), TroopDefBonus = (int)D(11), Skill = new SlotSkill(p[12], (int)D(13), (int)D(14)),
            };
        }).ToList();

        [Test]
        public void TheGenerator_ShouldFieldWhatTheWebFields()
        {
            Plan("orcs-last", 42, new object[] { "HollowBarrow", "gate" }, 60, "Warrior", ORC_MIX);
            Plan("orcs-0", 42, new object[] { "HollowBarrow", "gate", 0 }, 30, "Warrior", ORC_MIX);
            Plan("harpies", 7, new object[] { "SunkenChapel", "gate" }, 300, "Archer", HARPY_MIX);
            Plan("goblins", 7, new object[] { "DrownedIronworks", "gate" }, 440, "Lancer");
            Plan("drake", 99, new object[] { "StarObservatory", "gate" }, 1000, "Any");
            Plan("evolved", 3, new object[] { "deep", 3, 1 }, 9000, "Cavalry");
            Plan("huge", 3, new object[] { "deep", 9 }, 40000, "Archer");
            Plan("tiny", 5, new object[] { "x" }, 1, "Lancer");
            Plan("villains", 11, new object[] { "portal", 20 }, 3000, "Warrior", pool: Pool());
            Plan("scaled", 11, new object[] { "portal", 40 }, 30000, "Warrior", pool: Pool());
            Plan("boss", 11, new object[] { "room", 6 }, 2000, "Archer", boss: Pool().First(v => v.Id == "DrownedChoir"));
        }

        [Test]
        public void TheResolver_ShouldReplayTheWebsFights_EventForEvent()
        {
            var orcs = _generator.Generate(42, new object[] { "HollowBarrow", "gate" }, 60, "Warrior", ORC_MIX);
            Fight("chain-vs-orcs", new[] { Q("Warrior", 30) }, null, orcs.Squads, null);

            var goblins = _generator.Generate(7, new object[] { "DrownedIronworks", "gate" }, 440, "Lancer");
            Fight("mixed", new[] { Q("Warrior_e2", 50), Q("Archer", 40), Q("Cavalry", 20), Q("Lancer", 30) }, null, goblins.Squads, goblins.Fighters);

            Fight("skills", new[] { Q("Warrior", 60), Q("Archer", 50) }, new[]
            {
                F("medic", "Archer", 300, 12, dmgMult: 1.2, skill: new SlotSkill("Mend", 150, 30)),
                F("wall", "Warrior", 500, hpMult: 1.3, defBonus: 2, skill: new SlotSkill("Shield", 400, 50)),
                F("cry", "Cavalry", 200, skill: new SlotSkill("WarCry", 100, 0)),
            }, new[] { Q("Lancer", 70), Q("Cavalry", 30) }, new[]
            {
                F("volley", "Archer", 400, 20, skill: new SlotSkill("Volley", 500, 45)),
                F("daze", "Lancer", 400, skill: new SlotSkill("Daze", 20, 35)),
                F("wave", "Warrior", 400, defBonus: 3, skill: new SlotSkill("Wave", 100, 40)),
            });

            Fight("strikes", new[] { Q("Lancer_e3", 40) }, new[]
            {
                F("a", "Cavalry", 400, skill: new SlotSkill("Ambush", 900, 25)),
                F("c", "Warrior", 400, hpNow: 250, skill: new SlotSkill("Crush", 700, 30)),
                F("s", "Archer", 400, skill: new SlotSkill("Sharpshot", 1200, 20)),
            }, new[] { Q("Archer_e2", 60), Q("Warrior", 80) }, new[]
            {
                F("b", "Archer", skill: new SlotSkill("Bulwark", 4, 0)),
                F("v", skill: new SlotSkill("Vigour", 200, 0)),
            });

            Fight("timeout", new SquadSpec[0], new[] { F("x", hp: 100000, dmg: 1, def: 200, cooldown: 50) },
                new SquadSpec[0], new[] { F("y", "Lancer", 100000, 1, 200, 60) });
            Fight("empty", new SquadSpec[0], null, new[] { Q("Warrior", 5) }, null);
        }

        private void Plan(string name, uint seed, object[] parts, double budget, string affinity, IReadOnlyDictionary<string, double> mix = null,
            IReadOnlyList<FighterSpec> pool = null, FighterSpec boss = null)
        {
            var plan = _generator.Generate(seed, parts, budget, affinity, mix, pool, boss);
            var lines = new List<string>
            {
                "squads|" + string.Join(";", plan.Squads.Select(s => $"{s.Troop}x{s.Count}")),
                "fighters|" + string.Join(";", plan.Fighters.Select(f => $"{f.Id},{N(f.Hp)},{N(f.Dmg)},{f.Power},{N(f.Atk)},{N(f.Def)}")),
            };
            Assert.That(lines, Is.EqualTo(_golden["plan " + name].Where(l => !l.StartsWith("villain|")).ToList()), name);
        }

        private void Fight(string name, IReadOnlyList<SquadSpec> ours, IReadOnlyList<FighterSpec> ourFighters, IReadOnlyList<SquadSpec> theirs,
            IReadOnlyList<FighterSpec> theirFighters)
        {
            var log = _combat.Resolve(_combat.BuildBoard(ours, ourFighters ?? new FighterSpec[0]),
                _combat.BuildBoard(theirs, theirFighters ?? new FighterSpec[0]));
            var expected = _golden["fight " + name];
            var actual = log.Events.Select(Line).ToList();
            for (var i = 0; i < Math.Min(expected.Count, actual.Count); i++)
                Assert.That(Same(actual[i], expected[i]), Is.True, $"{name}, event {i}:\n  ours: {actual[i]}\n  web:  {expected[i]}");
            Assert.That(actual.Count, Is.EqualTo(expected.Count), name);
        }

        private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);

        // Two lines say the same when every field does: a number by its value, so a double printed by another engine's
        // shortest form still matches.
        private static bool Same(string a, string b)
        {
            var x = a.Split('|', ',', ';', '/');
            var y = b.Split('|', ',', ';', '/');
            if (x.Length != y.Length) return false;
            for (var i = 0; i < x.Length; i++)
            {
                if (x[i] == y[i]) continue;
                if (!double.TryParse(x[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var p)
                    || !double.TryParse(y[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var q) || p != q) return false;
            }

            return true;
        }

        private static string Ref(SlotRef r) => r.ToString();

        private static string Line(BattleEvent e) => e.Kind switch
        {
            BattleEventKind.Start => $"start|{e.Ours.Count + e.Theirs.Count}|" + string.Join("/", new[] { e.Ours, e.Theirs }.Select(side =>
                string.Join(";", side.Select(p => $"{p.Slot.Id},{(p.Slot.IsHero ? "hero" : "troop")},{(p.Slot.Row == Row.Front ? "front" : "back")},{p.Slot.Type},"
                                                  + $"{p.Slot.Troop ?? "-"},{p.Slot.Count},{p.Slot.Frontage},{p.Slot.Atk},{N(p.Slot.Dmg)},{N(p.Slot.Def)},{N(p.Slot.HpUnit)},"
                                                  + $"{N(p.Slot.HpPool)},{p.Slot.Cooldown},{p.Slot.Power},{p.At.X},{p.At.Y}")))),
            BattleEventKind.Move => $"move|{e.Tick}|{Ref(e.At)}|{e.X}|{e.Y}",
            BattleEventKind.Attack => $"attack|{e.Tick}|{Ref(e.From)}|{Ref(e.At)}|{e.Hits}|{e.Dealt}|{e.Skill ?? "-"}|{e.Absorbed}|"
                                      + (e.Edge > 0 ? "adv" : e.Edge < 0 ? "dis" : "-"),
            BattleEventKind.Skill => $"skill|{e.Tick}|{Ref(e.From)}|{e.Skill}",
            BattleEventKind.Healed => $"healed|{e.Tick}|{Ref(e.At)}|{N(e.Healed)}|{e.Alive}|{N(e.HpPool)}",
            BattleEventKind.Shielded => $"shielded|{e.Tick}|{Ref(e.At)}|{e.Amount}",
            BattleEventKind.Dazed => $"dazed|{e.Tick}|{Ref(e.At)}|{e.Amount}",
            BattleEventKind.TroopsLost => $"lost|{e.Tick}|{Ref(e.At)}|{e.Alive}|{N(e.HpPool)}",
            BattleEventKind.SlotWiped => $"wiped|{e.Tick}|{Ref(e.At)}",
            _ => $"end|{e.Tick}|{(e.Winner == Side.Ours ? "ours" : "theirs")}|{(e.Reason == EndReason.Wiped ? "wiped" : "timeout")}",
        };

        // Beside this file, found from wherever the runner stands: the project in the editor, the build folder outside it.
        private static string Golden()
        {
            const string RELATIVE = "Assets/Codigames/Kingdom/Tests/Battles/battle-golden.txt";
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, RELATIVE))) return Path.Combine(dir.FullName, RELATIVE);
            for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, RELATIVE))) return Path.Combine(dir.FullName, RELATIVE);
            throw new FileNotFoundException(RELATIVE);
        }
    }
}
