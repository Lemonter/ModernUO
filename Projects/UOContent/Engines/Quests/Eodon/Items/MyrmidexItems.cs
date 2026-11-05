using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests;
using Server.Engines.MLQuests.Definitions;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Eodon;

/// <summary>
///     Quest items, teleporters and reward items of the "Myrmidex Threat" Eodon line, ported from ServUO
///     (Scripts/Quests/Eodon/MyrmidexThreat/Items/).
///
///     Dropped, because this codebase has no hook for them:
///     <list type="bullet">
///         <item><c>TreasureMap.OnMapComplete</c> — the special map no longer seeds the activator, the quest hands it over.</item>
///         <item><c>SOS.OnSOSComplete</c> — likewise for the regulator on the shipwreck message.</item>
///         <item><c>EodonTribeRewardTitleToken</c> — replaced by the already ported Eodon title deeds.</item>
///         <item><c>MonsterStatuetteType.SakkhranBirdOfPrey</c> — replaced by <see cref="MonsterStatuetteType.Gorilla" />.</item>
///         <item><c>MoonstoneCrystal</c>'s <c>ISecurable</c> house storage — the crystal still gates on being locked down,
///         but has no secure-level context menu.</item>
///     </list>
/// </summary>
[SerializationGenerator(0, false)]
public partial class HiddenTreasuresTreasureMap : TreasureMap
{
    [Constructible]
    public HiddenTreasuresTreasureMap() : this(2, GetMap())
    {
    }

    [Constructible]
    public HiddenTreasuresTreasureMap(int level, Map map) : base(level, map)
    {
    }

    private static Map GetMap() => Utility.Random(6) switch
    {
        1       => Map.Malas,
        2       => Map.Tokuno,
        3       => Map.TerMur,
        4       => Map.Trammel,
        5       => Map.Felucca,
        _       => Map.Ilshenar
    };

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1156811); // *A Special Map Thought to Lead to Lost Kotl Technology*
    }
}

[SerializationGenerator(0, false)]
public partial class SaltySeaMIB : MessageInABottle
{
    [SerializableField(0)]
    private int _saltyLevel;

    [Constructible]
    public SaltySeaMIB() : this(Utility.RandomBool() ? Map.Trammel : Map.Felucca, 3)
    {
    }

    [Constructible]
    public SaltySeaMIB(Map map, int level) : base(map, level) => _saltyLevel = level;

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
        {
            from.SendLocalizedMessage(1019045); // I can't reach that.
            return;
        }

        var sos = new SaltySeaSOS(TargetMap, _saltyLevel);
        var parent = Parent as Container;

        Delete();

        if (parent != null)
        {
            parent.AddItem(sos);
        }
        else
        {
            sos.MoveToWorld(Location, Map);
        }

        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501891); // You extract the message from the bottle.
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1156806); // *Coordinates to a Shipwreck thought to have Lost Kotl Technology*
    }
}

[SerializationGenerator(0, false)]
public partial class SaltySeaSOS : SOS
{
    [Constructible]
    public SaltySeaSOS() : this(Map.Trammel, 3)
    {
    }

    [Constructible]
    public SaltySeaSOS(Map map, int level) : base(map, level)
    {
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1156806); // *Coordinates to a Shipwreck thought to have Lost Kotl Technology*
    }
}

[SerializationGenerator(0, false)]
public partial class MyrmidexIdol : Item
{
    private static readonly Point3D Destination = new(896, 2304, 45);

    [Constructible]
    public MyrmidexIdol() : base(39453) => Movable = false;

    public override int LabelNumber => 1156823; // Idol of Zipactriotl

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile pm)
        {
            return;
        }

        var doingQuest = MLQuestSystem.GetContext(pm)?.IsDoingQuest(typeof(DestructionOfZipactriotlQuest)) == true;

        if (!doingQuest && from.AccessLevel <= AccessLevel.Player)
        {
            // You must be on the "Destruction of Zipactriotl" quest to enter. Visit the Barrabian Tinker...
            from.SendLocalizedMessage(1156841);
            return;
        }

        BaseCreature.TeleportPets(from, Destination, Map.TerMur);
        from.MoveToWorld(Destination, Map.TerMur);

        Effects.SendLocationParticles(
            EffectItem.Create(from.Location, Map.TerMur, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            2023
        );
        Effects.SendLocationParticles(
            EffectItem.Create(Destination, Map.TerMur, EffectItem.DefaultDuration),
            0x3728,
            10,
            10,
            5023
        );

        from.PlaySound(0x1FE);
    }
}

[SerializationGenerator(0, false)]
public partial class StasisChamberActivator : Item
{
    [Constructible]
    public StasisChamberActivator() : base(40158)
    {
    }

    public override int LabelNumber => 1156624;
}

[SerializationGenerator(0, false)]
public partial class StasisChamberRegulator : Item
{
    [Constructible]
    public StasisChamberRegulator() : base(40157)
    {
    }

    public override int LabelNumber => 1156626;
}

[SerializationGenerator(0, false)]
public partial class StasisChamberStator : Item
{
    [Constructible]
    public StasisChamberStator() : base(40156)
    {
    }

    public override int LabelNumber => 1156628;
}

[SerializationGenerator(0, false)]
public partial class BottledMyrmidexPheromone : Item
{
    [Constructible]
    public BottledMyrmidexPheromone() : base(3836)
    {
    }

    public override int LabelNumber => 1156620;
}

[SerializationGenerator(0, false)]
public partial class BottleOfConcentratedInsecticide : Item
{
    [Constructible]
    public BottleOfConcentratedInsecticide() : base(22344)
    {
    }

    public override int LabelNumber => 1156617;
}

[SerializationGenerator(0, false)]
public partial class MyrmidexPopulationReport : Item
{
    [Constructible]
    public MyrmidexPopulationReport() : base(18098) => Hue = 467;

    public override int LabelNumber => 1156775;
}

[SerializationGenerator(0, false)]
public partial class KotlPowerCore : Item
{
    [Constructible]
    public KotlPowerCore() : base(40147)
    {
    }

    public override int LabelNumber => 1124179; // Kotl Power Core
}

[SerializationGenerator(0, false)]
public partial class UnabridgedAtlasOfEodon : Item
{
    [Constructible]
    public UnabridgedAtlasOfEodon() : base(7185) => Hue = 2007;

    public override int LabelNumber => 1156721; // Unabridged Atlas of Eodon

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        from.SendGump(new AtlasGump());
    }

    private class AtlasGump : Gump
    {
        public AtlasGump() : base(100, 50)
        {
            AddImage(0, 0, 30236);
            AddHtmlLocalized(115, 35, 350, 600, 1156723, 1, false, true);
        }
    }
}

[Flippable(40253, 40252)]
[SerializationGenerator(0, false)]
public partial class EodonianWallMap : Item
{
    [Constructible]
    public EodonianWallMap() : base(40253)
    {
    }

    public override int LabelNumber => 1156690; // Wall Map of Eodon

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 5))
        {
            from.SendGump(new WallMapGump());
        }
    }

    private class WallMapGump : Gump
    {
        public WallMapGump() : base(0, 0) => AddImage(0, 0, 49999);
    }
}

[SerializationGenerator(0, false)]
public partial class MoonstoneCrystal : Item
{
    private static readonly (int Label, Point3D Point)[] Locations =
    {
        (1156705, new Point3D(715, 1866, 40)), // Eodon Moongate
        (1156706, new Point3D(642, 1721, 40)), // Barako Village
        (1156707, new Point3D(701, 2106, 40)), // Jukari Village
        (1156708, new Point3D(355, 1873, 0)),  // Kurak Village
        (1156709, new Point3D(552, 1471, 40)), // Sakkhra Village
        (1156710, new Point3D(412, 1595, 40)), // Urali Village
        (1156711, new Point3D(167, 1800, 80)), // Barrab Village
        (1156712, new Point3D(929, 2016, 50)), // Shadowguard
        (1156713, new Point3D(731, 1603, 40)), // The great ape cave
        (1156714, new Point3D(878, 2105, 40)), // The Volcano
        (1156715, new Point3D(390, 1690, 40)), // Dragon Turtle Habitat
        (1156716, new Point3D(269, 1726, 80))  // Britannian Encampment
    };

    [Constructible]
    public MoonstoneCrystal() : base(0x9CBB) => Weight = 10;

    public override int LabelNumber => 1124143; // Moonstone Crystal

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsLockedDown && !IsSecure)
        {
            from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500295); // You are far away.
            return;
        }

        from.SendGump(new CrystalGump(this, from as PlayerMobile));
    }

    private class CrystalGump : Gump
    {
        private readonly MoonstoneCrystal _crystal;
        private readonly PlayerMobile _user;

        public CrystalGump(MoonstoneCrystal crystal, PlayerMobile user) : base(75, 75)
        {
            _crystal = crystal;
            _user = user;

            AddBackground(0, 0, 400, 400, 9270);
            AddHtmlLocalized(0, 15, 400, 16, 1154645, "#1156704", 0xFFFF, false, false); // Select your destination:

            for (var i = 0; i < Locations.Length; i++)
            {
                AddHtmlLocalized(60, 45 + i * 25, 250, 16, Locations[i].Label, 0xFFFF, false, false);
                AddButton(20, 50 + i * 25, 2117, 2118, i + 1, GumpButtonType.Reply, 0);
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID <= 0 || info.ButtonID > Locations.Length)
            {
                return;
            }

            if (_crystal == null || _crystal.Deleted || _user == null)
            {
                return;
            }

            var destination = Locations[info.ButtonID - 1].Point;

            if (_user.InRange(destination, 1) && _user.Map == Map.TerMur)
            {
                _user.SendLocalizedMessage(1019003); // You are already there.
                return;
            }

            if (!_user.InRange(_crystal.GetWorldLocation(), 2) || _user.Map != _crystal.Map)
            {
                _user.SendLocalizedMessage(500295); // You are far away.
                return;
            }

            if (_user.Murderer || _user.Criminal)
            {
                _user.SendLocalizedMessage(1019004); // You are not allowed to travel there.
                return;
            }

            BaseCreature.TeleportPets(_user, destination, Map.TerMur);
            _user.Combatant = null;
            _user.Warmode = false;
            _user.Hidden = true;
            _user.MoveToWorld(destination, Map.TerMur);

            Effects.PlaySound(destination, Map.TerMur, 0x1FE);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class MyrmidexRewardBag : Backpack
{
    [Constructible]
    public MyrmidexRewardBag()
    {
        Hue = Utility.RandomNondyedHue();

        switch (Utility.Random(4))
        {
            case 0:
                DropItem(new RecipeScroll(Utility.RandomMinMax(900, 905)));
                break;
            case 1:
                DropItem(new DefenderOfTheMyrmidexTitleDeed());
                break;
            case 2:
                DropItem(new RecipeScroll(455));
                break;
            case 3:
                DropItem(new MoonstoneCrystal());
                break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class EodonianRewardBag : Backpack
{
    [Constructible]
    public EodonianRewardBag()
    {
        Hue = Utility.RandomNondyedHue();

        switch (Utility.Random(4))
        {
            case 0:
                DropItem(new MonsterStatuette(MonsterStatuetteType.Gorilla));
                break;
            case 1:
                DropItem(new DefenderOfEodonTitleDeed());
                break;
            case 2:
                DropItem(new RecipeScroll(1000));
                break;
            case 3:
                DropItem(Utility.RandomDouble() < 0.5 ? new RawMoonstoneLargeAddonDeed() : new RawMoonstoneSmallAddonDeed());
                break;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class RawMoonstoneLargeAddon : BaseAddon
{
    [Constructible]
    public RawMoonstoneLargeAddon() => AddComponent(new LocalizedAddonComponent(40129, 1124130), 0, 0, 0);

    public override BaseAddonDeed Deed => new RawMoonstoneLargeAddonDeed();
}

[SerializationGenerator(0, false)]
public partial class RawMoonstoneLargeAddonDeed : BaseAddonDeed
{
    [Constructible]
    public RawMoonstoneLargeAddonDeed()
    {
    }

    public override BaseAddon Addon => new RawMoonstoneLargeAddon();

    public override int LabelNumber => 1156703;
}

[SerializationGenerator(0, false)]
public partial class RawMoonstoneSmallAddon : BaseAddon
{
    [Constructible]
    public RawMoonstoneSmallAddon() => AddComponent(new LocalizedAddonComponent(40136, 1124130), 0, 0, 0);

    public override BaseAddonDeed Deed => new RawMoonstoneSmallAddonDeed();
}

[SerializationGenerator(0, false)]
public partial class RawMoonstoneSmallAddonDeed : BaseAddonDeed
{
    [Constructible]
    public RawMoonstoneSmallAddonDeed()
    {
    }

    public override BaseAddon Addon => new RawMoonstoneSmallAddon();

    public override int LabelNumber => 1156702;
}
