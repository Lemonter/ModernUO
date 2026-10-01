using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Cut from anything the Void has already reshaped once. Ported from ServUO
/// (Scripts/Items/Resource/VoidEssence.cs).</summary>
[SerializationGenerator(0, false)]
public partial class VoidEssence : Item, ICommodity
{
    [Constructible]
    public VoidEssence(int amount = 1) : base(0x4007)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1112327; // void essence

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

/// <summary>Only the fully evolved void creatures carry one. Ported from ServUO
/// (Scripts/Items/Resource/VoidCore.cs).</summary>
[SerializationGenerator(0, false)]
public partial class VoidCore : Item, ICommodity
{
    [Constructible]
    public VoidCore(int amount = 1) : base(0x5728)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double DefaultWeight => 0.1;

    public override int LabelNumber => 1113334; // void core

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

/// <summary>Shards of gargoyle pottery the void creatures turn up. Ported from ServUO
/// (Scripts/Items/Quest/SAQuestItems.cs); the Royal City museum asks for ten.</summary>
[SerializationGenerator(0, false)]
public partial class AncientPotteryFragments : Item
{
    [Constructible]
    public AncientPotteryFragments() : base(0x2243) => Hue = 2108;

    public override int LabelNumber => 1112990; // Ancient Pottery fragments
}
