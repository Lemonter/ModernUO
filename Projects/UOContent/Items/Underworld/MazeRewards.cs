using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/Rewards.cs) — plain decorative trophies handed out by UnderworldPuzzleItem on a
// correct guess. Trivial one-liners, no dependencies.
[SerializationGenerator(0, false)]
public partial class MouldingBoard : Item
{
    [Constructible]
    public MouldingBoard() : base(5353)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DoughBowl : Item
{
    [Constructible]
    public DoughBowl() : base(4323)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HornedTotemPole : Item
{
    [Constructible]
    public HornedTotemPole() : base(12289)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LargeSquarePillow : Item
{
    [Constructible]
    public LargeSquarePillow() : base(5691)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class LargeDiamondPillow : Item
{
    [Constructible]
    public LargeDiamondPillow() : base(5690)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DustyPillow : Item
{
    public override int LabelNumber => 1113638; // dusty pillow

    [Constructible]
    public DustyPillow() : base(Utility.RandomList(5690, 5691))
    {
    }
}

[SerializationGenerator(0, false)]
public partial class StatuePedestal : Item
{
    [Constructible]
    public StatuePedestal() : base(13042)
    {
        Weight = 5;
    }
}

[SerializationGenerator(0, false)]
public partial class FlouredBreadBoard : Item
{
    public override int LabelNumber => 1113639; // floured bread board

    [Constructible]
    public FlouredBreadBoard() : base(0x14E9)
    {
        Weight = 3.0;
    }
}
