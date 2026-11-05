using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x9A1E, 0x9A1F)]
[SerializationGenerator(0, false)]
public partial class ObsidianSkull : Item
{
    [Constructible]
    public ObsidianSkull() : base(0x9A1E)
    {
    }

    public override int LabelNumber => 1123478; // Obsidian Skull
}
