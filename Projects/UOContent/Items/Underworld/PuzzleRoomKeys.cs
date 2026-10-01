using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/PuzzleRoom/
// {CopperPuzzleKey,GoldPuzzleKey}.cs) — trivial timed tokens, both decay on their own.
[SerializationGenerator(0, false)]
public partial class CopperPuzzleKey : BaseDecayingItem
{
    public override int Lifespan => 1800;
    public override int LabelNumber => 1024110; // copper key

    [Constructible]
    public CopperPuzzleKey() : base(4115)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class GoldPuzzleKey : BaseDecayingItem
{
    public override int Lifespan => 1800;
    public override int LabelNumber => 1024111; // gold key

    [Constructible]
    public GoldPuzzleKey() : base(4114)
    {
        Hue = 1174;
    }
}
