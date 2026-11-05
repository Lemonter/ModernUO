using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Luck/LuneRouge.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LuneRouge : GoldRing
{
    public override int LabelNumber => 1154372; // Lune Rouge

    [Constructible]
    public LuneRouge() : base()
    {
        this.Weight = 1.0;
        this.Hue = 1166;

        this.Attributes.Luck = 150;
        this.Attributes.AttackChance = 10;
        this.Attributes.WeaponDamage = 20;

    }
}
