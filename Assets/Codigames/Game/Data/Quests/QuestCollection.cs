using Codigames.Kingdom.Quests;
using UnityEngine;

namespace Codigames.Game.Data.Quests
{
    // The quest chain: list order is chain order.
    [CreateAssetMenu(fileName = "Quests", menuName = "Kingdom/Data/Quest Collection")]
    public class QuestCollection : DefinitionCollection<IQuestDefinition, QuestAsset>
    {
        public override string Title => "Quests";
    }
}
