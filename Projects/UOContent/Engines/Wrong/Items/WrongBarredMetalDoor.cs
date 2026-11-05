using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (WrongBarredMetalDoor.cs) — the cell door that relocks
///     itself ten seconds after being picked.
///
///     Adapted: ServUO's <c>IMageUnlockable</c> and its telekinesis hook both went. This codebase's
///     BaseDoor.OnTelekinesis is not virtual, so the magery-gated unlock cannot be layered on top of it —
///     the door still opens to telekinesis like any other, it just no longer skips the magery check. The
///     self-relock timer is a one-shot token cancelled with the door instead of a Timer subclass.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WrongBarredMetalDoor : BaseDoor, ILockpickable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _lockLevel;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _maxLockLevel;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _requiredSkill;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _picker;

    private TimerExecutionToken _relockToken;

    public WrongBarredMetalDoor(DoorFacing facing)
        : base(0x685 + 2 * (int)facing, 0x686 + 2 * (int)facing, 0xEC, 0xF3, GetOffset(facing))
    {
        Locked = true;
        _lockLevel = 80;
        _maxLockLevel = 110;
        _requiredSkill = 100;
    }

    public void LockPick(Mobile from)
    {
        Picker = from;
        Locked = false;
        StartRelock();
    }


    public override void Use(Mobile from)
    {
        if (Locked && !Open)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501746); // It appears to be locked.
            return;
        }

        base.Use(from);
    }

    private void StartRelock()
    {
        _relockToken.Cancel();
        Timer.StartTimer(System.TimeSpan.FromSeconds(10), Relock, out _relockToken);
    }

    private void Relock() => Locked = true;

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _relockToken.Cancel();
    }
}
