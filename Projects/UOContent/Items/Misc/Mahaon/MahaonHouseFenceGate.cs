using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

/// <summary>
///     The one opening in a house's fence ring. Same graphics/sounds as the engine's own
///     IronGate (see Doors.cs: 0x824 + 2*facing / 0x825 + 2*facing, sounds 0xEC/0xF3), to
///     match the iron fence around it — copied rather than subclassed, so this stays a single
///     BaseDoor-deep inheritance like every other custom door in the project instead of
///     layering a second SerializationGenerator on top of an already-generated concrete door
///     class (unproven anywhere else here). Open/close and lockable via ILockable (built into
///     BaseDoor, same as a house's own front door) come for free — only OwnerHouse is new,
///     tracked the same way MahaonHouseFence tracks it, so MahaonHouseFenceSystem finds and
///     removes this along with the rest of the fence.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonHouseFenceGate : BaseDoor
{
    [SerializableField(0, setter: "private")]
    private BaseHouse _ownerHouse;

    [Constructible]
    public MahaonHouseFenceGate(DoorFacing facing, BaseHouse ownerHouse) : base(
        0x824 + 2 * (int)facing,
        0x825 + 2 * (int)facing,
        0xEC,
        0xF3,
        GetOffset(facing)
    )
    {
        Name = "Калитка усадьбы";
        OwnerHouse = ownerHouse;
    }

    /// <summary>The old wooden gate (LightWoodGate graphics 0x839-0x848) of the stone fence.</summary>
    public bool IsLegacy => ClosedId is >= 0x839 and <= 0x848;

    // A house deleted before fences went with their house left its fence standing; it is
    // cleared on the next load.
    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (OwnerHouse?.Deleted != false)
        {
            Timer.DelayCall(Delete);
        }
    }
}
