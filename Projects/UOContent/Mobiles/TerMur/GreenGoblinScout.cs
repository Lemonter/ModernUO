using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/GreenGoblinScout.cs). The free-roaming
/// counterpart to EnslavedGoblinScout, and one of the names that had been failing to resolve in
/// badspawn.log — `greengoblinscout` is spawned by underworld.xml.
///
/// Two adaptations: ServUO builds it on AIType.AI_OrcScout, which doesn't exist here, so it
/// uses AI_Archer like this codebase's own OrcScout; and its TribeType.GreenGoblin is dropped,
/// since the Eodon tribe system isn't ported.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a goblin corpse")]
public partial class GreenGoblinScout : BaseCreature
{
    [Constructible]
    public GreenGoblinScout() : base(AIType.AI_Archer, FightMode.Closest, 10, 7)
    {
        ActiveSpeed = 0.2;
        PassiveSpeed = 0.4;

        Name = "a green goblin scout";
        Body = 723;
        BaseSoundID = 0x600;

        SetStr(276, 309);
        SetDex(65, 79);
        SetInt(107, 146);

        SetHits(174, 198);
        SetMana(107, 146);
        SetStam(65, 79);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 41, 49);
        SetResistance(ResistanceType.Fire, 33, 39);
        SetResistance(ResistanceType.Cold, 26, 33);
        SetResistance(ResistanceType.Poison, 14, 20);
        SetResistance(ResistanceType.Energy, 11, 20);

        SetSkill(SkillName.MagicResist, 90.7, 98.8);
        SetSkill(SkillName.Tactics, 80.9, 86.3);
        SetSkill(SkillName.Wrestling, 107.7, 119.5);
        SetSkill(SkillName.Anatomy, 80.3, 88.2);

        Fame = 1500;
        Karma = -1500;
    }

    public override int GetAngerSound() => 0x600;
    public override int GetIdleSound() => 0x600;
    public override int GetAttackSound() => 0x5FD;
    public override int GetHurtSound() => 0x5FF;
    public override int GetDeathSound() => 0x5FE;

    public override bool CanRummageCorpses => true;
    public override int TreasureMapLevel => 1;
    public override int Meat => 1;
}
