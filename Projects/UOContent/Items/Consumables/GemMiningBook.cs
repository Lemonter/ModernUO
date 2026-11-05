using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флага PlayerMobile.GemMining в этом репозитории нет, поэтому
///     книга не обучает добыче самоцветов — остаётся декоративным руководством.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GemMiningBook : Item
{
    [Constructible]
    public GemMiningBook() : base(0xFBE) => Weight = 1.0;

    public override int LabelNumber => 1112240; // Mining for Quality Gems

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile pm)
        {
            return;
        }

        if (!IsChildOf(pm.Backpack))
        {
            pm.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (pm.Skills.Mining.Base < 100.0)
        {
            pm.SendLocalizedMessage(1080041); // Only a Grandmaster Miner can learn from this book.
        }
        else
        {
            pm.SendLocalizedMessage(1112238); // You have learned to mine for gems.
        }
    }
}
