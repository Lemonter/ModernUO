using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Spells.Ninjitsu;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/MinionOfScelestus.cs). Dropped: TaintedLifeAura
/// (no such property), the letter loot table (none of those correspondence items exist here), and
/// AcquireOnApproach is expressed with AcquireOnApproachDelay/Range. Dragon breath maps to the
/// FireBreath ability.</summary>
[SerializationGenerator(0, false)]
public partial class MinionOfScelestus : BaseCreature
{
    private const int MaxWanderDistance = 45;

    [Constructible]
    public MinionOfScelestus() : base(AIType.AI_Mage, FightMode.Weakest, 10, 1)
    {
        Body = 9;
        BaseSoundID = 357;
        Hue = 1159;

        SetStr(375, 405);
        SetDex(175, 200);
        SetInt(200, 225);

        SetHits(30000);

        SetDamage(19, 21);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Poison, 50);
        SetDamageType(ResistanceType.Energy, 10);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 50);
        SetResistance(ResistanceType.Cold, 100);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 50);

        SetSkill(SkillName.MagicResist, 130.8, 140.0);
        SetSkill(SkillName.Tactics, 110.0, 120.0);
        SetSkill(SkillName.Wrestling, 110.2, 120.0);
        SetSkill(SkillName.Poisoning, 120.0);
        SetSkill(SkillName.Magery, 115.0, 125.0);
        SetSkill(SkillName.EvalInt, 100.0, 120.0);

        Fame = 12500;
        Karma = -12500;
    }

    public override string CorpseName => "труп прислужника Сцелеста";
    public override string DefaultName => "прислужник Сцелеста";

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.SuperBoss);
        AddLoot(LootPack.UltraRich);
    }

    public override int TreasureMapLevel => 4;
    public override Poison PoisonImmune => Poison.LethalParasitic;
    public override Poison HitPoison => Poison.Lethal;
    public override bool ReacquireOnMovement => true;
    public override TimeSpan AcquireOnApproachDelay => TimeSpan.Zero;
    public override int AcquireOnApproachRange => 12;

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        if (0.20 > Utility.RandomDouble() && (defender.Mounted || defender.Flying))
        {
            if (defender is PlayerMobile pm)
            {
                if (AnimalForm.UnderTransformation(defender))
                {
                    defender.SendLocalizedMessage(1114066, Name); // ~1_NAME~ knocked you out of animal form!
                }
                else if (defender.Mounted)
                {
                    defender.SendLocalizedMessage(1040023); // You have been knocked off of your mount!
                }

                pm.SetMountBlock(BlockMountType.Dazed, TimeSpan.FromSeconds(10), true);
            }
            else if (defender.Mount != null)
            {
                defender.Mount.Rider = null;
            }

            defender.PlaySound(0x140);
            defender.FixedParticles(0x3728, 10, 15, 9955, EffectLayer.Waist);
        }

        base.OnGaveMeleeAttack(defender, damage);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (Utility.GetDistanceToSqrt(Location, Home) > MaxWanderDistance && (Combatant == null || 0.01 > Utility.RandomDouble()))
        {
            foreach (var m in GetMobilesInRange(10))
            {
                m.SendMessage("Прислужник вернулся домой.");
            }

            FixedParticles(0x376A, 9, 32, 0x13AF, EffectLayer.Waist);
            MoveToWorld(Home, Map);
        }
    }
}
