using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/TwistedHolidayTree.cs).
///     ServUO's <c>SetAreaEffect(AreaEffect.ExplosiveGoo)</c> is a periodic fire burst on a combat target
///     with no counterpart in this codebase's monster-ability framework or its built-in aura, so it stays
///     dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TwistedHolidayTree : BaseCreature
{
    public override string CorpseName => "труп искорёженного праздничного дерева";

    [Constructible]
    public TwistedHolidayTree() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "искорёженное праздничное дерево";

        Body = 47;
        BaseSoundID = 442;
        Hue = 1175;

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

        SetSkill(SkillName.EvalInt, 200);
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
