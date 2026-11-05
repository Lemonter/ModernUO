using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests.Items;

namespace Server.Items;

/// <summary>
///     ServUO derives this from BaseQuestItem and points it at the LastWordsQuest chain, neither of which
///     exists in ModernUO. Ported as a transient quest giver item without the quest list.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DenthesJournal : TransientQuestGiverItem
{
    [Constructible]
    public DenthesJournal() : base(0xFF2, TimeSpan.FromSeconds(3600)) => LootType = LootType.Blessed;

    public override int LabelNumber => 1073240; // Lord Denthe's Journal
}
