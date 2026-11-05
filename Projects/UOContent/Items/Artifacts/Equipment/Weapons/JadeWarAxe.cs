using ModernUO.Serialization;
using System;
using Server.Engines.Harvest;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/JadeWarAxe.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class JadeWarAxe : WarAxe
{
    public override int LabelNumber => 1115445; // Jade War Axe

    [Constructible]
    public JadeWarAxe()
    {
        Hue = 1162;
        Slayer = SlayerName.ReptilianDeath;
        WeaponAttributes.HitFireball = 30;
        WeaponAttributes.HitLowerDefend = 60;
        Attributes.WeaponSpeed = 20;
        Attributes.WeaponDamage = 50;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
