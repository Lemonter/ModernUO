using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).
///     ServUO's <c>[FlipableAddon]</c> attribute and its <c>IFlipable</c> interface do not exist here, so the addon
///     keeps its south-facing art and the <c>Flip</c> helper only changes the components. <c>AddNameProperty</c>
///     becomes a <c>GetProperties</c> override, and ServUO's <c>ApplySkillBonusGump</c> is replaced with the local
///     <see cref="SkillBonusGump" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SkeletalHangmanAddon : BaseAddon
{
    [SerializableField(0)]
    private Mobile _clicker;

    [Constructible]
    public SkeletalHangmanAddon() : this(null)
    {
    }

    [Constructible]
    public SkeletalHangmanAddon(Mobile clicker)
    {
        _clicker = clicker;

        AddComponent(new AddonComponent(0x9D39), 0, 0, 0);
        AddComponent(new InternalComponent(0x9D38), 0, 1, 0);
    }

    public override BaseAddonDeed Deed => new SkeletalHangmanAddonDeed();

    public virtual void Flip(Mobile from, Direction direction)
    {
        switch (direction)
        {
            case Direction.South:
                AddComponent(new AddonComponent(0x9D39), 0, 0, 0);
                AddComponent(new InternalComponent(0x9D38), 0, 1, 0);
                break;
            case Direction.East:
                AddComponent(new AddonComponent(0x9D3B), 0, 0, 0);
                AddComponent(new InternalComponent(0x9D3A), 1, 0, 0);
                break;
        }
    }

    [SerializationGenerator(0, false)]
    public partial class InternalComponent : AddonComponent
    {
        public override bool ForceShowProperties => true;

        [Constructible]
        public InternalComponent(int id) : base(id)
        {
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Addon is SkeletalHangmanAddon { _clicker: not null } addon)
            {
                list.Add(1156983, addon._clicker.Name);
            }
            else
            {
                list.Add(1156982);
            }
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class SkeletalHangmanAddonDeed : BaseAddonDeed
{
    private Mobile _clicker;

    public override int LabelNumber => 1156982; // Skeletal Hangman

    public override BaseAddon Addon => new SkeletalHangmanAddon(_clicker);

    [Constructible]
    public SkeletalHangmanAddonDeed()
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        _clicker = m;

        base.OnDoubleClick(m);
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class KotlSacraficialAltarAddon : BaseAddon
{
    [Constructible]
    public KotlSacraficialAltarAddon()
    {
        AddComponent(new AddonComponent(0x9D5F), 0, 0, 0);
        AddComponent(new AddonComponent(0x9D60), 0, 1, 0);
        AddComponent(new AddonComponent(0x9D61), 1, 0, 0);
        AddComponent(new AddonComponent(0x9D62), 1, 1, 0);
    }

    public override BaseAddonDeed Deed => new KotlSacraficialAltarAddonDeed();

    public virtual void Flip(Mobile from, Direction direction)
    {
        switch (direction)
        {
            case Direction.East:
                AddComponent(new AddonComponent(0x9D5F), 0, 0, 0);
                AddComponent(new AddonComponent(0x9D60), 0, 1, 0);
                AddComponent(new AddonComponent(0x9D61), 1, 0, 0);
                AddComponent(new AddonComponent(0x9D62), 1, 1, 0);
                break;
            case Direction.South:
                AddComponent(new AddonComponent(0x9D63), 0, 0, 0);
                AddComponent(new AddonComponent(0x9D64), 1, 0, 0);
                AddComponent(new AddonComponent(0x9D65), 0, 1, 0);
                AddComponent(new AddonComponent(0x9D66), 1, 1, 0);
                break;
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class KotlSacraficialAltarAddonDeed : BaseAddonDeed
{
    public override BaseAddon Addon => new KotlSacraficialAltarAddon();

    public override int LabelNumber => 1124311; // Kotl Sacrificial Altar

    [Constructible]
    public KotlSacraficialAltarAddonDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class TreasuresOfKotlRewardDeed : BaseRewardTitleDeed
{
    [SerializableField(0)]
    private TextDefinition _titleCliloc;

    public override TextDefinition Title => _titleCliloc;

    [Constructible]
    public TreasuresOfKotlRewardDeed() : this(1156982)
    {
    }

    [Constructible]
    public TreasuresOfKotlRewardDeed(int localization)
    {
        _titleCliloc = localization;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class KatalkotlsRing : SilverRing
{
    public static SkillName[] Skills { get; } =
    {
        SkillName.Necromancy,
        SkillName.Magery,
        SkillName.Bushido,
        SkillName.Chivalry,
        SkillName.Ninjitsu,
        SkillName.Mysticism
    };

    public override int LabelNumber => 1156989;

    [Constructible]
    public KatalkotlsRing()
    {
        Hue = 2591;

        SkillBonuses.SetValues(0, SkillName.MagicResist, 10);
        SkillBonuses.SetValues(1, SkillName.Meditation, 10);
        Attributes.RegenHits = 5;
        Attributes.RegenMana = 3;
        Attributes.SpellDamage = 20;
    }

    public bool HasSkillBonus => SkillBonuses.Skill_3_Value != 0;

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack) && m is PlayerMobile && !HasSkillBonus)
        {
            m.SendGump(new SkillBonusGump(SkillBonuses, Skills, 2, 20));
        }
        else
        {
            base.OnDoubleClick(m);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!HasSkillBonus)
        {
            list.Add(1155609); // Double Click to Set Skill Bonus
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class BootsOfEscaping : ThighBoots
{
    public override int LabelNumber => 1155607; // Boots of Escaping

    [Constructible]
    public BootsOfEscaping()
    {
        Attributes.BonusDex = 4;
        Attributes.RegenStam = 1;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class TalonsOfEscaping : LeatherTalons
{
    public override int LabelNumber => 1155682; // Talons of Escaping

    [Constructible]
    public TalonsOfEscaping()
    {
        Attributes.BonusDex = 4;
        Attributes.RegenStam = 1;
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/Rewards.cs).
///     ServUO's <c>IFlipable</c> does not exist here, so the banner is placed in its north-west art and does not
///     flip.
/// </summary>
[SerializationGenerator(0, false)]
public partial class TribalBanner : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private EodonTribe _tribe;

    public override int LabelNumber => ItemID + 1084024;

    public override bool ForceShowProperties => true;

    public int NorthID => GetTribeID(_tribe);

    public int WestID => GetTribeID(_tribe) + 1;

    [Constructible]
    public TribalBanner() : this(EodonTribe.Jukari)
    {
    }

    [Constructible]
    public TribalBanner(EodonTribe tribe) : base(GetTribeID(tribe))
    {
        _tribe = tribe;
    }

    private void OnTribeChanged()
    {
        var id = GetTribeID(_tribe);

        if (ItemID != id)
        {
            ItemID = id;
        }
    }

    public static int GetTribeID(EodonTribe tribe) => tribe switch
    {
        EodonTribe.Kurak   => 0x9D55,
        EodonTribe.Barako  => 0x9D57,
        EodonTribe.Sakkhra => 0x9D59,
        EodonTribe.Barrab  => 0x9D5B,
        EodonTribe.Urali   => 0x9D5D,
        _                  => 0x9D53
    };
}
