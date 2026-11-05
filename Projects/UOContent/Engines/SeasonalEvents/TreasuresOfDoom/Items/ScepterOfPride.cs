using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/ScepterOfPride.cs).
///     ServUO's <c>IsArtifact</c> is dropped: it does not exist here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ScepterOfPride : Scepter
{
    public override int LabelNumber => 1155623; // Scepter of Pride

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public ScepterOfPride()
    {
        WeaponAttributes.HitLeechStam = 70;
        WeaponAttributes.HitLeechMana = 70;
        WeaponAttributes.HitLeechHits = 70;
        Attributes.WeaponSpeed = 30;

        Slayer = SlayerName.Exorcism;
        Slayer2 = SlayerName.Silver;
    }
}
