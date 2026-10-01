using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Functional/ChickenLizardEgg.cs). The original's
/// incubation/watering/dryness minigame and the BattleChickenLizard mutation outcome are
/// dropped — a substantial side-mechanic well outside the scope of this creature port; kept
/// only the core "double-click to hatch into a ChickenLizard" flavor via a confirm gump
/// (BaseConfirmGump, confirmed to exist locally).</summary>
[SerializationGenerator(0, false)]
public partial class ChickenLizardEgg : Item
{
    [Constructible]
    public ChickenLizardEgg() : base(0x41BF)
    {
        Hue = 555;
    }

    public override int LabelNumber => 1112467; // a chicken lizard egg

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(new ConfirmHatchGump(from, this));
        }
    }

    public void Hatch(Mobile from)
    {
        if (Deleted)
        {
            return;
        }

        from.SendLocalizedMessage(1112477); // You hatch a chicken lizard.

        var bc = new ChickenLizard { Hue = Hue };
        bc.MoveToWorld(from.Location, from.Map);

        Delete();
    }

    private class ConfirmHatchGump : BaseConfirmGump
    {
        private readonly ChickenLizardEgg _egg;

        public override int TitleNumber => 1112444;
        public override int LabelNumber => 1112445;

        public ConfirmHatchGump(Mobile from, ChickenLizardEgg egg)
        {
            _egg = egg;
        }

        public override void Confirm(Mobile from)
        {
            _egg?.Hatch(from);
        }
    }
}
