using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/RabidReindeer.cs).
///     ServUO's <c>SetSpecialAbility(VenomousBite)</c> has no counterpart in this codebase's
///     monster-ability framework, so it stays dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class RabidReindeer : BaseCreature
{
    public override string CorpseName => "труп бешеного оленя";

    [Constructible]
    public RabidReindeer() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "бешеный олень";

        Body = 0xEA;
        Hue = 2707;

        SetStr(800);
        SetDex(150);
        SetInt(1200);

        SetHits(8000);

        SetDamage(22, 29);

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

    public override int Meat => 6;

    public override int Hides => 15;

    public override int GetAttackSound() => 0x82;

    public override int GetHurtSound() => 0x83;

    public override int GetDeathSound() => 0x84;

    public override Poison HitPoison => Poison.Lethal;

    public override bool AlwaysMurderer => true;

    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}
