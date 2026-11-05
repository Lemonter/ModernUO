using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TeleporterComponent : AddonComponent
{
    [SerializableField(0)]
    private Direction _direction;

    public TeleporterComponent(int itemID, Direction direction) : base(itemID) => _direction = direction;

    public override bool ForceShowProperties => true;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (Addon != null)
        {
            SetSecureLevelEntry.AddTo(from, Addon, ref list);
        }
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is PlayerMobile pm && Addon is SpiralStaircaseAddon staircase && staircase.CheckAccessible(pm, this))
        {
            Timer.StartTimer(TimeSpan.FromSeconds(0.5), () => DoTeleport(m));
        }

        return base.OnMoveOver(m);
    }

    public virtual void DoTeleport(Mobile m)
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            map = m.Map;
        }

        if (Addon is not SpiralStaircaseAddon staircase)
        {
            return;
        }

        TeleporterComponent destination = null;

        foreach (var component in staircase.Components)
        {
            if (component is TeleporterComponent teleporter && teleporter != this)
            {
                destination = teleporter;
                break;
            }
        }

        if (destination == null)
        {
            return;
        }

        var p = new Point3D(
            destination.Location.X,
            destination.Location.Y,
            destination._direction == Direction.Up ? Location.Z + 20 : destination.Location.Z
        );

        BaseCreature.TeleportPets(m, p, map);

        m.MoveToWorld(p, map);
    }
}

[SerializationGenerator(0, false)]
public partial class SpiralStaircaseAddon : BaseAddon, ISecurable, IDyable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public SpiralStaircaseAddon(bool topper = false)
    {
        if (topper)
        {
            AddComponent(new AddonComponent(42319), 0, 1, 0);
            AddComponent(new AddonComponent(42320), 1, 0, 0);
            AddComponent(new AddonComponent(42333), 1, 1, 20);
            AddComponent(new AddonComponent(42334), 0, 1, 20);
            AddComponent(new AddonComponent(42335), 1, 0, 20);
            AddComponent(new TeleporterComponent(42336, Direction.Up), 0, 0, 20);
            AddComponent(new TeleporterComponent(42318, Direction.Down), 1, 1, 0);
        }
        else
        {
            AddComponent(new TeleporterComponent(42321, Direction.Up), 0, 0, 0);
            AddComponent(new AddonComponent(42319), 0, 1, 0);
            AddComponent(new AddonComponent(42320), 1, 0, 0);
            AddComponent(new TeleporterComponent(42318, Direction.Down), 1, 1, 0);
        }
    }

    public override BaseAddonDeed Deed => new SpiralStaircaseDeed();

    public virtual bool Dye(Mobile from, DyeTub sender)
    {
        if (Deleted)
        {
            return false;
        }

        Hue = sender.DyedHue;
        return true;
    }

    public bool CheckAccessible(Mobile from, Item item)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true;
        }

        var house = BaseHouse.FindHouseAt(item);

        if (house == null)
        {
            return false;
        }

        return _level switch
        {
            SecureLevel.Owner    => house.IsOwner(from),
            SecureLevel.CoOwners => house.IsCoOwner(from),
            SecureLevel.Friends  => house.IsFriend(from),
            SecureLevel.Anyone   => true,
            SecureLevel.Guild    => house.IsGuildMember(from),
            _                    => false
        };
    }
}

[SerializationGenerator(0, false)]
public partial class SpiralStaircaseDeed : BaseAddonDeed, IRewardOption
{
    [SerializableField(0)]
    private bool _topper;

    [Constructible]
    public SpiralStaircaseDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new SpiralStaircaseAddon(_topper);
    public override int LabelNumber => 1159429; // spiral staircase

    public void GetOptions(RewardOptionList list)
    {
        list.Add(0, 1159431); // Without Topper
        list.Add(1, 1159432); // With Topper
    }

    public void OnOptionSelected(Mobile from, int choice)
    {
        _topper = choice == 1;

        if (!Deleted)
        {
            base.OnDoubleClick(from);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        // AddonOptionGump в репозитории нет; заголовок «Choose an option:» передан напрямую.
        RewardOptionGump.DisplayTo(from, this, 1159430);
    }
}
