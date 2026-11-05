using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Covetous void pool (Scripts/Services/Revamped Dungeons/Covetous Void Spawn/
///     Creatures/CovetousCreature.cs) — base for the nine creature families that spawn in the pool and
///     scale with the wave level.
///
///     The original derives <c>IsMagical</c> from <c>AIObject is MageAI</c> and gates mana/skills on it;
///     that survives unchanged. <c>GivesFameAndKarmaAward</c> does not exist on this codebase's
///     BaseCreature (see DespiseCreature for the same drop), and the constructor's trailing
///     active/passive-speed arguments are gone — speeds are derived from Body/AI instead.
///
///     The waypoint dwell timer was a <c>Tuple&lt;WayPoint, DateTime&gt;</c>; here it is the waypoint's
///     serial plus a <see cref="Core.TickCount" /> deadline, so no Item reference is held across
///     waypoint changes and the deadline comparison stays wraparound-safe.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CovetousCreature : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _voidSpawn;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _level;

    private uint _wayPointSerial;
    private long _wayPointDeadline;

    private const long WayPointDwell = 120_000;

    public virtual int Stage => Math.Max(1, _level / 5);
    public virtual int MaxStage => 15;

    public virtual int StatRatio => Utility.RandomMinMax(35, 60);

    public virtual double SkillStart => Utility.RandomMinMax(35, 50);
    public virtual double SkillMax => 160.0;

    public virtual int StrStart => Utility.RandomMinMax(91, 100);
    public virtual int DexStart => Utility.RandomMinMax(91, 100);
    public virtual int IntStart => IsMagical ? Utility.RandomMinMax(91, 100) : 1;

    public virtual int StrMax => 410;
    public virtual int DexMax => 422;
    public virtual int IntMax => 250;

    public virtual int MaxHits => 2400;
    public virtual int MaxStam => 3000;
    public virtual int MaxMana => IsMagical ? 8500 : 1500;

    public virtual int MinDamMax => 5;
    public virtual int MaxDamMax => 12;

    public virtual int MinDamStart => 5;
    public virtual int MaxDamStart => 15;

    public virtual int HitsStart => StrStart + (int)(StrStart * (StatRatio / 100.0));
    public virtual int StamStart => DexStart + (int)(DexStart * (StatRatio / 100.0));
    public virtual int ManaStart => IntStart + (int)(IntStart * (StatRatio / 100.0));

    public virtual bool RaiseDamage => true;
    public virtual double RaiseDamageFactor => 0.33;

    public virtual int ResistStart => 25;
    public virtual int ResistMax => 95;

    public virtual bool IsMagical => AIObject is MageAI;

    public override bool PlayerRangeSensitive => false;
    public override bool CanDestroyObstacles => true;

    public CovetousCreature(AIType ai, int level = 60, bool voidSpawn = false)
        : base(ai, FightMode.Closest, 10, 1)
    {
        _level = level;
        _voidSpawn = voidSpawn;

        SetSkill(SkillName.MagicResist, SkillStart);
        SetSkill(SkillName.Tactics, SkillStart);
        SetSkill(SkillName.Wrestling, SkillStart);
        SetSkill(SkillName.Anatomy, SkillStart);

        if (ai == AIType.AI_Mage)
        {
            SetSkill(SkillName.Magery, SkillStart);
            SetSkill(SkillName.EvalInt, SkillStart);
            SetSkill(SkillName.Meditation, SkillStart);
        }

        SetStr(StrStart);
        SetDex(DexStart);
        SetInt(IntStart);

        SetHits(HitsStart);
        SetStam(StamStart);
        SetMana(ManaStart);

        SetDamage(MinDamStart, MaxDamStart);
        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, ResistStart - 5, ResistStart + 5);
        SetResistance(ResistanceType.Fire, ResistStart - 5, ResistStart + 5);
        SetResistance(ResistanceType.Cold, ResistStart - 5, ResistStart + 5);
        SetResistance(ResistanceType.Poison, ResistStart - 5, ResistStart + 5);
        SetResistance(ResistanceType.Energy, ResistStart - 5, ResistStart + 5);

        if (Stage > 1)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(0.5), SetPower);
        }

        Fame = Math.Min(8500, _level * 142);
        Karma = Math.Min(8500, _level * 142) * -1;
    }

    public override void OnThink()
    {
        base.OnThink();

        if (!Alive)
        {
            return;
        }

        var current = CurrentWayPoint?.Serial.Value ?? 0u;
        var now = Core.TickCount;

        if (current != _wayPointSerial)
        {
            _wayPointSerial = current;
            _wayPointDeadline = current == 0 ? 0 : now + WayPointDwell;
            return;
        }

        if (current != 0 && now - _wayPointDeadline >= 0)
        {
            if (CheckCanTeleport())
            {
                MoveToWorld(CurrentWayPoint!.Location, Map);
            }

            _wayPointDeadline = now + WayPointDwell;
        }
    }

    protected virtual bool CheckCanTeleport()
    {
        if (CurrentWayPoint == null || Frozen || Paralyzed || Combatant?.InLOS(this) == true)
        {
            return false;
        }

        var map = Map;

        if (map == null)
        {
            return false;
        }

        foreach (var m in map.GetMobilesInRange(Location, 10))
        {
            if (m is PlayerMobile { AccessLevel: AccessLevel.Player })
            {
                return false;
            }
        }

        foreach (var item in map.GetItemsInRange(Location, 8))
        {
            // Doors and their frames block the step-through used to escape a stuck waypoint.
            if (item.ItemID is 0x82 or 0x3946 or 0x3956 or 0x3967 or 0x3979)
            {
                return false;
            }
        }

        return true;
    }

    public override void GenerateLoot()
    {
        if (!_voidSpawn)
        {
            AddLoot(LootPack.Rich, Math.Max(1, Stage / 2));
        }
    }

    /// <summary>Raises the creature toward its level's thresholds; the original ran this from a
    /// timer once at spawn for anything past stage 1.</summary>
    public virtual void SetPower()
    {
        foreach (var skill in Skills)
        {
            if (skill is { Base: > 0 } && skill.Base < SkillMax)
            {
                var toRaise = SkillMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5);

                if (toRaise > skill.Base)
                {
                    skill.Base = Math.Min(SkillMax, toRaise);
                }
            }
        }

        SetResistance(ResistanceType.Physical, ResistMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5));
        SetResistance(ResistanceType.Fire, ResistMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5));
        SetResistance(ResistanceType.Cold, ResistMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5));
        SetResistance(ResistanceType.Poison, ResistMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5));
        SetResistance(ResistanceType.Energy, ResistMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5));

        var strRaise = StrMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5);
        var dexRaise = DexMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5);
        var intRaise = IntMax / MaxStage * Stage + Utility.RandomMinMax(-5, 5);

        if (strRaise > RawStr)
        {
            SetStr(Math.Min(StrMax, strRaise));
        }

        if (dexRaise > RawDex)
        {
            SetDex(Math.Min(DexMax, dexRaise));
        }

        if (intRaise > RawInt)
        {
            SetInt(Math.Min(IntMax, intRaise));
        }

        var hitsRaise = MaxHits / 60 * _level + Utility.RandomMinMax(-5, 5);
        var stamRaise = MaxStam / 60 * _level + Utility.RandomMinMax(-5, 5);
        var manaRaise = MaxMana / 60 * _level + Utility.RandomMinMax(-5, 5);

        if (hitsRaise > HitsMax)
        {
            SetHits(Math.Min(MaxHits, hitsRaise));
        }

        if (stamRaise > StamMax)
        {
            SetStam(Math.Min(MaxStam, stamRaise));
        }

        if (manaRaise > ManaMax)
        {
            SetMana(Math.Min(MaxMana, manaRaise));
        }

        if (RaiseDamage && Utility.RandomDouble() < RaiseDamageFactor)
        {
            DamageMin = Math.Min(MinDamMax, DamageMin + 1);
            DamageMax = Math.Min(MaxDamMax, DamageMax + 1);
        }
    }
}
