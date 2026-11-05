using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/GarishGingerman.cs).
///     ServUO's <c>SetMagicalAbility(MageryMastery)</c> is a pet-training school flag with no counterpart
///     in this codebase's monster-ability framework, so it stays dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GarishGingerman : BaseCreature
{
    public override string CorpseName => "труп пряничного человечка";

    [Constructible]
    public GarishGingerman() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "пряничный человечек";

        Body = 14;
        BaseSoundID = 268;
        Hue = 1461;

        SetStr(400);
        SetDex(150);
        SetInt(1200);

        SetHits(8000);

        SetDamage(21, 27);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 80, 90);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 100);

        SetSkill(SkillName.EvalInt, 120);
        SetSkill(SkillName.Magery, 120);
        SetSkill(SkillName.Meditation, 100);
        SetSkill(SkillName.MagicResist, 200);
        SetSkill(SkillName.Tactics, 100.0);
        SetSkill(SkillName.Wrestling, 120);

        Fame = 12000;
        Karma = -12000;
    }

    public override bool AlwaysMurderer => true;

    public override Poison PoisonImmune => Poison.Deadly;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}
