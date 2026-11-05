using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SingingBall : Item, ISecurable, IGumpToggleItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _turnedOn;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public SingingBall(int itemId = 0xE2E) : base(itemId)
    {
        Weight = 10.0;
        LootType = LootType.Blessed;

        Light = LightType.Circle300;
    }

    public override int LabelNumber => 1041245; // Singing Ball

    public override bool HandlesOnMovement => _turnedOn && IsLockedDown;

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

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (_turnedOn && IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, Location, 2) &&
            !Utility.InRange(oldLocation, Location, 2))
        {
            Effects.PlaySound(Location, Map, SoundList());
        }

        base.OnMovement(m, oldLocation);
    }

    public virtual int SoundList() => Utility.RandomMinMax(0, 1338);

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(_turnedOn ? 502695 : 502696); // turned on / turned off
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!CheckAccessible(from, this))
        {
            PublicOverheadMessage(MessageType.Regular, 0x3E9, 1061637); // You are not allowed to access
            return;
        }

        if (TurnedOn)
        {
            from.SendGump(new TurnOffGump(this));
        }
        else
        {
            from.SendGump(new TurnOnGump(this));
        }
    }
}
