using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Spells;

namespace Server.Engines.Despise;

/// <summary>
///     Ported from ServUO's Despise Revamped dungeon (Scripts/Mobiles/Normal/
///     DespiseEvilCreatures.cs) — the 8 evil-aligned wildlife. They fight the "good"-karma
///     side through FightMode.Good, which this codebase's enum was missing at the time of the
///     port and has since gained (added with the Void Creatures, which need it too).
///     `Power = powerLevel;` in the original constructors is
///     now `GainPower(powerLevel);` (see DespiseCreature's class doc comment). Fixed
///     WeaponAbility assignments (SetWeaponAbility in the original, gated behind a versioned
///     Deserialize check for old saves that don't apply to a fresh port) are now a plain
///     GetWeaponAbility() override.
///
///     Phantom's original had a MagicalAbility.Discordance auto-cast hook via
///     SetMagicalAbility/GetBardTarget — neither exists anywhere in this codebase (grepped,
///     zero hits), so it's dropped; Phantom fights as a plain melee creature like the rest.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Phantom : DespiseCreature
{
    [Constructible]
    public Phantom() : this(1)
    {
    }

    [Constructible]
    public Phantom(int powerLevel) : base(AIType.AI_Melee, FightMode.Good)
    {
        Body = 0xFC;
        BaseSoundID = 0x482;
        Hue = 2671;

        Fame = GetFame;
        Karma = GetKarmaEvil;

        GainPower(powerLevel);
    }

    public override string DefaultName => "фантом";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override int StrStart => Utility.RandomMinMax(65, 75);
    public override int DexStart => Utility.RandomMinMax(100, 110);
    public override int IntStart => Utility.RandomMinMax(100, 110);
}

[SerializationGenerator(0, false)]
public partial class Naba : DespiseCreature
{
    [Constructible]
    public Naba() : this(1)
    {
    }

    [Constructible]
    public Naba(int powerLevel) : base(AIType.AI_Mage, FightMode.Good)
    {
        Body = 0x88;
        BaseSoundID = 639;
        Hue = 2707;

        Fame = GetFame;
        Karma = GetKarmaEvil;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Наба";

    protected override BaseAI ForcedAI => new DespiseMageAI(this);
    public override int StrStart => Utility.RandomMinMax(65, 80);
    public override int DexStart => Utility.RandomMinMax(70, 80);
    public override int IntStart => Utility.RandomMinMax(110, 150);
}

[SerializationGenerator(0, false)]
public partial class Darkmane : DespiseCreature
{
    [Constructible]
    public Darkmane() : this(1)
    {
    }

    [Constructible]
    public Darkmane(int powerLevel) : base(AIType.AI_Melee, FightMode.Good)
    {
        Body = 0xCC;
        Hue = 1910;
        BaseSoundID = 0xA8;

        Fame = GetFame;
        Karma = GetKarmaEvil;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Черногрив";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override int StrStart => Utility.RandomMinMax(80, 100);
    public override int DexStart => Utility.RandomMinMax(110, 115);
    public override int IntStart => Utility.RandomMinMax(100, 115);

    public override bool RaiseDamage => true;
    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ArmorIgnore;
}

[SerializationGenerator(0, false)]
public partial class Skeletrex : DespiseCreature
{
    [Constructible]
    public Skeletrex() : this(1)
    {
    }

    [Constructible]
    public Skeletrex(int powerLevel) : base(AIType.AI_Archer, FightMode.Good)
    {
        Body = 147;
        BaseSoundID = 451;
        Hue = 2075;

        SetSkill(SkillName.Archery, SkillStart);

        Fame = GetFame;
        Karma = GetKarmaEvil;

        AddItem(new Bow());
        PackItem(new Arrow(Utility.RandomMinMax(5, 10)));
        GainPower(powerLevel);
    }

    public override string DefaultName => "Скелетрекс";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override int StrStart => Utility.RandomMinMax(40, 55);
    public override int DexStart => Utility.RandomMinMax(160, 180);
    public override int IntStart => Utility.RandomMinMax(110, 120);

    public override bool RaiseDamage => true;
}

[SerializationGenerator(0, false)]
public partial class Hellion : DespiseCreature
{
    [Constructible]
    public Hellion() : this(1)
    {
    }

    [Constructible]
    public Hellion(int powerLevel) : base(AIType.AI_Melee, FightMode.Good)
    {
        Body = 4;
        BaseSoundID = 0x174;
        Hue = 2671;

        Fame = GetFame;
        Karma = GetKarmaEvil;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Головорез";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);
    public override PackInstinct PackInstinct => PackInstinct.Bear;
    public override int MinDamMax => 15;
    public override int MaxDamMax => 26;
    public override bool RaiseDamage => true;

    public override int StrStart => Utility.RandomMinMax(150, 175);
    public override int DexStart => Utility.RandomMinMax(90, 105);
    public override int IntStart => Utility.RandomMinMax(30, 40);

    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.CrushingBlow;
}

[SerializationGenerator(0, false)]
public partial class Echidnite : DespiseCreature
{
    [Constructible]
    public Echidnite() : this(1)
    {
    }

    [Constructible]
    public Echidnite(int powerLevel) : base(AIType.AI_Melee, FightMode.Good)
    {
        Body = 250;
        BaseSoundID = 0x52A;
        Hue = 2671;

        Fame = GetFame;
        Karma = GetKarmaEvil;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Эхиднит";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);

    public override bool RaiseDamage => true;

    public override int StrStart => Utility.RandomMinMax(150, 175);
    public override int DexStart => Utility.RandomMinMax(120, 130);
    public override int IntStart => Utility.RandomMinMax(50, 60);

    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ConcussionBlow;
}

[SerializationGenerator(0, false)]
[TypeAlias("Server.Engines.Despise.BerlingBlades")]
public partial class BirlingBlades : DespiseCreature
{
    [Constructible]
    public BirlingBlades() : this(1)
    {
    }

    [Constructible]
    public BirlingBlades(int powerLevel) : base(AIType.AI_Melee, FightMode.Good)
    {
        Body = 574;
        BaseSoundID = 224;
        Hue = 2672;

        Fame = GetFame;
        Karma = GetKarmaEvil;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Бирлинг Блейдс";

    protected override BaseAI ForcedAI => new DespiseMeleeAI(this);

    public override bool RaiseDamage => true;

    public override int StrStart => Utility.RandomMinMax(100, 120);
    public override int DexStart => Utility.RandomMinMax(140, 155);
    public override int IntStart => Utility.RandomMinMax(30, 50);

    public override double WeaponAbilityChance => 0.5;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.DoubleStrike;

    public override int GetAngerSound() => 0x23A;
    public override int GetAttackSound() => 0x3B8;
    public override int GetHurtSound() => 0x23A;
}

[SerializationGenerator(0, false)]
public partial class Prometheoid : DespiseCreature
{
    private DateTime _nextHeal;
    private const double HealThreshold = 0.5;

    public virtual int MinHeal => Math.Max(10, Power * 3);
    public virtual int MaxHeal => Math.Max(25, Power * 5);

    [Constructible]
    public Prometheoid() : this(1)
    {
    }

    [Constructible]
    public Prometheoid(int powerLevel) : base(AIType.AI_Melee, FightMode.Good)
    {
        Body = 305;
        BaseSoundID = 224;
        Hue = 2671;

        Fame = GetFame;
        Karma = GetKarmaEvil;

        _nextHeal = DateTime.UtcNow;
        GainPower(powerLevel);
    }

    public override string DefaultName => "Прометеоид";

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
