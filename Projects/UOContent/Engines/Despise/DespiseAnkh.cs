using System.Linq;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Despise;

/// <summary>
///     Ported from ServUO's Despise Revamped dungeon (Scripts/Items/Functional/
///     DespiseAnkh.cs) — touch this to receive a WispOrb matching its alignment (if your
///     karma agrees). The original's quest tie-in (QuestHelper.HasQuest&lt;
///     WhisperingWithWispsQuest&gt;/TownCryerSystem.CompleteQuest) is dropped — see
///     DespiseController's class doc comment for why; granting the orb still works exactly
///     the same, just without the quest-completion side messages.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DespiseAnkh : BaseAddon
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Alignment _alignment;

    [Constructible]
    public DespiseAnkh(Alignment alignment)
    {
        _alignment = alignment;

        switch (alignment)
        {
            default:
            case Alignment.Good:
                AddComponent(new AddonComponent(4), 0, 0, 0);
                AddComponent(new AddonComponent(5), +1, 0, 0);
                break;
            case Alignment.Evil:
                AddComponent(new AddonComponent(2), 0, 0, 0);
                AddComponent(new AddonComponent(3), 0, -1, 0);
                break;
        }
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (!from.InRange(c.Location, 3) || from.Backpack == null)
        {
            return;
        }

        if (WispOrb.Orbs.Any(x => x.Owner == from))
        {
            LabelTo(from, 1153357); // Thou can guide but one of us.
            return;
        }

        var alignment = Alignment.Neutral;

        if (from.Karma > 0 && _alignment == Alignment.Good)
        {
            alignment = Alignment.Good;
        }
        else if (from.Karma < 0 && _alignment == Alignment.Evil)
        {
            alignment = Alignment.Evil;
        }

        if (alignment != Alignment.Neutral)
        {
            var orb = new WispOrb(from, alignment);
            from.Backpack.DropItem(orb);
            from.SendLocalizedMessage(1153355); // I will follow thy guidance.
        }
        else
        {
            LabelTo(from, 1153350); // Thy spirit be not compatible with our goals!
        }
    }
}
