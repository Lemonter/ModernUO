using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/DoomDecorationArtifacts.cs).
///     IncenseBurner: BaseArtifactLight has no counterpart here; re-based on BaseLight.
/// </summary>
[SerializationGenerator(0, false)]
public partial class HangingPlatemailArms : BaseDecorationArtifact
{
    public override int LabelNumber => 1124703; // Hanging Platemail

    public override int ArtifactRarity => 8;

    [Constructible]
    public HangingPlatemailArms()
        : base(40681)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HangingPlatemailLeggings : BaseDecorationArtifact
{
    public override int LabelNumber => 1124703; // Hanging Platemail

    public override int ArtifactRarity => 8;

    [Constructible]
    public HangingPlatemailLeggings()
        : base(40683)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HangingPlatemailTunic : BaseDecorationArtifact
{
    public override int LabelNumber => 1124703; // Hanging Platemail

    public override int ArtifactRarity => 8;

    [Constructible]
    public HangingPlatemailTunic()
        : base(40679)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ArcaneTable : BaseDecorationArtifact
{
    public override int LabelNumber => 1124701; // Table

    public override int ArtifactRarity => 9;

    [Constructible]
    public ArcaneTable()
        : base(40677)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ArtifactBookshelf : BaseDecorationArtifact
{
    public override int LabelNumber => 1124699; // Bookshelf

    public override int ArtifactRarity => 9;

    [Constructible]
    public ArtifactBookshelf()
        : base(40675)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class IncenseBurner : BaseLight
{
    public override int LabelNumber => Burning ? 1124691 : 1124709; // Incense / Incense Burner

    [Constructible]
    public IncenseBurner() : base(40685)
    {
        Duration = TimeSpan.Zero;
        Burning = false;
        Light = LightType.Circle300;
        Weight = 10.0;
    }

    public override int LitItemID => ItemID == 40685 ? 40667 : 40671;

    public override int UnlitItemID => ItemID == 40667 ? 40685 : 40686;

    public void Flip()
    {
        Light = LightType.Circle300;

        switch (this.ItemID)
        {
            case 40685:
                ItemID = 40686;
                break;//unlit
            case 40667:
                ItemID = 40671;
                break;//lit
            case 40686:
                ItemID = 40685;
                break;//unlit
            case 40671:
                ItemID = 40667;
                break;//lit
        }
    }
}

[SerializationGenerator(0, false)]
public partial class BambooStoolArtifact : BaseDecorationArtifact
{
    public override int ArtifactRarity => 6;

    [Constructible]
    public BambooStoolArtifact()
        : base(0x11FC)
    {
    }
}
