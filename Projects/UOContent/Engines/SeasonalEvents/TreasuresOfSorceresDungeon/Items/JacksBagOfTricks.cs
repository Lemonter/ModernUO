using System;
using ModernUO.Serialization;
using Server.Events.Halloween;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/JacksBagOfTricks.cs).
///     <see cref="HolidaySettings" /> exists here (Holiday Stuff/Halloween), so the random-treat roll is kept.
///     ServUO's <c>SpikedEggnog</c> is not added: an unrelated <c>SpikedEggNog</c> already exists here and the
///     serialization generator treats the two names as colliding, so the bag hands out that existing item.
/// </summary>
[SerializationGenerator(0, false)]
public partial class JacksBagOfTricks : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextUse;

    public override int LabelNumber => 1157656; // Jack's Bag of Tricks

    [Constructible]
    public JacksBagOfTricks() : base(0x9F85)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (Core.Now.Month != 10 && from.AccessLevel <= AccessLevel.Player)
        {
            from.SendLocalizedMessage(1157632); // This only works during the Month of October.
            return;
        }

        if (_nextUse >= Core.Now)
        {
            from.SendLocalizedMessage(1157633); // You have already used this today.
            return;
        }

        Item item;

        if (Utility.RandomDouble() < 0.1)
        {
            item = HolidaySettings.RandomTreat;
        }
        else
        {
            item = Utility.Random(6) switch
            {
                0 => new RancidReindeerMeat(),
                1 => new GlassyCandyCane(),
                2 => new NamedSeveredElfEars(),
                3 => new SuspiciousGiftBox(),
                4 => new InsultingDoll(),
                _ => new SpikedEggNog()
            };
        }

        if (item == null)
        {
            return;
        }

        if (from.Backpack == null || !from.Backpack.TryDropItem(from, item, false))
        {
            item.MoveToWorld(from.Location, from.Map);
        }

        _nextUse = Core.Now + TimeSpan.FromDays(1);
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/JacksBagOfTricks.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0x1E90, 0x1E91)]
public partial class RancidReindeerMeat : Item
{
    public override int LabelNumber => 1157634; // Rancid Reindeer Meat

    [Constructible]
    public RancidReindeerMeat() : base(0x1E90)
    {
        Hue = 2707;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            m.PrivateOverheadMessage(MessageType.Regular, 0, 1157635, m.NetState); // *It smells terrible!*
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/JacksBagOfTricks.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0x9DC3, 0x9DC4)]
public partial class GlassyCandyCane : Item
{
    public override int LabelNumber => 1157636; // Glass Candy Cane

    [Constructible]
    public GlassyCandyCane() : base(0x9DC3)
    {
        Hue = 1927;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/JacksBagOfTricks.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0x312D, 0x312E)]
public partial class NamedSeveredElfEars : Item
{
    private static readonly string[] _names =
    {
        "Alabaster Snowball", "Pepper Minstix", "Wunorse Openslae", "Sugarplum Mary"
    };

    [SerializableField(0)]
    private string _elfName;

    [Constructible]
    public NamedSeveredElfEars() : base(0x312D)
    {
        _elfName = _names[Utility.Random(_names.Length)];
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1157637, _elfName ?? string.Empty); // The Severed Ears of an Elf Named ~1_NAME~
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            // *You hear the faint jingle of cheery bells...*
            m.PrivateOverheadMessage(MessageType.Regular, 0, 1157638, m.NetState);
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/JacksBagOfTricks.cs).</summary>
[SerializationGenerator(0, false)]
public partial class SuspiciousGiftBox : Container
{
    public override int LabelNumber => 1157639; // Suspicious Gift Box

    [Constructible]
    public SuspiciousGiftBox() : base(0x232A)
    {
        Hue = 1976;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            m.PrivateOverheadMessage(MessageType.Regular, 0, 1157640, m.NetState); // Uh oh...
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/JacksBagOfTricks.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0x48E2, 0x48E3)]
public partial class InsultingDoll : Item
{
    public override int LabelNumber => 1157641; // Insulting Doll

    [Constructible]
    public InsultingDoll() : base(0x48E2)
    {
        Hue = 1933;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (IsChildOf(m.Backpack))
        {
            m.PrivateOverheadMessage(MessageType.Regular, 0, Utility.RandomMinMax(1157642, 1157646), m.NetState);
        }
    }
}



