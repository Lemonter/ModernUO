using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
[Flippable(0x1452, 0x1457)]
public partial class LichBoneLegs : BaseArmor
{
    [Constructible]
    public LichBoneLegs() : base(0x1452)
    {
        Name = "поножи из костей лича";
        Hue = 0x455;
        SkillBonuses.SetValues(0, SkillName.Necromancy, 3.0);
    }

    public override double DefaultWeight => 3.0;

    public override int BasePhysicalResistance => 5;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 6;
    public override int BasePoisonResistance => 4;
    public override int BaseEnergyResistance => 6;

    public override int InitMinHits => 60;
    public override int InitMaxHits => 70;

    public override int AosStrReq => 55;
    public override int OldStrReq => 40;

    public override int OldDexBonus => -4;

    public override int ArmorBase => 38;
    public override int RevertArmorBase => 7;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;
}
