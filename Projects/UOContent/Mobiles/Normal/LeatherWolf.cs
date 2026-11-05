using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/LeatherWolf.cs). Dropped: IRepairableMobile
/// (no repair interface here) and the 20% LeatherWolfSkin drop (that item does not exist). The
/// 30-second fellow timer becomes a TimerExecutionToken cancelled in OnAfterDelete; fellows are
/// spawned with the same "random point in range, average Z" helper the other ports use because
/// Map.GetSpawnPosition does not exist.</summary>
[SerializationGenerator(0, false)]
public partial class LeatherWolf : BaseCreature
{
    private const int MaxFellows = 3;

    private readonly List<Mobile> _fellows = new();
    private TimerExecutionToken _fellowsTimer;

    [Constructible]
    public LeatherWolf() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        Body = 739;
        BaseSoundID = 0xE5;

        SetStr(104, 125);
        SetDex(102, 125);
        SetInt(20, 34);

        SetHits(291, 329);

        SetDamage(12, 23);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 49);
        SetResistance(ResistanceType.Fire, 20, 29);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 21, 29);
        SetResistance(ResistanceType.Energy, 20, 25);

        SetSkill(SkillName.MagicResist, 79.5, 94.9);
        SetSkill(SkillName.Tactics, 80.6, 89.4);
        SetSkill(SkillName.Wrestling, 70.9, 88.4);

        Fame = 4500;
        Karma = -4500;

        Tamable = false;
    }

    public override string CorpseName => "труп волка";
    public override string DefaultName => "кожаный волк";

    public override bool AlwaysMurderer => true;

    public override int Meat => 1;
    public override PackInstinct PackInstinct => PackInstinct.Canine;
    public override int Hides => 7;
    public override FoodType FavoriteFood => FoodType.Meat;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.BleedAttack;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager, 2);
    }

    public override int GetIdleSound() => 1545;
    public override int GetAngerSound() => 1542;
    public override int GetHurtSound() => 1544;
    public override int GetDeathSound() => 1543;

    public override void OnCombatantChange()
    {
        base.OnCombatantChange();

        if (Combatant != null && !_fellowsTimer.Running)
        {
            Timer.StartTimer(TimeSpan.Zero, TimeSpan.FromSeconds(30.0), CheckFellows, out _fellowsTimer);
        }
    }

    public void CheckFellows()
    {
        if (!Alive || Combatant == null || Map == null || Map == Map.Internal)
        {
            for (var i = 0; i < _fellows.Count; i++)
            {
                _fellows[i].Delete();
            }

            _fellows.Clear();
            _fellowsTimer.Cancel();
            return;
        }

        for (var i = _fellows.Count - 1; i >= 0; i--)
        {
            if (_fellows[i].Deleted)
            {
                _fellows.RemoveAt(i);
            }
        }

        var spawned = false;

        for (var i = _fellows.Count; i < MaxFellows; i++)
        {
            var friend = new LeatherWolfFellow();

            friend.MoveToWorld(FindFellowSpawn(), Map);
            friend.Combatant = Combatant;

            if (friend.AIObject != null)
            {
                friend.AIObject.Action = ActionType.Combat;
            }

            _fellows.Add(friend);
            spawned = true;
        }

        if (spawned)
        {
            Say(1113132); // The leather wolf howls for help
            PlaySound(0xE6);
        }
    }

    private Point3D FindFellowSpawn()
    {
        for (var i = 0; i < 10; i++)
        {
            var loc = new Point3D(X + Utility.RandomMinMax(-6, 6), Y + Utility.RandomMinMax(-6, 6), Z);
            loc.Z = Map.GetAverageZ(loc.X, loc.Y);

            if (Map.CanSpawnMobile(loc))
            {
                return loc;
            }
        }

        return Location;
    }

    public override void OnAfterDelete()
    {
        _fellowsTimer.Cancel();
        _fellows.Clear();

        base.OnAfterDelete();
    }
}

[SerializationGenerator(0, false)]
public partial class LeatherWolfFellow : BaseCreature
{
    [Constructible]
    public LeatherWolfFellow() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 739;
        BaseSoundID = 0xE5;

        SetStr(105, 115);
        SetDex(101, 114);
        SetInt(23, 34);

        SetHits(81, 110);

        SetDamage(9, 20);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 36, 50);
        SetResistance(ResistanceType.Fire, 10, 18);
        SetResistance(ResistanceType.Cold, 23, 29);
        SetResistance(ResistanceType.Poison, 10, 17);
        SetResistance(ResistanceType.Energy, 10, 15);

        SetSkill(SkillName.MagicResist, 59.2, 75);
        SetSkill(SkillName.Tactics, 53.3, 64.8);
        SetSkill(SkillName.Wrestling, 64, 79);

        Fame = 2500;
        Karma = -2500;
    }

    public override string CorpseName => "труп волка";
    public override string DefaultName => "кожаный волк";

    public override PackInstinct PackInstinct => PackInstinct.Canine;
}
