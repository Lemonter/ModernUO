using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Engines.Despise;

/// <summary>
///     Ported from ServUO's Despise Revamped dungeon (Scripts/Mobiles/Normal/
///     DespiseGoodCreatures.cs) — the 8 good-aligned wildlife (FightMode.Evil, i.e. they
///     fight the "evil"-karma side). See DespiseEvilCreatures.cs's class doc comment for the
///     GainPower/GetWeaponAbility/dropped-MagicalAbility conversion notes — same rules apply
///     here (Silenii is this file's Phantom-equivalent: its GetBardTarget/CanDoTarget/
///     SetMagicalAbility hook is dropped, same reasoning).
/// </summary>
[SerializationGenerator(0, false)]
public partial class Silenii : DespiseCreature
{
    [Constructible]
    public Silenii() : this(1)
    {
    }

    [Constructible]
    public Silenii(int powerLevel) : base(AIType.AI_Melee, FightMode.Evil)
    {
        Body = 0x10F;
        BaseSoundID = 0x585;

        Fame = GetFame;
        Karma = GetKarmaGood;

        GainPower(powerLevel);
    }

    public override string DefaultName => "Силении";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override int StrStart => Utility.RandomMinMax(65, 75);
    public override int DexStart => Utility.RandomMinMax(100, 110);
    public override int IntStart => Utility.RandomMinMax(100, 110);
}

[SerializationGenerator(0, false)]
public partial class ForestNymph : DespiseCreature
{
    [Constructible]
    public ForestNymph() : this(1)
    {
    }

    [Constructible]
    public ForestNymph(int powerLevel) : base(AIType.AI_Mage, FightMode.Evil)
    {
        Body = 266;
        BaseSoundID = 0x467;

        Fame = GetFame;
        Karma = GetKarmaGood;
        GainPower(powerLevel);
    }

    public override string DefaultName => "лесная нимфа";

    protected override BaseAI ForcedAI => new DespiseMageAI(this);
    public override int StrStart => Utility.RandomMinMax(65, 80);
    public override int DexStart => Utility.RandomMinMax(70, 80);
    public override int IntStart => Utility.RandomMinMax(110, 150);
}

[SerializationGenerator(0, false)]
public partial class DespiseUnicorn : DespiseCreature
{
    [Constructible]
    public DespiseUnicorn() : this(1)
    {
    }

    [Constructible]
    public DespiseUnicorn(int powerLevel) : base(AIType.AI_Melee, FightMode.Evil)
    {
        Body = 0x7A;
        BaseSoundID = 0x4BC;

        Fame = GetFame;
        Karma = GetKarmaGood;
        GainPower(powerLevel);
    }

    public override string DefaultName => "единорог";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override int StrStart => Utility.RandomMinMax(80, 100);
    public override int DexStart => Utility.RandomMinMax(110, 115);
    public override int IntStart => Utility.RandomMinMax(100, 115);

    public override bool RaiseDamage => true;
    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ArmorIgnore;
}

[SerializationGenerator(0, false)]
[TypeAlias("Server.Engines.Despise.Sagittari")]
public partial class Sagittarri : DespiseCreature
{
    [Constructible]
    public Sagittarri() : this(1)
    {
    }

    [Constructible]
    public Sagittarri(int powerLevel) : base(AIType.AI_Archer, FightMode.Evil)
    {
        Body = 101;
        BaseSoundID = 679;

        SetSkill(SkillName.Archery, SkillStart);

        Fame = GetFame;
        Karma = GetKarmaGood;

        AddItem(new Bow());
        PackItem(new Arrow(Utility.RandomMinMax(5, 10)));
        GainPower(powerLevel);

        RangeFight = 8;
    }

    public override string DefaultName => "Сагиттарри";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override int StrStart => Utility.RandomMinMax(40, 55);
    public override int DexStart => Utility.RandomMinMax(160, 180);
    public override int IntStart => Utility.RandomMinMax(110, 120);

    public override bool RaiseDamage => true;
}

[SerializationGenerator(0, false)]
public partial class Ursadane : DespiseCreature
{
    [Constructible]
    public Ursadane() : this(1)
    {
    }

    [Constructible]
    public Ursadane(int powerLevel) : base(AIType.AI_Melee, FightMode.Evil)
    {
        Body = 212;
        BaseSoundID = 0xA3;

        Fame = GetFame;
        Karma = GetKarmaGood;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Урсадан";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override PackInstinct PackInstinct => PackInstinct.Bear;

    public override bool RaiseDamage => true;

    public override int StrStart => Utility.RandomMinMax(150, 175);
    public override int DexStart => Utility.RandomMinMax(90, 105);
    public override int IntStart => Utility.RandomMinMax(30, 40);

    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.CrushingBlow;
}

[SerializationGenerator(0, false)]
public partial class DivineGuardian : DespiseCreature
{
    [Constructible]
    public DivineGuardian() : this(1)
    {
    }

    [Constructible]
    public DivineGuardian(int powerLevel) : base(AIType.AI_Melee, FightMode.Evil)
    {
        Body = 123;
        BaseSoundID = 0x2F7;

        Fame = GetFame;
        Karma = GetKarmaGood;
        GainPower(powerLevel);
    }

    public override string DefaultName => "божественный страж";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);

    public override bool RaiseDamage => true;

    public override int StrStart => Utility.RandomMinMax(150, 175);
    public override int DexStart => Utility.RandomMinMax(120, 130);
    public override int IntStart => Utility.RandomMinMax(50, 60);

    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ConcussionBlow;
}

[SerializationGenerator(0, false)]
public partial class Dendrite : DespiseCreature
{
    [Constructible]
    public Dendrite() : this(1)
    {
    }

    [Constructible]
    public Dendrite(int powerLevel) : base(AIType.AI_Melee, FightMode.Evil)
    {
        Body = 301;

        Fame = GetFame;
        Karma = GetKarmaGood;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Дендрит";

    public override int GetIdleSound() => 443;
    public override int GetDeathSound() => 31;
    public override int GetAttackSound() => 672;

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);

    public override bool RaiseDamage => true;

    public override int StrStart => Utility.RandomMinMax(100, 120);
    public override int DexStart => Utility.RandomMinMax(140, 155);
    public override int IntStart => Utility.RandomMinMax(30, 50);

    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.DoubleStrike;
}

[SerializationGenerator(0, false)]
public partial class Fairy : DespiseCreature
{
    private DateTime _nextHeal;
    private const double HealThreshold = 0.60;

    public virtual int MinHeal => Math.Max(10, Power * 3);
    public virtual int MaxHeal => Math.Max(25, Power * 5);

    [Constructible]
    public Fairy() : this(1)
    {
    }

    [Constructible]
    public Fairy(int powerLevel) : base(AIType.AI_Melee, FightMode.Evil)
    {
        Body = 0x108;
        BaseSoundID = 0x467;

        Fame = GetFame;
        Karma = GetKarmaGood;

        _nextHeal = DateTime.UtcNow;
        GainPower(powerLevel);
    }

    public override string DefaultName => "фея";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);

    public override int StrStart => Utility.RandomMinMax(85, 100);
    public override int DexStart => Utility.RandomMinMax(110, 125);
    public override int IntStart => Utility.RandomMinMax(130, 150);

    public override void OnThink()
    {
        base.OnThink();

        if (_nextHeal >= DateTime.UtcNow || Map == null || Map == Map.Internal)
        {
            return;
        }

        var eligible = new List<Mobile>();

        foreach (var m in Map.GetMobilesInRange(Location, 8))
        {
            if (m.Alive && m.Hits <= (int)(m.HitsMax * HealThreshold) && CanDoHeal(m))
            {
                eligible.Add(m);
            }
        }

        if (eligible.Count > 0)
        {
            var m = eligible[Utility.Random(eligible.Count)];

            Direction = GetDirectionTo(m);

            SpellHelper.Heal(Utility.RandomMinMax(MinHeal, MaxHeal), m, this);
            m.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
            m.PlaySound(0x202);

            var nextHeal = Utility.RandomMinMax(20 - Power, 30 - Power);
            _nextHeal = DateTime.UtcNow + TimeSpan.FromSeconds(nextHeal);
            return;
        }

        _nextHeal = DateTime.UtcNow + TimeSpan.FromSeconds(5);
    }

    private bool CanDoHeal(Mobile toHeal)
    {
        if (toHeal is DespiseCreature dc)
        {
            return dc.Alignment == Alignment;
        }

        return toHeal.Karma < 0 && Alignment == Alignment.Evil || toHeal.Karma > 0 && Alignment == Alignment.Good;
    }
}
