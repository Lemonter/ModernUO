using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/KhaldunCampRegion.cs).
///     An invisible line that only lets detectives holding <see cref="DetectiveCredentials" /> pass; the guard
///     speaks a warning when someone is turned away.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunCampBlocker : Item
{
    [SerializableField(0)]
    private int _position;

    [Constructible]
    public KhaldunCampBlocker(int position) : base(0x1BC3)
    {
        _position = position;

        Name = "заграждение лагеря Халдуна";
        Movable = false;
        Visible = false;
    }

    [Constructible]
    public KhaldunCampBlocker() : this(0)
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return base.OnMoveOver(m);
        }

        if (m is BaseCreature { Controlled: true } creature && creature.GetMaster() is PlayerMobile master)
        {
            m = master;
        }

        var hasCredentials = m.FindItemOnLayer(Layer.Neck) is DetectiveCredentials;
        var canCross = hasCredentials;

        if (!canCross)
        {
            // Another blocker on the same tile means the line is already broken here; let the move through.
            foreach (var blocker in m.Map.GetItemsInRange<KhaldunCampBlocker>(m.Location, 0))
            {
                if (blocker != this && !blocker.Deleted)
                {
                    return base.OnMoveOver(m);
                }
            }

            if (X == 5991 && Y == 3749)
            {
                CheckGuard(m, false);
                return false;
            }

            var moving = Utility.GetDirection(m.Location, GetWorldLocation());

            canCross = _position switch
            {
                0    => moving is Direction.North or Direction.Up or Direction.West,
                1 or 2 => moving is Direction.North or Direction.Up or Direction.Right,
                3    => moving is Direction.South or Direction.Down or Direction.East,
                4    => moving is Direction.West or Direction.Up or Direction.Left,
                5    => moving is Direction.North or Direction.Right,
                _    => canCross
            };
        }

        if (!canCross)
        {
            CheckGuard(m, false);
            return false;
        }

        if (hasCredentials)
        {
            CheckGuard(m, true);
        }

        return base.OnMoveOver(m);
    }

    private void CheckGuard(Mobile m, bool pass)
    {
        foreach (var guard in Map.GetMobilesInRange<KhaldunCampGuard>(m.Location, 10))
        {
            if (pass)
            {
                guard.PrivateOverheadMessage(MessageType.Regular, 0x47E, 1158716, m.NetState); // *nods* Please proceed, Detective
            }
            else
            {
                guard.PrivateOverheadMessage(MessageType.Regular, 0x47E, 1158715, m.NetState); // Halt! Who goes there! Official RBG Detective personnel only!
            }

            break;
        }
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Quest/Items/DetectiveCredentials.cs).
///     Ported here because the camp gate and the lead investigator both key off this necklace.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DetectiveCredentials : BaseNecklace
{
    public override int LabelNumber => 1158641; // RBG Detective Branch Official Credential

    [Constructible]
    public DetectiveCredentials() : base(0x1088)
    {
        Hue = 1176;
        LootType = LootType.Blessed;
    }
}
