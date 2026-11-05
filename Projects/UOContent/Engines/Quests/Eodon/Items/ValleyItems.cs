using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     Quest and reward items of the "Valley of One" Eodon line, ported from ServUO
///     (Scripts/Quests/Eodon/Valley of One Quest/Items.cs).
///
///     Dropped: <c>BaseQuestItem</c> (ServUO's quest-item base with a lifespan) — MLQuest items are plain items here
///     and are removed by the collect/deliver objectives instead of expiring.
///     Substituted: <c>PetTigerCubStatuette</c> uses <see cref="MonsterStatuetteType.BlackCat" />, because this
///     codebase's <see cref="MonsterStatuetteType" /> has no tiger cub entry.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KingBlackthornOrders : Item
{
    [Constructible]
    public KingBlackthornOrders() : base(8792)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1156516; // Orders from King Blackthorn to Sir Geoffrey
}

[SerializationGenerator(0, false)]
public partial class MosaicOfHeluzz : Item
{
    [Constructible]
    public MosaicOfHeluzz() : base(39638) => Hue = 2952;

    public override int LabelNumber => 1156467; // Mosaic of Heluzz
}

[SerializationGenerator(0, false)]
public partial class TotemOfFabozz : Item
{
    [Constructible]
    public TotemOfFabozz() : base(40092) => Hue = 2576;

    public override int LabelNumber => 1156468; // Totem Of Faboz
}

[SerializationGenerator(0, false)]
public partial class FiresOfKukuzz : Item
{
    [Constructible]
    public FiresOfKukuzz() : base(40014)
    {
    }

    public override int LabelNumber => 1156469; // Fires of Kukuzz
}

[SerializationGenerator(0, false)]
public partial class SkullOfMotazz : Item
{
    [Constructible]
    public SkullOfMotazz() : base(40051) => Hue = 2500;

    public override int LabelNumber => 1156470; // The Skull of Motazz
}

[SerializationGenerator(0, false)]
public partial class SkullOfAphazz : Item
{
    [Constructible]
    public SkullOfAphazz() : base(8707)
    {
    }

    public override int LabelNumber => 1156471; // The Skull of Aphazz
}

[SerializationGenerator(0, false)]
public partial class LavaRockDisplay : Item
{
    [Constructible]
    public LavaRockDisplay() : base(40009) => Weight = 5.0;

    public override int LabelNumber => 1124033; // lava rock display
}

[SerializationGenerator(0, false)]
public partial class UniqueTreasureBag : Pouch
{
    [Constructible]
    public UniqueTreasureBag()
    {
        Hue = Utility.RandomNondyedHue();

        switch (Utility.Random(6))
        {
            case 0:
                DropItem(new LavaRockDisplay());
                break;
            case 1:
                var random = Utility.RandomDouble();

                if (random <= 0.02)
                {
                    DropItem(new TigerRugAddonDeed(RugHue.White));
                }
                else if (random < 0.15)
                {
                    DropItem(new TigerRugAddonDeed(RugHue.Black));
                }
                else
                {
                    DropItem(new TigerRugAddonDeed(RugHue.Regular));
                }

                break;
            case 2:
                DropItem(new BananaHoardAddonDeed());
                break;
            case 3:
                DropItem(new DragonTurtleFountainAddonDeed());
                break;
            case 4:
                DropItem(new DinosaurHunterRewardTitleDeed());
                break;
            case 5:
                DropItem(new PetTigerCubStatuette());
                break;
        }
    }

    public override int LabelNumber => 1156581; // A bag with a unique treasure
}

public enum RugHue
{
    Regular,
    White,
    Black
}

[SerializationGenerator(0, false)]
public partial class TigerRugAddon : BaseAddon
{
    [SerializableField(0)]
    private int _rugHueType;

    [Constructible]
    public TigerRugAddon() : this(RugHue.Regular, true)
    {
    }

    public TigerRugAddon(RugHue type, bool south)
    {
        RugType = type;

        var hue = 0;

        var cliloc = type switch
        {
            RugHue.White => 1156483,
            RugHue.Black => 1156482,
            _            => 1156481
        };

        if (type == RugHue.White)
        {
            hue = 2500;
        }
        else if (type == RugHue.Black)
        {
            hue = 1175;
        }

        if (south)
        {
            AddComponent(new LocalizedAddonComponent(40057, cliloc), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(40058, cliloc), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(40059, cliloc), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(40060, cliloc), -1, -1, 0);
            AddComponent(new LocalizedAddonComponent(40061, cliloc), 0, -2, 0);
            AddComponent(new LocalizedAddonComponent(40062, cliloc), -1, -2, 0);
        }
        else
        {
            AddComponent(new LocalizedAddonComponent(40051, cliloc), 0, 0, 0);
            AddComponent(new LocalizedAddonComponent(40052, cliloc), 0, -1, 0);
            AddComponent(new LocalizedAddonComponent(40053, cliloc), -1, 0, 0);
            AddComponent(new LocalizedAddonComponent(40054, cliloc), -1, -1, 0);
            AddComponent(new LocalizedAddonComponent(40055, cliloc), -2, 0, 0);
            AddComponent(new LocalizedAddonComponent(40056, cliloc), -2, -1, 0);
        }

        Hue = hue;
    }

    public override BaseAddonDeed Deed => new TigerRugAddonDeed(RugType);

    public RugHue RugType
    {
        get => (RugHue)_rugHueType;
        set => _rugHueType = (int)value;
    }
}

[SerializationGenerator(0, false)]
public partial class TigerRugAddonDeed : BaseAddonDeed
{
    [SerializableField(0)]
    private int _rugHueType;

    [Constructible]
    public TigerRugAddonDeed() : this(RugHue.Regular)
    {
    }

    [Constructible]
    public TigerRugAddonDeed(RugHue type) => RugType = type;

    public override BaseAddon Addon => new TigerRugAddon(RugType, SouthFacing);

    public RugHue RugType
    {
        get => (RugHue)_rugHueType;
        set => _rugHueType = (int)value;
    }

    public bool SouthFacing { get; set; }

    public override int LabelNumber => RugType switch
    {
        RugHue.White => 1156483,
        RugHue.Black => 1156482,
        _            => 1156481
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(
                new SouthEastGump(
                    south =>
                    {
                        SouthFacing = south;
                        base.OnDoubleClick(from);
                    }
                )
            );
        }
    }
}

public enum BananaHoardSize
{
    Small,
    Medium,
    Large
}

[SerializationGenerator(0, false)]
public partial class BananaHoardAddon : BaseAddon
{
    [SerializableField(0)]
    private int _sizeValue;

    [Constructible]
    public BananaHoardAddon()
    {
    }

    public BananaHoardAddon(BananaHoardSize size)
    {
        BananaHoardSize = size;

        switch (size)
        {
            case BananaHoardSize.Small:
                AddComponent(new LocalizedAddonComponent(40047, 1156484), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(40046, 1156484), -1, 0, 0);
                AddComponent(new LocalizedAddonComponent(40049, 1156484), 0, -1, 0);
                AddComponent(new LocalizedAddonComponent(40050, 1156484), -1, -1, 0);
                break;
            case BananaHoardSize.Medium:
                AddComponent(new LocalizedAddonComponent(40043, 1156485), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(40044, 1156485), 0, -1, 0);
                AddComponent(new LocalizedAddonComponent(40048, 1156485), -1, 0, 0);
                AddComponent(new LocalizedAddonComponent(40045, 1156485), -1, -1, 0);
                break;
            case BananaHoardSize.Large:
                AddComponent(new LocalizedAddonComponent(40042, 1156486), 0, 0, 0);
                AddComponent(new LocalizedAddonComponent(40040, 1156486), -1, 0, 0);
                AddComponent(new LocalizedAddonComponent(40041, 1156486), 0, -1, 0);
                break;
        }
    }

    public override BaseAddonDeed Deed => new BananaHoardAddonDeed { BananaHoardSize = BananaHoardSize };

    public BananaHoardSize BananaHoardSize
    {
        get => (BananaHoardSize)_sizeValue;
        set => _sizeValue = (int)value;
    }
}

[SerializationGenerator(0, false)]
public partial class BananaHoardAddonDeed : BaseAddonDeed
{
    [SerializableField(0)]
    private int _sizeValue;

    [Constructible]
    public BananaHoardAddonDeed()
    {
    }

    public override BaseAddon Addon => new BananaHoardAddon(BananaHoardSize);

    public override int LabelNumber => 1156556; // Great Ape's Banana Hoard

    public BananaHoardSize BananaHoardSize
    {
        get => (BananaHoardSize)_sizeValue;
        set => _sizeValue = (int)value;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(
                new SizeGump(
                    size =>
                    {
                        BananaHoardSize = size;
                        base.OnDoubleClick(from);
                    }
                )
            );
        }
    }

    private class SizeGump : Gump
    {
        private readonly Action<BananaHoardSize> _callback;

        public SizeGump(Action<BananaHoardSize> callback) : base(50, 50)
        {
            _callback = callback;

            AddBackground(0, 0, 200, 200, 5054);
            AddBackground(10, 10, 180, 180, 3000);

            AddHtml(55, 50, 150, 16, "Небольшая", false, false);
            AddHtml(55, 80, 150, 16, "Средняя", false, false);
            AddHtml(55, 110, 150, 16, "Огромная", false, false);

            for (var i = 0; i < 3; i++)
            {
                AddButton(20, 50 + i * 30, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID > 0)
            {
                _callback((BananaHoardSize)(info.ButtonID - 1));
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class DragonTurtleFountainAddon : BaseAddon
{
    [SerializableField(0)]
    private bool _south;

    [Constructible]
    public DragonTurtleFountainAddon() : this(true)
    {
    }

    public DragonTurtleFountainAddon(bool south)
    {
        _south = south;

        if (south)
        {
            AddComponent(new AddonComponent(40087), 0, 0, 0);
            AddComponent(new AddonComponent(40089), 0, -1, 0);
            AddComponent(new AddonComponent(40088), 1, -1, 0);
            AddComponent(new AddonComponent(40090), 1, -2, 0);
        }
        else
        {
            AddComponent(new AddonComponent(40092), 0, 0, 0);
            AddComponent(new AddonComponent(40093), -1, 0, 0);
            AddComponent(new AddonComponent(40094), -1, 1, 0);
            AddComponent(new AddonComponent(40096), -2, 1, 0);
        }
    }

    public override BaseAddonDeed Deed => new DragonTurtleFountainAddonDeed { SouthFacing = _south };
}

[SerializationGenerator(0, false)]
public partial class DragonTurtleFountainAddonDeed : BaseAddonDeed
{
    [Constructible]
    public DragonTurtleFountainAddonDeed()
    {
    }

    public override BaseAddon Addon => new DragonTurtleFountainAddon(SouthFacing);

    public override int LabelNumber => 1156488; // Dragon Turtle Fountain

    public bool SouthFacing { get; set; }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendGump(
                new SouthEastGump(
                    south =>
                    {
                        SouthFacing = south;
                        base.OnDoubleClick(from);
                    }
                )
            );
        }
    }
}

[SerializationGenerator(0, false)]
public partial class DinosaurHunterRewardTitleDeed : BaseRewardTitleDeed
{
    [Constructible]
    public DinosaurHunterRewardTitleDeed()
    {
    }

    public override TextDefinition Title => "Охотник на динозавров";
}

[SerializationGenerator(0, false)]
public partial class PetTigerCubStatuette : MonsterStatuette
{
    [SerializableField(0)]
    private bool _usedTigerCub;

    [Constructible]
    public PetTigerCubStatuette() : base(MonsterStatuetteType.BlackCat) => IsRewardItem = false;

    public bool HasUsed
    {
        get => _usedTigerCub;
        set => _usedTigerCub = value;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (HasUsed || !IsLockedDown || from.Followers == from.FollowersMax)
        {
            base.OnDoubleClick(from);
            return;
        }

        var cub = new TigerCub();
        var p = from.Location;

        var x = p.X;
        var y = p.Y;

        Server.Movement.Movement.Offset(from.Direction, ref x, ref y);
        var z = from.Map.GetAverageZ(x, y);

        if (from.Map.CanSpawnMobile(x, y, z))
        {
            p = new Point3D(x, y, z);
        }

        cub.MoveToWorld(p, from.Map);
        cub.SetControlMaster(from);
        cub.IsBonded = true;

        HasUsed = true;
    }
}
