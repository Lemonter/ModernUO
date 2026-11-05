using ModernUO.Serialization;
using System;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Talismans/ConjurersTrinket.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ConjurersTrinket : BaseTalisman
{
    public override int LabelNumber => 1094800; // Conjurer's Trinket

    [Constructible]
    public ConjurersTrinket()
        : base(0x2F58)
    {
        Hue = 1157;
        Slayer = TalismanSlayerName.None;
        Attributes.BonusStr = 1;
        Attributes.RegenHits = 2;
        Attributes.WeaponDamage = 20;
        Attributes.AttackChance = 10;
    }
}
