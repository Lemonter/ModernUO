using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Revamped
/// Dungeons/BlackthornDungeon/Items/QuiverOfInfinityBase). ReforgedSuffix dropped: the ReforgedSuffix type does
/// not exist in this codebase.</summary>
[SerializationGenerator(0, false)]
public partial class CloakBearingTheCrestOfBlackthorn : Cloak
{
    [Constructible]
    public CloakBearingTheCrestOfBlackthorn()
    {
        Hue = 1766;
        Attributes.DefendChance = 5;
    }
}

[SerializationGenerator(0, false)]
public partial class GargishClothWingArmorBearingTheCrestOfBlackthorn : GargishClothWingArmor
{
    [Constructible]
    public GargishClothWingArmorBearingTheCrestOfBlackthorn()
    {
        Hue = 1766;
        Attributes.DefendChance = 5;
    }
}
