using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/CloakOfLight.cs).
///     ServUO's <c>IsArtifact</c> and <c>[Alterable]</c> are dropped: neither exists here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CloakOfLight : Cloak
{
    public override int LabelNumber => 1155608; // Cloak of Light

    [Constructible]
    public CloakOfLight()
    {
        Attributes.NightSight = 1;
        Attributes.RegenHits = 2;
        Attributes.AttackChance = 5;
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/CloakOfLight.cs).
///     ServUO's <c>IsArtifact</c> is dropped: it does not exist here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WingArmorOfLight : GargishClothWingArmor
{
    public override int LabelNumber => 1155683; // Wing Armor of Light

    [Constructible]
    public WingArmorOfLight()
    {
        Attributes.NightSight = 1;
        Attributes.RegenHits = 2;
        Attributes.AttackChance = 5;
    }
}
