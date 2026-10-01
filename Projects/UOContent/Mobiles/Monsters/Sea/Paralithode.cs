using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/Paralithode.cs). An
/// ambusher: while wild and out of combat it sits hidden and rooted, and only unroots when
/// something it can harm comes within five tiles or it is pulled into war mode. Tameable, but
/// never bondable and deleted on release.
///
/// Two adjustments, both because the helper does not exist here:
/// SetWeaponAbility (ServUO's pet-training registration) becomes a GetWeaponAbility override
/// rolling between the same two abilities; and OnAfterTame, which the original uses to stop the
/// hide timer, is dropped because that timer already stops itself the first tick after the
/// creature becomes controlled — the original's call is belt-and-braces over the same check.</summary>
[SerializationGenerator(0, false)]
public partial class Paralithode : BaseCreature
{
    private TimerExecutionToken _hideTimer;

    [Constructible]
    public Paralithode() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 729;
        Hue = 1922;

        SetStr(642, 729);
        SetDex(87, 103);
        SetInt(25, 30);

        SetHits(1800, 2000);
        SetMana(315, 343);

        SetDamage(20, 24);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 75);
        SetResistance(ResistanceType.Fire, 50, 60);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 50, 60);

        SetSkill(SkillName.MagicResist, 68.7, 75.0);
        SetSkill(SkillName.Anatomy, 98.0, 100.6);
        SetSkill(SkillName.Tactics, 95.8, 100.9);
        SetSkill(SkillName.Wrestling, 100.2, 109.0);
        SetSkill(SkillName.Parry, 100.0, 110.0);
        SetSkill(SkillName.Ninjitsu, 100.2, 109.0);
        SetSkill(SkillName.DetectHidden, 50.0);

        Fame = 2500;
        Karma = -2500;

        Tamable = true;
        ControlSlots = 4;
        MinTameSkill = 47.1;

        PackItem(new FertileDirt(2));

        StartHideTimer();
    }

    public override string CorpseName => "труп паралитода";
    public override string DefaultName => "паралитод";

    public override bool IsScaredOfScaryThings => false;
    public override bool IsBondable => false;
    public override FoodType FavoriteFood => FoodType.FruitsAndVeggies;
    public override bool BleedImmune => true;
    public override bool DeleteOnRelease => true;
    public override bool BardImmune => !Core.AOS || Controlled;
    public override Poison PoisonImmune => Poison.Lethal;
    public override bool CanAngerOnTame => true;
    public override bool StatLossAfterTame => true;

    public override int Meat => 9;
    public override int Hides => 20;
    public override HideType HideType => HideType.Horned;

    public override WeaponAbility GetWeaponAbility() =>
        Utility.RandomBool() ? WeaponAbility.DualWield : WeaponAbility.ForceOfNature;

    public override void GenerateLoot() => AddLoot(LootPack.Gems, 2);

    public override int GetAngerSound() => 541;
    public override int GetAttackSound() => 562;
    public override int GetIdleSound() => Controlled ? base.GetIdleSound() : 542;
    public override int GetDeathSound() => Controlled ? base.GetDeathSound() : 545;
    public override int GetHurtSound() => Controlled ? 320 : base.GetHurtSound();

    public override void OnAfterDelete()
    {
        _hideTimer.Cancel();
        base.OnAfterDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!Controlled)
        {
            StartHideTimer();
        }
    }

    private void StartHideTimer() =>
        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), HideTick, out _hideTimer);

    public void PerformHide()
    {
        if (Deleted)
        {
            return;
        }

        Hidden = true;
        CantWalk = true;
    }

    private void HideTick()
    {
        if (Controlled)
        {
            _hideTimer.Cancel();
            CantWalk = false;
            Hidden = false;
            return;
        }

        if (Warmode)
        {
            CantWalk = false;
            return;
        }

        if (!Hidden)
        {
            PerformHide();
        }

        if (Map == null || Map == Map.Internal)
        {
            return;
        }

        foreach (var m in Map.GetMobilesInRange(Location, 5))
        {
            if (m == this || m is Paralithode || !CanBeHarmful(m))
            {
                continue;
            }

            CantWalk = false;
        }
    }
}
