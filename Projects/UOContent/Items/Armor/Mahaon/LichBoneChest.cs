using ModernUO.Serialization;

namespace Server.Items;

// Mahaon: "кости лича" — обычный костяной сет, перекрашенный в пепельно-серый
// (0x455, тот же оттенок что и MahaonMinerBag — уже устоявшийся "серый" в этом коде),
// с бонусом к Некромантии и чуть большей защитой, чем ванильный Bone-сет.
// Падает при разделке трупа лича ножом — см. Lich.OnCarve / CarvedBoneDrops.
[SerializationGenerator(0, false)]
[Flippable(0x144f, 0x1454)]
public partial class LichBoneChest : BaseArmor
{
    [Constructible]
    public LichBoneChest() : base(0x144F)
    {
        Name = "нагрудник из костей лича";
        Hue = 0x455;
        SkillBonuses.SetValues(0, SkillName.Necromancy, 3.0);
    }

    public override double DefaultWeight => 6.0;

    public override int BasePhysicalResistance => 5;
    public override int BaseFireResistance => 5;
    public override int BaseColdResistance => 6;
    public override int BasePoisonResistance => 4;
    public override int BaseEnergyResistance => 6;

    public override int InitMinHits => 60;
    public override int InitMaxHits => 70;

    public override int AosStrReq => 60;
    public override int OldStrReq => 40;

    public override int OldDexBonus => -6;

    public override int ArmorBase => 38;
    public override int RevertArmorBase => 11;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;
}
