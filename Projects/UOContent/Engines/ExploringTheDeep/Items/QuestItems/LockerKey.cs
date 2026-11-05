using System;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/QuestItems/LockerKey.cs). Dropped by the
///     one barrel in a locker's ring that carries it; using it on the matching locker releases the spare
///     part and puts the locker on a ten minute respawn cycle. The key itself expires after five minutes,
///     which is <see cref="BaseDecayingItem" />'s job.
///
///     Substitution: ServUO's <c>GetProperties</c> used <c>String.Format("#{0}", cliloc)</c> for the part
///     name reference, which is a real cliloc indirection in the tooltip rather than text; the same
///     argument is built with an interpolated string here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class LockerKey : BaseDecayingItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Parts _type;

    [Constructible]
    public LockerKey() : this(Parts.None)
    {
    }

    [Constructible]
    public LockerKey(Parts type) : base(0x410B)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
        Hue = 1359;
        _type = type;
        QuestItem = true;
    }

    public override int LabelNumber => 1154227; // Strongbox Key

    public override int Lifespan => 300;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // A Key to the Spare ~1_PART~ Parts Locker
        list.Add(1154445, $"#{1154427 + (int)_type}");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(501661); // That key is unreachable.
            return;
        }

        from.SendLocalizedMessage(501662); // What shall I use this key on?

        // *You insert the key into the mechanism and turn it. To your delight the lock opens with a click
        // and you remove the contents*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154228);

        from.Target = new UnlockTarget(this);
    }

    private class UnlockTarget : Target
    {
        private readonly LockerKey _key;

        public UnlockTarget(LockerKey key) : base(1, false, TargetFlags.None) => _key = key;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_key.Deleted || !_key.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(501661); // That key is unreachable.
                return;
            }

            if (targeted is not StorageLocker locker)
            {
                from.SendLocalizedMessage(501666); // You can't unlock that!
                return;
            }

            if (locker.Type != _key.Type)
            {
                return;
            }

            locker.Locked = false;

            var part = locker.Type switch
            {
                Parts.Flywheel        => (Item)new FlyWheel(),
                Parts.BearingAssembly => new BearingAssembly(),
                Parts.PowerCore       => new PowerCore(),
                Parts.WireSpool       => new WireSpool(),
                _                     => null
            };

            if (part != null)
            {
                locker.DropItem(part);
            }

            locker.BeginRestart(TimeSpan.FromMinutes(10.0));

            _key.Delete();
        }
    }
}
