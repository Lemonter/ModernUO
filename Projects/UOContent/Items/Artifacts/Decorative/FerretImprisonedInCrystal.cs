using ModernUO.Serialization;
using System;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/FerretImprisonedInCrystal.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class FerretImprisonedInCrystal : BaseImprisonedMobile
{
    [Constructible]
    public FerretImprisonedInCrystal()
        : base(0x1F19)
    {
        this.Name = "хорёк, заточённый в кристалле";
        this.Weight = 1.0;
    }

    public override BaseCreature Summon => new ShimmeringFerret();
}

[SerializationGenerator(0, false)]
public partial class ShimmeringFerret : Ferret
{
    [Constructible]
    public ShimmeringFerret()
        : base()
    {
        this.SetSkill(SkillName.MagicResist, 100.0);
        this.SetSkill(SkillName.Tactics, 100.0);
        this.SetSkill(SkillName.Wrestling, 100.0);
    }

    public override bool DeleteOnRelease => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1049646); // (summoned)
    }
}
