using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HumansAndElvesRobe : Robe
{
    [Constructible]
    public HumansAndElvesRobe()
    {
        LootType = LootType.Blessed;
        Hue = Utility.RandomDyedHue();
    }

    public override int LabelNumber => 1151202; // Humans & Elves are our friends!
}

[SerializationGenerator(0, false)]
public partial class GargoylesAreOurFriendsRobe : Robe
{
    [Constructible]
    public GargoylesAreOurFriendsRobe()
    {
        LootType = LootType.Blessed;
        Hue = Utility.RandomDyedHue();
    }

    public override int LabelNumber => 1151203; // Gargoyles are our friends!
}

[SerializationGenerator(0, false)]
public partial class WeArePiratesRobe : Robe
{
    [Constructible]
    public WeArePiratesRobe()
    {
        LootType = LootType.Blessed;
        Hue = Utility.RandomDyedHue();
    }

    public override int LabelNumber => 1151204; // We are pirates!
}

[SerializationGenerator(0, false)]
public partial class FollowerOfBaneRobe : Robe
{
    [Constructible]
    public FollowerOfBaneRobe()
    {
        LootType = LootType.Blessed;
        Hue = Utility.RandomDyedHue();
    }

    public override int LabelNumber => 1151205; // Follower of Bane
}

[SerializationGenerator(0, false)]
public partial class QueenDawnForeverRobe : Robe
{
    [Constructible]
    public QueenDawnForeverRobe()
    {
        LootType = LootType.Blessed;
        Hue = Utility.RandomDyedHue();
    }

    public override int LabelNumber => 1151206; // Queen Dawn Forever
}
