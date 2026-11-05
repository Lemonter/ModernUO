using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     ServUO's DragonBreath special ability comes through <see cref="GetMonsterAbilities" /> as
///     <see cref="MonsterAbilities.FireBreath" /> (40% of its damage is fire). Its IRepairableMobile
///     implementation has no counterpart and stays dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Vollem : BaseCreature
{
    [Constructible]
    public Vollem() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 0x125;
        SetSpeed(0.2, 0.4);

        SetStr(496, 524);
        SetDex(88, 105);
        SetInt(94, 117);

        SetHits(300, 315);
        SetDamage(16, 22);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Fire, 40);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 20, 30);

        SetSkill(SkillName.MagicResist, 90.4, 97.8);
        SetSkill(SkillName.Tactics, 99.0, 99.5);
        SetSkill(SkillName.Wrestling, 84.2, 87.7);
        SetSkill(SkillName.EvalInt, 21.9, 43.3);
        SetSkill(SkillName.Magery, 25.6, 38.6);

        ControlSlots = 2;
    }

    public override string DefaultName => "воллем";
    public override string CorpseName => "труп воллема";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override bool IsScaredOfScaryThings => false;
    public override bool IsScaryToPets => true;
    public override bool IsBondable => false;
    public override bool DeleteOnRelease => true;
    public override bool AutoDispel => !Controlled;
    public override bool BleedImmune => true;
    public override bool BardImmune => !Core.AOS || Controlled;
    public override Poison PoisonImmune => Poison.Lethal;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override int Meat => 5;
    public override int Hides => 10;

    public override int GetAngerSound() => 541;

    public override int GetIdleSound()
    {
        if (!Controlled)
        {
            return 542;
        }

        return base.GetIdleSound();
    }

    public override int GetDeathSound()
    {
        if (!Controlled)
        {
            return 545;
        }

        return base.GetDeathSound();
    }

    public override int GetAttackSound() => 562;

    public override int GetHurtSound()
    {
        if (Controlled)
        {
            return 320;
        }

        return base.GetHurtSound();
    }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        if (Controlled || Summoned)
        {
            var master = ControlMaster;

            master ??= SummonMaster;

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
        }

        base.OnDamage(amount, from, willKill);
    }
}
