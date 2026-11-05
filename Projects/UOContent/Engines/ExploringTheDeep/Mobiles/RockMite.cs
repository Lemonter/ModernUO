using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/RockMite.cs). Gravewater Lake filler
///     creature; its tail swipe is registered through <see cref="GetMonsterAbilities" /> instead of
///     ServUO's <c>SetSpecialAbility</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class RockMite : BaseCreature
{
    [Constructible]
    public RockMite() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 787;
        BaseSoundID = 1006;
        Hue = 2500;

        SetStr(733, 754);
        SetDex(126, 144);
        SetInt(75, 94);

        SetHits(803, 817);
        SetStam(126, 144);
        SetMana(75, 94);

        SetDamage(12, 19);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 50, 59);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 52, 59);
        SetResistance(ResistanceType.Poison, 80, 85);
        SetResistance(ResistanceType.Energy, 80, 90);

        SetSkill(SkillName.MagicResist, 88.5, 119.6);
        SetSkill(SkillName.Tactics, 84.9, 112.9);
        SetSkill(SkillName.Wrestling, 82.7, 119.8);
        SetSkill(SkillName.Parry, 90.0, 100.0);
        SetSkill(SkillName.DetectHidden, 42.9);

        Fame = 3500;
        Karma = -3500;

        VirtualArmor = 40;
    }

    public override string CorpseName => "труп каменного клеща";
    public override string DefaultName => "каменный клещ";

    public override MonsterAbility[] GetMonsterAbilities() => [MonsterAbilities.TailSwipe];

    public override int GetAngerSound() => 0x5A;
    public override int GetIdleSound() => 0x5A;
    public override int GetAttackSound() => 0x164;
    public override int GetHurtSound() => 0x187;
    public override int GetDeathSound() => 0x1BA;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Gems, Utility.Random(1, 5));
    }
}
