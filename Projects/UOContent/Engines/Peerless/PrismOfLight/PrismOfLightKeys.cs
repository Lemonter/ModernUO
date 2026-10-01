using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The six crystal offerings the Prism of Light pedestals accept, ported from ServUO
/// (Scripts/Items/Quest/). ShatteredCrystals already existed here as a plain Item — it was
/// written before the PeerlessKey base was ported, with a comment saying so — and now sits on
/// the real base along with the other five, in Items/Misc/TerMur/ShatteredCrystals.cs.</summary>
[SerializationGenerator(0, false)]
public partial class BrokenCrystals : PeerlessKey
{
    [Constructible]
    public BrokenCrystals() : base(0x2247)
    {
        Weight = 1.0;
        Hue = 0x2B2;
    }

    public override int LabelNumber => 1074261; // broken crystals
}

[SerializationGenerator(0, false)]
public partial class CrushedCrystals : PeerlessKey
{
    [Constructible]
    public CrushedCrystals() : base(0x223C)
    {
        Weight = 1.0;
        Hue = 0x47E;
    }

    public override int LabelNumber => 1074262; // crushed crystals
}

[SerializationGenerator(0, false)]
public partial class PiecesOfCrystal : PeerlessKey
{
    [Constructible]
    public PiecesOfCrystal() : base(0x2245)
    {
        Weight = 1.0;
        Hue = 0x2B2;
    }

    public override int LabelNumber => 1074263; // pieces of crystal
}

[SerializationGenerator(0, false)]
public partial class ScatteredCrystals : PeerlessKey
{
    [Constructible]
    public ScatteredCrystals() : base(0x2248)
    {
        Weight = 1.0;
        Hue = 0x47E;
    }

    public override int LabelNumber => 1074264; // scattered crystals
}

[SerializationGenerator(0, false)]
public partial class JaggedCrystals : PeerlessKey
{
    [Constructible]
    public JaggedCrystals() : base(0x223E)
    {
        Weight = 1.0;
        Hue = 0x2B2;
    }

    public override int LabelNumber => 1074265; // jagged crystals
}

/// <summary>The key the Prism of Light altar hands back.</summary>
[SerializationGenerator(0, false)]
public partial class PrismOfLightKey : MasterKey
{
    [Constructible]
    public PrismOfLightKey() : base(0xE27)
    {
    }
}
