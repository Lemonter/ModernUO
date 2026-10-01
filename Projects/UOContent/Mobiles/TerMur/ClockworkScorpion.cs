using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/ClockworkScorpion.cs).
/// IRepairableMobile doesn't exist here (verified) — dropped along with RepairResource. The
/// legacy version-0-migration SetMagicalAbility block dropped too (fresh port, no prior
/// version to migrate from, and SetMagicalAbility doesn't exist regardless).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a clockwork scorpion corpse")]
public partial class ClockworkScorpion : BaseCreature
{
    [Constructible]
    public ClockworkScorpion() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 717;

        SetStr(225, 245);
        SetDex(80, 100);
        SetInt(30, 40);

        SetHits(151, 210);

        SetDamage(5, 10);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Poison, 40);

        SetResistance(ResistanceType.Physical, 80, 100);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 60, 80);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 10, 25);

        SetSkill(SkillName.MagicResist, 30.1, 50.0);
        SetSkill(SkillName.Poisoning, 95.1, 100.0);
        SetSkill(SkillName.Tactics, 70.1, 90.0);
        SetSkill(SkillName.Wrestling, 50.1, 80.0);

        Fame = 3500;
        Karma = -3500;

        ControlSlots = 1;
    }

    public override string DefaultName => "механический скорпион";

    public override bool IsScaredOfScaryThings => false;
    public override bool IsScaryToPets => true;
    public override bool IsBondable => false;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override bool AutoDispel => !Controlled;
    public override bool BleedImmune => true;
    public override bool DeleteOnRelease => true;
    public override bool BardImmune => Controlled;
    public override Poison PoisonImmune => Poison.Lethal;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager, 2);
    }

    public override int GetAngerSound() => 541;

    public override int GetIdleSound() => !Controlled ? 542 : base.GetIdleSound();

    public override int GetDeathSound() => !Controlled ? 545 : base.GetDeathSound();

    public override int GetAttackSound() => 562;

    public override int GetHurtSound() => Controlled ? 320 : base.GetHurtSound();

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        var master = GetMaster();

        if (master != null && master.Player && master.Map == Map && master.InRange(Location, 20))
        {
            if (master.Mana >= amount)
            {
                master.Mana -= amount;
            }
            else
            {
                amount -= master.Mana;
                master.Mana = 0;
                master.Damage(amount);
            }
        }

        base.OnDamage(amount, from, willKill);
    }
}
