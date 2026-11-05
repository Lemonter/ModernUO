using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[Furniture]
[Flippable(0xA50F, 0xA510)]
[SerializationGenerator(0, false)]
public partial class CowStatue : Item, ISecurable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextResourceCount;

    [Constructible]
    public CowStatue() : base(0xA50F)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
        _nextResourceCount = Core.Now + TimeSpan.FromDays(1);
    }

    public override int LabelNumber => 1072494; // Cow

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public bool CheckAccessible(Mobile from, Item item)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true; // Staff can access anything
        }

        var house = BaseHouse.FindHouseAt(item);

        if (house == null)
        {
            return false;
        }

        return Level switch
        {
            SecureLevel.Owner    => house.IsOwner(from),
            SecureLevel.CoOwners => house.IsCoOwner(from),
            SecureLevel.Friends  => house.IsFriend(from),
            SecureLevel.Anyone   => true,
            SecureLevel.Guild    => house.IsGuildMember(from),
            _                    => false
        };
    }

    public override void OnDoubleClick(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (house == null || !IsLockedDown)
        {
            from.SendLocalizedMessage(1114298); // This must be locked down in order to use it.
        }
        else if (from.InRange(GetWorldLocation(), 2) && CheckAccessible(from, this))
        {
            if (NextResourceCount > Core.Now)
            {
                NextResourceCount = Core.Now + TimeSpan.FromDays(1);
                from.AddToBackpack(new RawRibs(2));
            }
            else
            {
                from.SendLocalizedMessage(1071539); // Sorry. You cannot receive another item at this time.
            }
        }
    }
}
