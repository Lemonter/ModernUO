using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).
///     Dropped: <c>ExtendedWeaponAttributes</c> (HitExplosion) — that attribute block does not exist here. ServUO's
///     <c>BasicInfoGump</c> is replaced with the same cliloc sent in the localised speech of the reader.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DragonCannon : BaseAddon
{
    [Constructible]
    public DragonCannon() : this(DirectionType.South)
    {
    }

    [Constructible]
    public DragonCannon(DirectionType direction)
    {
        switch (direction)
        {
            case DirectionType.North:
                AddComponent(new AddonComponent(0x44F4), 0, 0, 0);
                AddComponent(new AddonComponent(0x44F3), 0, 1, 0);
                AddComponent(new AddonComponent(0x44F5), 0, -1, 0);
                break;
            case DirectionType.West:
                AddComponent(new AddonComponent(0x424A), 0, 0, 0);
                AddComponent(new AddonComponent(0x4223), 1, 0, 0);
                AddComponent(new AddonComponent(0x418F), -1, 0, 0);
                break;
            case DirectionType.East:
                AddComponent(new AddonComponent(0x44F7), 0, 0, 0);
                AddComponent(new AddonComponent(0x44F6), 1, 0, 0);
                AddComponent(new AddonComponent(0x44F8), -1, 0, 0);
                break;
            default:
                AddComponent(new AddonComponent(0x4221), 0, 0, 0);
                AddComponent(new AddonComponent(0x4222), 0, 1, 0);
                AddComponent(new AddonComponent(0x4220), 0, -1, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new DragonCannonDeed();
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class DragonCannonDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private DirectionType _direction;

    public override int LabelNumber => 1158926; // Decorative Dragon Cannon

    public override BaseAddon Addon => new DragonCannon(_direction);

    [Constructible]
    public DragonCannonDeed()
    {
    }

    public void GetOptions(RewardOptionList list)
    {
        list.Add((int)DirectionType.North, 1075389); // North
        list.Add((int)DirectionType.West, 1075390);  // West
        list.Add((int)DirectionType.South, 1075386); // South
        list.Add((int)DirectionType.East, 1075387);  // East
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _direction = (DirectionType)choice;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            RewardOptionGump.DisplayTo(from, this, 1076783);
        }
        else
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0xA2CA, 0xA2CB)]
public partial class ShoulderParrot : BaseOuterTorso
{
    [SerializableField(0)]
    private string _masterName;

    [SerializableField(1)]
    private Mobile _lastShoulder;

    private DateTime _nextFly;
    private DateTime _flyEnd;
    private TimerExecutionToken _flyToken;

    [Constructible]
    public ShoulderParrot() : base(0xA2CA)
    {
        LootType = LootType.Blessed;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_masterName != null)
        {
            var suffix = _masterName.EndsWith('s') || _masterName.EndsWith('z') ? "'" : "'s";
            list.Add(1158958, $"{_masterName}{suffix}");
        }
        else
        {
            list.Add(1158928); // Shoulder Parrot
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m.FindItemOnLayer(Layer.OuterTorso) != this)
        {
            m.SendLocalizedMessage(1158957); // Your parrot can't fly here.
            return;
        }

        if (_nextFly > Core.Now)
        {
            m.SendLocalizedMessage(1158956); // Your parrot is too tired to fly right now.
            return;
        }

        Timer.StartTimer(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500), FlyOnTick, out _flyToken);

        Movable = false;
        _lastShoulder = m;
        MoveToWorld(new Point3D(m.X, m.Y, m.Z + 15), m.Map);
        ItemID = 0xA2CC;

        _flyEnd = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(3, 5));
    }

    private void FlyOnTick()
    {
        if (_flyEnd >= Core.Now || _lastShoulder == null)
        {
            return;
        }

        Movable = true;
        ItemID = 0xA2CA;

        if (_lastShoulder.FindItemOnLayer(Layer.OuterTorso) != null)
        {
            _lastShoulder.Backpack?.DropItem(this);
        }
        else
        {
            _lastShoulder.AddItem(this);
        }

        _lastShoulder = null;
        _flyToken.Cancel();
        _nextFly = Core.Now + TimeSpan.FromMinutes(2);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _flyToken.Cancel();
        _lastShoulder = null;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0xA2C8, 0xA2C9)]
public partial class PirateWallMap : Item
{
    public override int LabelNumber => 1158938; // Pirate Wall Map

    [Constructible]
    public PirateWallMap() : base(0xA2C8)
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        var gump = new Gumps.Gump(50, 50);
        gump.AddImage(0, 0, 0x9CE9);

        m.SendGump(gump);
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0xA2C6, 0xA2C7)]
public partial class MysteriousStatue : Item
{
    public override int LabelNumber => 1158935; // Mysterious Statue

    [Constructible]
    public MysteriousStatue() : base(0xA2C6)
    {
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m.InRange(GetWorldLocation(), 2))
        {
            m.SendLocalizedMessage(1158937);
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0x4C26, 0x4C27)]
public partial class DecorativeWoodCarving : Item
{
    private static readonly string[] _shipNames =
    {
        "Adventure Galley", "Queen Anne's Revenge", "Fancy", "Whydah", "Royal Fortune", "The Black Pearl",
        "Satisfaction", "The Golden Fleece", "Bachelor's Delight", "The Revenge", "The Flying Dragon", "The Gabriel",
        "Privateer's Death", "Kiss of Death", "Devil's Doom", "Monkeebutt", "Mourning Star", "Cursed Sea-Dog",
        "The Howling Lusty Wench", "Scourage of the Seven Seas", "Neptune's Plague", "Sea's Hellish Plague",
        "The Salty Bastard"
    };

    [SerializableField(0)]
    private string _shipName;

    [Constructible]
    public DecorativeWoodCarving() : base(0x4C26)
    {
        Hue = 2968;
    }

    public void AssignRandomName()
    {
        var boats = BaseBoat.Boats;
        var names = new System.Collections.Generic.List<string>();

        foreach (var boat in boats)
        {
            if (!string.IsNullOrEmpty(boat.ShipName))
            {
                names.Add(boat.ShipName);
            }
        }

        _shipName = names.Count > 0 ? names[Utility.Random(names.Count)] : _shipNames[Utility.Random(_shipNames.Length)];
    }

    public override void GetProperties(IPropertyList list)
    {
        if (string.IsNullOrEmpty(_shipName))
        {
            list.Add(1158943); // Wood Carving of [Ship's Name]
        }
        else
        {
            list.Add(1158921, _shipName); // Wood Carving of ~1_name~
        }

        base.GetProperties(list);

        if (string.IsNullOrEmpty(_shipName))
        {
            list.Add(1158953); // Named with a random famous ship...
        }
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class QuartermasterRewardDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158951; // Quartermaster

    [Constructible]
    public QuartermasterRewardDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class SailingMasterRewardDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158950; // Sailing Master

    [Constructible]
    public SailingMasterRewardDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class BotswainRewardDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158949; // Botswain

    [Constructible]
    public BotswainRewardDeed()
    {
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).</summary>
[SerializationGenerator(0, false)]
public partial class PowderMonkeyRewardDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => 1158948; // Powder Monkey

    [Constructible]
    public PowderMonkeyRewardDeed()
    {
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).
///     Dropped: <c>ExtendedWeaponAttributes.HitExplosion</c> (no such attribute block here).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpikedWhipOfPlundering : SpikedWhip
{
    public override int LabelNumber => 1158925; // Spiked Whip of Plundering

    [Constructible]
    public SpikedWhipOfPlundering()
    {
        WeaponAttributes.HitLeechMana = 81;
        Attributes.SpellChanneling = 1;
        Attributes.Luck = 100;
        Attributes.WeaponDamage = 70;
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).
///     Dropped: <c>ExtendedWeaponAttributes.HitExplosion</c> (no such attribute block here).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BladedWhipOfPlundering : BladedWhip
{
    public override int LabelNumber => 1158924; // Bladed Whip of Plundering

    [Constructible]
    public BladedWhipOfPlundering()
    {
        WeaponAttributes.HitLeechMana = 81;
        Attributes.SpellChanneling = 1;
        Attributes.Luck = 100;
        Attributes.WeaponDamage = 70;
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/RisingTide/Rewards.cs).
///     Dropped: <c>ExtendedWeaponAttributes.HitExplosion</c> (no such attribute block here).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BarbedWhipOfPlundering : BarbedWhip
{
    public override int LabelNumber => 1158923; // Barbed Whip of Plundering

    [Constructible]
    public BarbedWhipOfPlundering()
    {
        WeaponAttributes.HitLeechMana = 81;
        Attributes.SpellChanneling = 1;
        Attributes.Luck = 100;
        Attributes.WeaponDamage = 70;
    }
}



