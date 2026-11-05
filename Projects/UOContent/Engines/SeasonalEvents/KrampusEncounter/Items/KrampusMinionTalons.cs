using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/KrampusEncounter/Items/KrampusMinionTalons.cs).
///     ServUO's CanBeWornByGargoyles has no counterpart here; RequiredRace handles the gargoyle restriction.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KrampusMinionTalons : BaseShoes
{
    public override int LabelNumber => 1125644; // krampus minion talons

    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public KrampusMinionTalons() : this(0)
    {
        Weight = 2.0;
    }

    [Constructible]
    public KrampusMinionTalons(int hue) : base(0xA294, 1153)
    {
    }

    public override CraftResource DefaultResource => CraftResource.RegularLeather;
}
