using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/TrapdoorSpider.cs). CanStealth
/// doesn't exist as an overridable member anywhere in this codebase (verified — dropped).
/// OnDamagedBySpell takes an extra `int damage` param here vs. ServUO's 1-arg version
/// (verified against Mobile.cs) — fixed.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a trapdoor spider corpse")]
public partial class TrapdoorSpider : BaseCreature
{
    [Constructible]
    public TrapdoorSpider() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 737;
        Hidden = true;

        SetStr(100, 104);
        SetDex(162, 165);
        SetInt(29, 50);

        SetHits(125, 144);

        SetDamage(15, 18);

        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Poison, 80);

        SetResistance(ResistanceType.Physical, 0);
        SetResistance(ResistanceType.Fire, 30, 35);
        SetResistance(ResistanceType.Cold, 30, 35);
        SetResistance(ResistanceType.Poison, 40, 45);
        SetResistance(ResistanceType.Energy, 95, 100);

        SetSkill(SkillName.Anatomy, 2.0, 3.8);
        SetSkill(SkillName.MagicResist, 47.5, 57.9);
        SetSkill(SkillName.Poisoning, 70.5, 73.5);
        SetSkill(SkillName.Tactics, 73.3, 78.9);
        SetSkill(SkillName.Wrestling, 92.5, 94.6);
        SetSkill(SkillName.Hiding, 110.3, 119.9);
        SetSkill(SkillName.Stealth, 110.5, 119.6);
    }

    public override string DefaultName => "паук-каменщик";

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        RevealingAction();
        base.OnDamage(amount, from, willKill);
    }

    public override void OnDamagedBySpell(Mobile from, int damage)
    {
        RevealingAction();
        base.OnDamagedBySpell(from, damage);
    }

    public override int TreasureMapLevel => 2;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }

    public override int GetIdleSound() => 1605;
    public override int GetAngerSound() => 1602;
    public override int GetHurtSound() => 1604;
    public override int GetDeathSound() => 1603;

    public override void OnThink()
    {
        if (!Alive || Deleted)
        {
            return;
        }

        if (!Hidden)
        {
            var chance = 0.05;

            if (Hits < 20)
            {
                chance = 0.1;
            }

            if (Poisoned)
            {
                chance = 0.01;
            }

            if (Utility.RandomDouble() < chance)
            {
                HideSelf();
            }

            base.OnThink();
        }
    }

    private void HideSelf()
    {
        if (Core.TickCount < NextSkillTime)
        {
            return;
        }

        Effects.SendLocationParticles(EffectItem.Create(Location, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

        PlaySound(0x22F);
        Hidden = true;

        UseSkill(SkillName.Stealth);
    }
}
