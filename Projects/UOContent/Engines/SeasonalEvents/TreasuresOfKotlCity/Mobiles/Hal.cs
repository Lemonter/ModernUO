using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Mobiles/Hal.cs).
///     ServUO's <c>BaseTurnInMobile</c> does not exist here, so Hal only opens the reward store; points are credited
///     by <see cref="KotlCityData.AwardKill" />. <c>SetWearable</c> becomes <c>AddItem</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Hal : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    private DateTime _nextSpeak;

    [Constructible]
    public Hal() : base("исследователь")
    {
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = "Хэл";

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new FancyShirt { Hue = 1255 });
        AddItem(new JinBaori { Hue = 2722 });
        AddItem(new Kilt { Hue = 2012 });
        AddItem(new ThighBoots { Hue = 1908 });
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1157038); // Artifacts of the Kotl City Trader
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (_nextSpeak >= Core.Now)
        {
            return;
        }

        SayTo(m, 1156903); // Bring me items of the Kotl and I will reward you with valuable items.
        _nextSpeak = Core.Now + TimeSpan.FromSeconds(25);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 5) && pm.CheckAlive())
        {
            pm.SendGump(new KotlCityRewardGump(this, pm));
        }
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Mobiles/SpectralKotlWarrior.cs and
///     SpectralMyrmidexWarrior.cs). ServUO only shipped the <c>(bool weak)</c> constructor; a parameterless
///     <c>[Constructible]</c> one is added here for the serializer and for spawners.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpectralKotlWarrior : BaseCreature
{
    public override string CorpseName => "труп котового воина";

    [Constructible]
    public SpectralKotlWarrior() : this(false)
    {
    }

    [Constructible]
    public SpectralKotlWarrior(bool weak) : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "призрачный котовый воин";
        Body = 0x24;
        BaseSoundID = 417;
        Hue = 2951;

        SetStr(500, 600);
        SetDex(82, 95);
        SetInt(130, 140);
        SetMana(40, 50);

        SetDamage(18, 22);

        if (weak)
        {
            SetHits(1200, 1400);

            SetResistance(ResistanceType.Physical, 1, 10);
            SetResistance(ResistanceType.Fire, 1, 10);
            SetResistance(ResistanceType.Cold, 1, 10);
            SetResistance(ResistanceType.Poison, 8, 10);
            SetResistance(ResistanceType.Energy, 5, 10);
        }
        else
        {
            SetHits(330, 360);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 90, 100);
            SetResistance(ResistanceType.Energy, 80, 90);
        }

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetSkill(SkillName.Wrestling, 90, 100);
        SetSkill(SkillName.Tactics, 90, 100);
        SetSkill(SkillName.MagicResist, 70, 80);
        SetSkill(SkillName.Poisoning, 70, 80);
        SetSkill(SkillName.Magery, 80, 90);
        SetSkill(SkillName.EvalInt, 70, 80);

        Fame = 16000;
        Karma = -16000;
    }

    public override Poison HitPoison => Poison.Deadly;

    public override Poison PoisonImmune => Poison.Deadly;

    public override bool IsEnemy(Mobile m) => m is SpectralMyrmidexWarrior || base.IsEnemy(m);

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Mobiles/SpectralMyrmidexWarrior.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpectralMyrmidexWarrior : BaseCreature
{
    public override string CorpseName => "труп мирмидекса";

    [Constructible]
    public SpectralMyrmidexWarrior() : this(false)
    {
    }

    [Constructible]
    public SpectralMyrmidexWarrior(bool weak) : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Name = "призрачный воин-мирмидекс";
        Body = 1403;
        BaseSoundID = 959;
        Hue = 2951;

        SetStr(500, 600);
        SetDex(82, 95);
        SetInt(130, 140);
        SetMana(40, 50);

        SetDamage(18, 22);

        if (weak)
        {
            SetHits(1200, 1400);

            SetResistance(ResistanceType.Physical, 1, 10);
            SetResistance(ResistanceType.Fire, 1, 10);
            SetResistance(ResistanceType.Cold, 1, 10);
            SetResistance(ResistanceType.Poison, 8, 10);
            SetResistance(ResistanceType.Energy, 5, 10);
        }
        else
        {
            SetHits(330, 360);

            SetResistance(ResistanceType.Physical, 40, 50);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 90, 100);
            SetResistance(ResistanceType.Energy, 80, 90);
        }

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetSkill(SkillName.Wrestling, 90, 100);
        SetSkill(SkillName.Tactics, 90, 100);
        SetSkill(SkillName.MagicResist, 70, 80);
        SetSkill(SkillName.Poisoning, 70, 80);
        SetSkill(SkillName.Magery, 80, 90);
        SetSkill(SkillName.EvalInt, 70, 80);

        Fame = 16000;
        Karma = -16000;
    }

    public override Poison HitPoison => Poison.Deadly;

    public override Poison PoisonImmune => Poison.Deadly;

    public override bool IsEnemy(Mobile m) => m is SpectralKotlWarrior || base.IsEnemy(m);

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}
