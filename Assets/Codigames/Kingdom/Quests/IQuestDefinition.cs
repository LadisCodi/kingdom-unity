using System.Collections.Generic;
using Codigames.Modules.Core;

namespace Codigames.Kingdom.Quests
{
    // One step of the chain: its goal and what it pays. Its line is generated from the goal; only its name is
    // written.
    public interface IQuestDefinition : IIdentifiable
    {
        string Name { get; }

        GoalType GoalType { get; }

        // A building, a group of them (AnyDecoration), a currency, a technology, a feature, a landmark kind; null
        // when the goal names none.
        string GoalTarget { get; }

        double GoalAmount { get; }

        // For an upgrade: the level asked for; 0 when none.
        int GoalLevel { get; }

        // Currencies paid on claim — Gold, Food, Wood, Stone, Mana, Gems, Stardust — each to the purse it belongs in.
        IReadOnlyDictionary<string, double> Reward { get; }

        // Knowledge paid on claim, as a lump.
        double RewardKnowledge { get; }

        // Bag items paid on claim.
        IReadOnlyDictionary<string, double> RewardItems { get; }

        // Relic fragments paid on claim, of relics already met, rolled on the quest.
        int RewardFragments => 0;

        // Claims itself the moment it is done.
        bool AutoClaim { get; }

        // A tutorial's pacing: this many seconds after it becomes active, the first house's store is topped up with
        // the Gold it still asks. 0 = none.
        double TutorialRentSeconds { get; }
    }
}
