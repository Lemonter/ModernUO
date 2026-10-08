using ModernUO.Serialization;
using Server.Multis;

namespace Server.Items;

/// <summary>
///     The one opening in a house's fence ring. Same graphics/sounds as the engine's own
///     LightWoodGate (see Doors.cs: 0x839 + 2*facing / 0x83A + 2*facing, sounds 0xEB/0xF2) —
///     copied rather than subclassed, so this stays a single BaseDoor-deep inheritance like
///     every other custom door in the project instead of layering a second
///     SerializationGenerator on top of an already-generated concrete door class (unproven
///     anywhere else here). Open/close and lockable via ILockable (built into BaseDoor,
///     same as a house's own front door) come for free — only OwnerHouse is new, tracked
///     the same way MahaonHouseFence tracks it, so MahaonHouseFenceSystem finds and removes
///     this along with the rest of the fence.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonHouseFenceGate : BaseDoor
{
    [SerializableField(0, setter: "private")]
    private BaseHouse _ownerHouse;

    [Constructible]
    public MahaonHouseFenceGate(DoorFacing facing, BaseHouse ownerHouse) : base(
        0x839 + 2 * (int)facing,
        0x83A + 2 * (int)facing,
        0xEB,
        0xF2,
        GetOffset(facing)
    )
    {
        Name = "Калитка усадьбы";
        OwnerHouse = ownerHouse;
    }

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
