using System.Collections.Generic;
using Codigames.Game.UI.Data;
using UnityEngine;

namespace Codigames.Game.UI.Battles
{
    public sealed class AttackSquadData
    {
        public Sprite Bust;
        public Vector2 Shift;
        public float Scale = 1;
        public string Count;
        public int Rank;
        public string Name;
        public bool Out;
        public bool Full;
    }

    // What the attack sheet says: the two boards and their power, the roster, the price and the button's words.
    public sealed class AttackSheetData
    {
        public string Title;
        public string EnemyHead;
        public string EnemyPower;
        public IReadOnlyList<AttackSquadData> Enemy;
        public string ArmyHead;
        public string ArmyPower;
        // Short on paper: our number warms. It warns, it never blocks.
        public bool Short;
        public int Slots;
        public IReadOnlyList<AttackSquadData> Party;
        // The hero slots, every one to the ceiling; empty when no hero has come yet.
        public IReadOnlyList<HeroSlotData> Heroes = new List<HeroSlotData>();
        public string RosterHead;
        public IReadOnlyList<AttackSquadData> Roster;
        public IReadOnlyList<PriceTerm> Price;
        public string QuickDeploy;
        public string Action;
        public bool Blocked;
        public string Note;
    }
}
