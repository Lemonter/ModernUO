using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Decorative/KepetchWax.cs).</summary>
[SerializationGenerator(0, false)]
public partial class KepetchWax : Item
{
    [Constructible]
    public KepetchWax() : base(0x5745)
    {
    }

    public override int LabelNumber => 1112412; // kepetch wax
}
