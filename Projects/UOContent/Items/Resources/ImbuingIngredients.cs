using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Four of the Stygian Abyss imbuing ingredients, ported from ServUO
/// (Scripts/Items/Resource/). They are what the Meager Imbuing Bag pays out, which is Ansikart's
/// reward for "A Little Something".
///
/// These four are here because that reward hands them out. The rest of the set — roughly
/// twenty-six more, plus ServUO's IngredientDropEntry table that hangs them off creature deaths
/// — is deliberately not ported: this shard replaced OSI imbuing with its own three-tier
/// material system (Systems/MahaonImbuing), so those items would have no consumer. Whether to
/// bring the full OSI ingredient economy across is an owner decision, noted in the ТЗ.</summary>
[SerializationGenerator(0, false)]
public partial class SlithTongue : Item, ICommodity
{
    [Constructible]
    public SlithTongue(int amount = 1) : base(0x5746)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1113359; // slith tongue

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class GoblinBlood : Item, ICommodity
{
    [Constructible]
    public GoblinBlood(int amount = 1) : base(0x572C)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1113335; // goblin blood

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class ReflectiveWolfEye : Item, ICommodity
{
    [Constructible]
    public ReflectiveWolfEye(int amount = 1) : base(0x5749)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1113362; // reflective wolf eye

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class RaptorTeeth : Item, ICommodity
{
    [Constructible]
    public RaptorTeeth(int amount = 1) : base(0x5747)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1113360; // raptor teeth

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
