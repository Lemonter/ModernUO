using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Glasses/LegendaryMapmakersGlasses.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LegendaryMapmakersGlasses : ElvenGlasses
{
    public override int LabelNumber => 1159023; //Legendary Mapmaker's Glasses

    [Constructible]
    public LegendaryMapmakersGlasses()
    {
        SkillBonuses.SetValues(0, SkillName.Cartography, Utility.RandomMinMax(1, 5));
        Quality = ArmorQuality.Exceptional;
    }
}
