using ModernUO.Serialization;
using System;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/ShadowCloakOfRejuvenation.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShadowCloakOfRejuvenation : Cloak
{
    public override int LabelNumber => 1115649; // Shadow Cloak Of Rejuvenation

    [Constructible]
    public ShadowCloakOfRejuvenation()
    {
        Hue = 1884;
        Attributes.RegenMana = 1;
        Attributes.RegenHits = 1;
        Attributes.RegenStam = 1;
        Attributes.LowerManaCost = 2;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishShadowCloakOfRejuvenation : GargishClothWingArmor
{
    public override int LabelNumber => 1115649; // Shadow Cloak Of Rejuvenation

    [Constructible]
    public GargishShadowCloakOfRejuvenation()
    {
        Hue = 1884;
        Attributes.RegenMana = 1;
        Attributes.RegenHits = 1;
        Attributes.RegenStam = 1;
        Attributes.LowerManaCost = 2;
    }
}
