using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/FireAnt.cs). ServUO's
/// <c>SetAreaEffect(AreaEffect.ExplosiveGoo)</c> stays dropped: it is a periodic fire burst on a
/// combat target with no counterpart in the monster-ability framework
/// (<see cref="GetMonsterAbilities" />) or the built-in aura. The rare SearedFireAntGoo drop
/// dropped too — that item doesn't exist upstream in ServUO's own master tree either (confirmed
/// via a full tree search), so there's no source to port from.</summary>
[SerializationGenerator(0, false)]
public partial class FireAnt : BaseCreature
{
    [Constructible]
    public FireAnt() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 738;

        SetStr(225);
        SetDex(108);
        SetInt(25);

        SetHits(299);

        SetDamage(15, 18);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Fire, 60);

        SetResistance(ResistanceType.Physical, 52);
        SetResistance(ResistanceType.Fire, 96);
        SetResistance(ResistanceType.Cold, 36);
        SetResistance(ResistanceType.Poison, 40);
        SetResistance(ResistanceType.Energy, 36);

        SetSkill(SkillName.Anatomy, 8.7);
        SetSkill(SkillName.MagicResist, 53.1);
        SetSkill(SkillName.Tactics, 77.2);
        SetSkill(SkillName.Wrestling, 75.4);
    }

    public override string CorpseName => "труп огненного муравья";
    public override string DefaultName => "огненный муравей";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average, 2);
    }

    public override int TreasureMapLevel => 3;

    public override int GetIdleSound() => 846;
    public override int GetAngerSound() => 849;
    public override int GetHurtSound() => 852;
    public override int GetDeathSound() => 850;
}
