using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/KrampusMinionEarrings.cs).
///     ServUO's CanBeWornByGargoyles has no counterpart here; RequiredRace handles the gargoyle restriction.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KrampusMinionEarrings : BaseArmor
{
    public override int LabelNumber => 1125645; // krampus minion earrings

    [Constructible]
    public KrampusMinionEarrings() : base(0xA295)
    {
        Layer = Layer.Earrings;
    }

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    public override ArmorMaterialType MaterialType => ArmorMaterialType.Chainmail;

    public override ArmorMeditationAllowance DefMedAllowance => ArmorMeditationAllowance.All;

    public override int BasePhysicalResistance => 1;

    public override int BaseFireResistance => 2;

    public override int BaseColdResistance => 2;

    public override int BasePoisonResistance => 2;

    public override int BaseEnergyResistance => 3;

    public override int InitMinHits => 30;

    public override int InitMaxHits => 50;
}
