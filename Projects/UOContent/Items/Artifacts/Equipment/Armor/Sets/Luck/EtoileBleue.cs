using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/Sets/Luck/EtoileBleue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class EtoileBleue : GoldRing
{
    public override int LabelNumber => 1080238; // Etoile Bleue

    [Constructible]
    public EtoileBleue() : base()
    {
        this.Weight = 1.0;
        this.Hue = 1165;

        this.Attributes.Luck = 150;
        this.Attributes.CastSpeed = 1;
        this.Attributes.CastRecovery = 1;

    }
}
