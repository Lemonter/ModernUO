using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Glasses/Glasses.cs).
///     Glasses: Dropped IRepairable — not present in this codebase.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Glasses : BaseArmor
{
    [Constructible]
    public Glasses()
        : base(0x2FB8)
    {
        Weight = 2.0;
    }

    public override int AosStrReq => 45;

    public override int OldStrReq => 40;

    public override int ArmorBase => 30;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Leather;

    public override CraftResource DefaultResource => CraftResource.RegularLeather;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override bool CanEquip(Mobile m)
    {
        if (m.NetState != null && !m.NetState.SupportsExpansion(Expansion.ML))
        {
            m.SendLocalizedMessage(1072791); // You must upgrade to Mondain's Legacy in order to use that item.

            return false;
        }

        return true;
    }
}
