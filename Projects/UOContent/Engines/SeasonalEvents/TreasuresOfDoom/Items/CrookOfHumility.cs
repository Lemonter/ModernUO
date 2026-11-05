using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/CrookOfHumility.cs).
///     ServUO's <c>IsArtifact</c> and the <c>Slayer3</c> (talisman slayer) assignment are dropped: neither exists
///     here (TalismanSlayerName exists, but ShepherdsCrook has no Slayer3 member).
/// </summary>
[SerializationGenerator(0, false)]
public partial class CrookOfHumility : ShepherdsCrook
{
    public override int LabelNumber => 1155624; // Crook of Humility

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public CrookOfHumility()
    {
        Attributes.SpellChanneling = 1;
        Attributes.BonusInt = 10;
        Attributes.WeaponDamage = 20;
    }
}
