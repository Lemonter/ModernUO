using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
[Flippable(0x144e, 0x1453)]
public partial class DemonBoneArms : BaseArmor
{
    [Constructible]
    public DemonBoneArms() : base(0x144E)
    {
        Name = "наручи из костей демона";
        Hue = 0x21;
        SkillBonuses.SetValues(0, SkillName.Magery, 3.0);
    }

    public override double DefaultWeight => 2.0;

    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 8;
    public override int BaseColdResistance => 9;
    public override int BasePoisonResistance => 6;
    public override int BaseEnergyResistance => 9;

    public override int InitMinHits => 90;
    public override int InitMaxHits => 100;

    public override int AosStrReq => 55;
    public override int OldStrReq => 40;

    public override int OldDexBonus => -2;

    public override int ArmorBase => 50;
    public override int RevertArmorBase => 4;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;
}
