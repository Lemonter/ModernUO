using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/KotlDoor.cs).
///     The door stays sealed until a nearby switch clears <see cref="BaseDoor.Locked" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KotlDoor : MetalDoor
{
    public static KotlDoor Instance { get; set; }

    [Constructible]
    public KotlDoor() : base(DoorFacing.EastCW)
    {
        Hue = 2591;
        Locked = true;

        KeyValue = Key.RandomValue();

        Instance ??= this;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Instance ??= this;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (Locked)
        {
            from.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1157017, from.NetState); // *The door is sealed shut. There is likely a switch that controls it nearby.*
        }
        else
        {
            Use(from);
        }
    }
}
