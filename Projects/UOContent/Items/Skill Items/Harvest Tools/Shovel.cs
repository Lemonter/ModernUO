using ModernUO.Serialization;
using Server.Engines.Harvest;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Shovel : BaseHarvestTool
{
    [Constructible]
    public Shovel(int uses = 50) : base(0xF39, uses)
    {
    }

    public override double DefaultWeight => 5.0;

    public override HarvestSystem HarvestSystem => Mining.System;

    public override void OnDoubleClick(Mobile from)
    {
        if (Systems.MahaonWorld.MahaonGraveyardDigSystem.IsGraveyard(from))
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            Systems.MahaonWorld.MahaonGraveyardDigSystem.TryDig(from);
            return;
        }

        base.OnDoubleClick(from);
    }
}
