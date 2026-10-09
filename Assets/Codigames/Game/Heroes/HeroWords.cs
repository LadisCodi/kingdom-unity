using System;
using Codigames.Kingdom.Heroes;
using Codigames.Modules.Localization;

namespace Codigames.Game.Heroes
{
    // A hero in the player's language: its rarity, what it fights as, its skill's name and what the skill does at a
    // rank (the web's skillSentence, one sentence per skill), and its boon — every number in them derived, never
    // written out, so a sentence cannot drift from the value beside it.
    public class HeroWords
    {
        private readonly Localizer _localizer;
        private readonly NumberFormat _numbers;
        private readonly HeroLadder _ladder;

        public HeroWords(Localizer localizer, NumberFormat numbers, HeroLadder ladder)
        {
            _localizer = localizer;
            _numbers = numbers;
            _ladder = ladder;
        }

        public string Rarity(HeroRarity rarity) => rarity switch
        {
            HeroRarity.Legendary => _localizer.Tr("Legendary"),
            HeroRarity.Rare => _localizer.Tr("Rare"),
            _ => _localizer.Tr("Common"),
        };

        public string UnitType(string type) => type switch
        {
            "Warrior" => _localizer.Tr("Warrior"),
            "Lancer" => _localizer.Tr("Lancer"),
            "Archer" => _localizer.Tr("Archer"),
            _ => _localizer.Tr("Cavalry"),
        };

        public string SkillName(string skill) => skill switch
        {
            "Sharpshot" => _localizer.Tr("Sharpshot"),
            "Crush" => _localizer.Tr("Crush"),
            "Cleave" => _localizer.Tr("Cleave"),
            "Ambush" => _localizer.Tr("Ambush"),
            "Volley" => _localizer.Tr("Volley"),
            "Mend" => _localizer.Tr("Mend"),
            "Wave" => _localizer.Tr("Healing wave"),
            "Shield" => _localizer.Tr("Shield"),
            "Daze" => _localizer.Tr("Daze"),
            "WarCry" => _localizer.Tr("War cry"),
            "Bulwark" => _localizer.Tr("Bulwark"),
            "Vigour" => _localizer.Tr("Vigour"),
            "Plunder" => _localizer.Tr("Plunder"),
            "Lore" => _localizer.Tr("Lore"),
            "Seasoned" => _localizer.Tr("Seasoned"),
            _ => _localizer.Tr("Field medic"),
        };

        // What the skill does at a rank.
        public string Skill(IHeroDefinition hero, int rank)
        {
            var v = _ladder.RankValue(hero, rank);
            var x = new (string, object)[]
            {
                ("every", Seconds(hero.SkillEvery)), ("pct", _numbers.Number(Math.Round(v * 10) / 10, 1) + "%"), ("secs", Seconds(v)),
                ("n", _numbers.Count(Math.Round(v, MidpointRounding.AwayFromZero))),
            };
            return hero.Skill switch
            {
                "Sharpshot" => _localizer.Tr("Every {every}, strikes the weakest enemy for {pct} of its damage", x),
                "Crush" => _localizer.Tr("Every {every}, strikes the strongest enemy for {pct} of its damage", x),
                "Cleave" => _localizer.Tr("Every {every}, strikes every enemy in the front row for {pct} of its damage", x),
                "Ambush" => _localizer.Tr("Every {every}, strikes the weakest enemy in the back row for {pct} of its damage", x),
                "Volley" => _localizer.Tr("Every {every}, strikes every enemy for {pct} of its damage", x),
                "Mend" => _localizer.Tr("Every {every}, heals the most wounded ally for {pct} of its health", x),
                "Wave" => _localizer.Tr("Every {every}, heals every ally for {pct} of its health", x),
                "Shield" => _localizer.Tr("Every {every}, shields the weakest ally in the front row for {pct} of its own health", x),
                "Daze" => _localizer.Tr("Every {every}, holds the hardest-hitting enemy back {secs}", x),
                "WarCry" => _localizer.Tr("Every ally squad hits {pct} harder", x),
                "Bulwark" => _localizer.Tr("Every ally squad has +{n} defence", x),
                "Vigour" => _localizer.Tr("Every ally squad has {pct} more health", x),
                "Plunder" => _localizer.Tr("A won fight brings home {pct} more loot", x),
                "Lore" => _localizer.Tr("A won fight brings home {pct} more Knowledge", x),
                "Seasoned" => _localizer.Tr("A won fight teaches {pct} more Hero XP", x),
                _ => _localizer.Tr("{n} more of every hundred fallen come home wounded, not lost", x),
            };
        }

        // A Legendary's boon, or null below Legendary; a boon is a multiplier, so its number is a percent.
        public string Boon(IHeroDefinition hero)
        {
            if (string.IsNullOrEmpty(hero.BoonStat)) return null;
            var v = _numbers.Count(Math.Round((hero.BoonValue - 1) * 100, MidpointRounding.AwayFromZero)) + "%";
            return hero.BoonStat switch
            {
                "buildSpeed" => _localizer.Tr("The builders work {v} faster", ("v", v)),
                "worldRevealSpeed" => _localizer.Tr("Explorers march {v} faster", ("v", v)),
                "manaRegen" => _localizer.Tr("Your kingdom makes {v} more Mana", ("v", v)),
                "knowledgeYield" => _localizer.Tr("Every lump of Knowledge is {v} bigger", ("v", v)),
                "heroXp" => _localizer.Tr("Every room teaches your heroes {v} more", ("v", v)),
                "unitHp" => _localizer.Tr("Every unit you field has {v} more health", ("v", v)),
                "unitAtk" => _localizer.Tr("Every unit you field hits {v} harder", ("v", v)),
                "unitDef" => _localizer.Tr("Every unit you field takes {v} less", ("v", v)),
                "armyCap" => _localizer.Tr("Your halls field {v} more power", ("v", v)),
                "discoverRadius" => _localizer.Tr("Your buildings see {v} further", ("v", v)),
                "tapYield" => _localizer.Tr("Every tap is worth {v} more", ("v", v)),
                "stardustYield" => _localizer.Tr("Rooms pay {v} more Stardust", ("v", v)),
                "workerSpeed" => _localizer.Tr("Your workers walk {v} faster", ("v", v)),
                "manaCap" => _localizer.Tr("Your Mana pool holds {v} more", ("v", v)),
                "taxRate" => _localizer.Tr("Your villagers pay {v} more tax", ("v", v)),
                "workerYield" => _localizer.Tr("Every worker carries {v} more", ("v", v)),
                _ => null,
            };
        }

        private string Seconds(double v) => _numbers.Number(Math.Round(v * 10) / 10, 1) + " s";
    }
}
