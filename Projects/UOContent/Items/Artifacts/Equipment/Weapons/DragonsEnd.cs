using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/DragonsEnd.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonsEnd : Longsword
{
    [Constructible]
    public DragonsEnd()
        : base()
    {
        Hue = 0x554;
        Slayer = SlayerName.DragonSlaying;
        Attributes.AttackChance = 10;
        Attributes.WeaponDamage = 60;
        WeaponAttributes.ResistFireBonus = 20;
    }

    public override int LabelNumber => 1079791; // Dragon's End

    public override int ArtifactRarity => 11;

    public override int InitMinHits => 225;

    public override int InitMaxHits => 225;

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        phys = fire = nrgy = pois = direct = chaos = 0;
        cold = 100;
    }
}
