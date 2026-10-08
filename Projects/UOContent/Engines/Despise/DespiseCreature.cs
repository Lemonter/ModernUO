using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Despise;

/// <summary>
///     Ported from ServUO's Despise Revamped dungeon (Scripts/Mobiles/Normal/DespiseCreature.cs).
///     Base for the 16 good/evil wildlife roaming the dungeon (see DespiseGoodCreatures.cs/
///     DespiseEvilCreatures.cs) — players "possess" one via a WispOrb, and it grows stronger
///     (Power, 1-15) as its Progress accumulates from combat, raising its stats/skills/resists
///     each threshold. Alignment is derived from Karma, same as the original.
///
///     The original's Power/Progress were plain properties with side effects baked into their
///     setters (raising stats, invalidating the owning orb's tooltip) — ModernUO's
///     [SerializableField] generates a plain get/set property with no room for that, so those
///     two are exposed as explicit methods (GainPower/AddProgress) instead of relying on
///     setter magic; every call site that used to write `Power = x` now calls GainPower(x).
///
///     Three ServUO-only BaseCreature flags this class used (ForceNotoriety,
///     GivesFameAndKarmaAward, CanAutoStable) don't exist anywhere in this codebase's
///     Mobile/BaseCreature — dropped; IsBondable/InitialInnocent/AlwaysMurderer (which DO
///     exist) already cover the load-bearing behavior. GetHitPoison() is HitPoison (a
///     property, not a method) here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DespiseCreature : BaseCreature
{
    [SerializableField(0)]
    private WispOrb _orb;

    [SerializableField(1)]
    private int _power;

    [SerializableField(2)]
    private int _maxPower;

    [SerializableField(3)]
    private int _progress;

    [CommandProperty(AccessLevel.GameMaster)]
    public virtual Alignment Alignment => Karma switch
    {
        > 0 => Alignment.Good,
        < 0 => Alignment.Evil,
        _   => Alignment.Neutral
    };

    public virtual int TightLeashLength => 1;
    public virtual int ShortLeashLength => 1;
    public virtual int LongLeashLength => 10;

    public virtual int StatRatio => Utility.RandomMinMax(35, 60);

    public virtual double SkillStart => Utility.RandomMinMax(80.0, 130.0);
    public virtual double SkillMax => _maxPower == 15 ? 130.0 : 110.0;

    public virtual int StrStart => Utility.RandomMinMax(91, 100);
    public virtual int DexStart => Utility.RandomMinMax(91, 100);
    public virtual int IntStart => Utility.RandomMinMax(91, 100);

    public virtual int StrMax => 600;
    public virtual int DexMax => 150;
    public virtual int IntMax => 450;

    public virtual int HitsStart => StrStart + (int)(StrStart * (StatRatio / 100.0));
    public virtual int StamStart => DexStart + (int)(DexStart * (StatRatio / 100.0));
    public virtual int ManaStart => IntStart + (int)(IntStart * (StatRatio / 100.0));

    public virtual int MaxHits => 1000;
    public virtual int MaxStam => 1000;
    public virtual int MaxMana => 1500;

    public virtual int MinDamStart => 8;
    public virtual int MaxDamStart => 13;

    public virtual int MinDamMax => 12;
    public virtual int MaxDamMax => 17;

    public virtual bool RaiseDamage => false;
    public virtual double RaiseDamageFactor => 0.33;

    public virtual int GetFame => _power * 500;
    public virtual int GetKarmaGood => _power * 500;
    public virtual int GetKarmaEvil => _power * -500;

    public override bool Commandable => false;

    public override bool InitialInnocent => Alignment < Alignment.Evil;
    public override bool AlwaysMurderer => Alignment == Alignment.Evil;
    public override bool IsBondable => false;

    public override Poison HitPoison => null;

    public override TimeSpan ReacquireDelay =>
        !Controlled || _orb == null || _orb.Aggression == Aggression.Defensive
            ? TimeSpan.FromSeconds(10.0)
            : TimeSpan.FromSeconds(Utility.RandomMinMax(4, 6));

    // Was base(ai, fightmode, 10, 1, .2, .4) — this codebase's BaseCreature constructor
    // dropped the trailing active/passive-speed args entirely (computed automatically via
    // GetSpeeds() from Body/AI type instead), keeping just RangePerception/RangeFight.
    public DespiseCreature(AIType ai, FightMode fightmode) : base(ai, fightmode, 10, 1)
    {
        _maxPower = 10;
        _power = 1;

        SetStr(StrStart);
        SetDex(DexStart);
        SetInt(IntStart);

        SetHits(HitsStart);
        SetStam(StamStart);
        SetMana(ManaStart);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 5, 50);
        SetResistance(ResistanceType.Fire, 5, 50);
        SetResistance(ResistanceType.Cold, 5, 50);
        SetResistance(ResistanceType.Poison, 5, 50);
        SetResistance(ResistanceType.Energy, 5, 50);

        SetSkill(SkillName.Wrestling, SkillStart);
        SetSkill(SkillName.Tactics, SkillStart);
        SetSkill(SkillName.MagicResist, SkillStart);
        SetSkill(SkillName.Anatomy, SkillStart);
        SetSkill(SkillName.Poisoning, SkillStart);
        SetSkill(SkillName.DetectHidden, SkillStart);
        SetSkill(SkillName.Parry, SkillStart);
        SetSkill(SkillName.Magery, SkillStart);
        SetSkill(SkillName.EvalInt, SkillStart);
        SetSkill(SkillName.Meditation, SkillStart);
        SetSkill(SkillName.Necromancy, SkillStart);
        SetSkill(SkillName.SpiritSpeak, SkillStart);
        SetSkill(SkillName.Focus, SkillStart);
        SetSkill(SkillName.Discordance, SkillStart);

        SetDamage(MinDamStart, MaxDamStart);
    }

    // Was NoLootOnDeath = true — that flag doesn't exist anywhere in this codebase's
    // Mobile/BaseCreature. Overriding GenerateLoot() as a no-op achieves the same thing
    // (these creatures aren't meant to drop standard tier loot; Power/PutridHeart are the
    // real reward path).
    public override void GenerateLoot()
    {
    }

    public override bool IsEnemy(Mobile m)
    {
        if (m is PlayerMobile)
        {
            if (m.Karma <= 1000 && Alignment == Alignment.Good)
            {
                return true;
            }

            if (m.Karma >= 1000 && Alignment == Alignment.Evil)
            {
                return true;
            }
        }
        else if (m is DespiseCreature dc)
        {
            return dc.Alignment != Alignment;
        }

        return false;
    }

    public override bool CanBeRenamedBy(Mobile from) =>
        from.AccessLevel > AccessLevel.Player && base.CanBeRenamedBy(from);

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (ControlMaster != null)
        {
            list.Add(1153303, ControlMaster.Name); // Controller: ~1_NAME~
        }

        list.Add(1153297, $"{_power}\t#{GetPowerLabel(_power)}"); // Power Level: ~1_LEVEL~: ~2_VAL~
    }

    public override void OnCombatantChange()
    {
        base.OnCombatantChange();
        _orb?.InvalidateHue();
    }

    public override void OnKarmaChange(int oldValue)
    {
        if (oldValue < 0 && Karma > 0 || oldValue > 0 && Karma < 0)
        {
            FightMode = Alignment switch
            {
                Alignment.Good => FightMode.Evil,
                Alignment.Evil => FightMode.Good,
                _              => FightMode.Aggressor
            };
        }
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (_orb != null)
        {
            Unlink(false);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (_orb != null && !_orb.Deleted)
        {
            _orb.Pet = null;
        }
    }

    public int GetLeashLength()
    {
        if (_orb == null)
        {
            return RangePerception;
        }

        return _orb.LeashLength switch
        {
            LeashLength.Long => LongLeashLength,
            _                => ShortLeashLength
        };
    }

    public void Link(WispOrb orb)
    {
        _orb = orb;
        RangeHome = 2;
        _orb.InvalidateHue();
    }

    public void Unlink(bool message = true)
    {
        RangeHome = 10;
        SetControlMaster(null);

        if (Alive && message && _orb?.Owner != null)
        {
            _orb.Owner.SendLocalizedMessage(1153335, Name); // You have released control of ~1_NAME~.
            NonlocalOverheadMessage(MessageType.Regular, 0x59, 1153296, Name); // * This creature is no longer influenced by a Wisp Orb *
        }

        if (_orb != null)
        {
            _orb.Conscripted = false;
            _orb.OnUnlinkPet();
            _orb.InvalidateHue();
            _orb = null;
        }
    }

    /// <summary>Was the Power property's setter — raises stats/skills toward the new
    /// power's threshold, same formulas as the original, just invoked explicitly instead of
    /// on assignment (see class doc comment for why).</summary>
    public void GainPower(int newPower)
    {
        var old = _power;

        if (newPower > _maxPower)
        {
            newPower = _maxPower;
        }

        if (old < newPower)
        {
            _power = newPower;
            IncreasePower();
            InvalidateProperties();
        }

        _orb?.InvalidateProperties();
    }

    /// <summary>Was the Progress property's setter — accumulates toward the next power
    /// threshold, calling GainPower/IncreaseResists once it's crossed.</summary>
    public void AddProgress(int amount)
    {
        _progress += amount;

        if (_progress >= _power)
        {
            GainPower(_power + 1);
            IncreaseResists();
            _progress = 0;
        }

        _orb?.InvalidateProperties();
    }

    public virtual void IncreasePower()
    {
        foreach (var skill in Skills)
        {
            if (skill is { Base: > 0 } && skill.Base < SkillMax)
            {
                var toRaise = SkillMax / _maxPower * _power + Utility.RandomMinMax(-5, 5);

                if (toRaise > skill.Base)
                {
                    skill.Base = Math.Min(SkillMax, toRaise);
                }
            }
        }

        var strRaise = StrMax / 15 * _power + Utility.RandomMinMax(-5, 5);
        var dexRaise = DexMax / 15 * _power + Utility.RandomMinMax(-5, 5);
        var intRaise = IntMax / 15 * _power + Utility.RandomMinMax(-5, 5);

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

        var hitsRaise = MaxHits / 15 * _power + Utility.RandomMinMax(-5, 5);
        var stamRaise = MaxStam / 15 * _power + Utility.RandomMinMax(-5, 5);
        var manaRaise = MaxMana / 15 * _power + Utility.RandomMinMax(-5, 5);

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

        FixedEffect(0x373A, 10, 30);
        PlaySound(0x209);
    }

    private void IncreaseResists()
    {
        SetResistance(ResistanceType.Physical, Math.Min(80, PhysicalResistanceSeed + Utility.RandomMinMax(5, 15)));
        SetResistance(ResistanceType.Fire, Math.Min(80, FireResistSeed + Utility.RandomMinMax(5, 15)));
        SetResistance(ResistanceType.Cold, Math.Min(80, ColdResistSeed + Utility.RandomMinMax(5, 15)));
        SetResistance(ResistanceType.Poison, Math.Min(80, PoisonResistSeed + Utility.RandomMinMax(5, 15)));
        SetResistance(ResistanceType.Energy, Math.Min(80, EnergyResistSeed + Utility.RandomMinMax(5, 15)));
    }

    public static int GetPowerLabel(int power) => power switch
    {
        <= 3  => 1153298, // Normal
        <= 6  => 1153299, // Improved
        <= 8  => 1153300, // Heightened
        <= 10 => 1153301, // Magnified
        <= 12 => 1153302, // Amplified
        <= 14 => 1153307, // Inspired
        _     => 1153308  // Galvanized
    };
}
