using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/CaptainJohnesBlade.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CaptainJohnesBlade : Scimitar
{
    public override int LabelNumber => 1154475; // CaptainJohnesBlade

    [Constructible]
    public CaptainJohnesBlade()
    {
        Slayer2 = BaseRunicTool.GetRandomSlayer();
        Attributes.AttackChance = 15;
        Attributes.DefendChance = 15;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 60;

        Hue = 2124;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        pois = 75; cold = 25;
        phys = nrgy = chaos = direct = fire = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}

[SerializationGenerator(0, false)]
public partial class GargishCaptainJohnesBlade : GlassSword
{
    public override int LabelNumber => 1154475; // GargishCaptainJohnesBlade

    [Constructible]
    public GargishCaptainJohnesBlade()
    {
        Slayer2 = BaseRunicTool.GetRandomSlayer();
        Attributes.AttackChance = 15;
        Attributes.DefendChance = 15;
        Attributes.WeaponSpeed = 30;
        Attributes.WeaponDamage = 60;

        Hue = 2124;
    }

    public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct)
    {
        pois = 75; cold = 25;
        phys = nrgy = chaos = direct = fire = 0;
    }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;
}
