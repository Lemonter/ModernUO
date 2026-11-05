using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Creatures/KhaldunBlood.cs).
///     Dropped: the <c>Caddellite</c> damage gate (no <c>OnBeforeDamage</c> / item-socket system here) and
///     <c>SetWeaponAbility</c> (now <see cref="GetWeaponAbility" />).
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunBlood : BaseCreature
{
    public override string CorpseName => "кровавый труп";

    [Constructible]
    public KhaldunBlood() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "кровь";
        Body = 0x33;
        BaseSoundID = 456;

        Hue = Utility.RandomList(33, 1157);

        SetStr(220, 286);
        SetDex(125, 130);
        SetInt(100, 101);

        SetHits(130);

        SetDamage(8, 18);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 30, 50);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.MagicResist, 60.0, 80.0);
        SetSkill(SkillName.Tactics, 85.0, 100.0);
        SetSkill(SkillName.Wrestling, 85.0, 100.0);
        SetSkill(SkillName.DetectHidden, 40.0);

        Fame = 300;
        Karma = -300;

        VirtualArmor = 8;
    }

    public override Poison PoisonImmune => Poison.Lesser;

    public override Poison HitPoison => Poison.Lesser;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Poor);
        AddLoot(LootPack.Gems);
    }
}
