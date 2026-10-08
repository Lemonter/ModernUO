using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Items/Quest/DryadsBlessing.cs). The offering the
/// Blighted Grove altar wants — dropped by the grove's own creatures and handed back to the
/// altar to wake Lady Melisande.</summary>
[SerializationGenerator(0, false)]
public partial class DryadsBlessing : PeerlessKey
{
    [Constructible]
    public DryadsBlessing() : base(0x1420)
    {
        Weight = 1.0;
        Hue = 0x494;
    }

    public override int LabelNumber => 1074761; // dryad's blessing
}
