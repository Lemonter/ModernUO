using ModernUO.Serialization;

namespace Server.Items;

// Mahaon: "кости демона" — костяной сет, перекрашенный в кроваво-красный (0x21, тот же
// оттенок что и MahaonHunterBag — уже устоявшийся "blood red" в этом коде), с бонусом
// к Магии и защитой выше, чем у сета из костей лича (LichBone*). Падает при разделке
// трупа демона ножом — см. Daemon/ChaosDaemon/ArcaneDaemon/DemonKnight.OnCarve /
// CarvedBoneDrops. Не путать с ванильным DaemonArms/Chest/Gloves/Legs (лут эфритов,
// не связан с разделкой) — разные классы, разные имена, специально не переиспользованы.
[SerializationGenerator(0, false)]
[Flippable(0x144f, 0x1454)]
public partial class DemonBoneChest : BaseArmor
{
    [Constructible]
    public DemonBoneChest() : base(0x144F)
    {
        Name = "нагрудник из костей демона";
        Hue = 0x21;
        SkillBonuses.SetValues(0, SkillName.Magery, 3.0);
    }

    public override double DefaultWeight => 6.0;

    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 8;
    public override int BaseColdResistance => 9;
    public override int BasePoisonResistance => 6;
    public override int BaseEnergyResistance => 9;

    public override int InitMinHits => 90;
    public override int InitMaxHits => 100;

    public override int AosStrReq => 60;
    public override int OldStrReq => 40;

    public override int OldDexBonus => -6;

    public override int ArmorBase => 50;
    public override int RevertArmorBase => 11;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Bone;
    public override CraftResource DefaultResource => CraftResource.RegularLeather;
}
