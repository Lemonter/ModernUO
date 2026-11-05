using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/AbominableSnowman.cs).
///     ServUO's two <c>SetWeaponAbility</c> registrations come back through <see cref="GetWeaponAbility" />
///     as a random pick per swing, which is what ServUO's ability profile did.
///     <c>SetSpecialAbility(TrueFear)</c> has no counterpart and stays dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class AbominableSnowman : BaseCreature
{
    public override string CorpseName => "труп снежного человека";

    [Constructible]
    public AbominableSnowman() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "снежный человек";

        Body = 241;
        BaseSoundID = 367;
        Hue = 1150;

        SetStr(800);
        SetDex(150);
        SetInt(1200);

        SetHits(8000);

        SetDamage(21, 27);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 100);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);

        SetSkill(SkillName.Anatomy, 115, 120);
        SetSkill(SkillName.Poisoning, 120);
        SetSkill(SkillName.MagicResist, 115, 120);
        SetSkill(SkillName.Tactics, 100.0);
        SetSkill(SkillName.Wrestling, 120, 130);

        Fame = 12000;
        Karma = -12000;
    }

    public override bool AlwaysMurderer => true;

    public override WeaponAbility GetWeaponAbility() => Utility.Random(2) switch
    {
        0 => WeaponAbility.ConcussionBlow,
        _ => WeaponAbility.CrushingBlow
    };

    public override Poison PoisonImmune => Poison.Deadly;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}
