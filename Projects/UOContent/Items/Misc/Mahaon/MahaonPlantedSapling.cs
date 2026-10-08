using System;
using ModernUO.Serialization;
using Server.Systems.MahaonSeasons;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonPlantedSapling : Item
{
    [SerializableField(0)]
    private MahaonTreeSpecies _species;

    // A couple seasons to mature — real seasons already tick on their own cadence
    // (SeasonSystem.SeasonLength), so growth naturally speeds up or slows down with
    // whatever that's tuned to, without this needing its own separate timer constant.
    private const int SeasonsToMature = 2;

    [Constructible]
    public MahaonPlantedSapling(MahaonTreeSpecies species = MahaonTreeSpecies.Apple) : base(0x0CE9)
    {
        Movable = false;
        _species = species;
        Name = $"саженец ({MahaonTreeSpeciesTable.Get(species).NameRu}) — растёт";

        ScheduleGrowth();
    }

    private void ScheduleGrowth()
    {
        Timer.DelayCall(TimeSpan.FromTicks(SeasonSystem.SeasonLength.Ticks * SeasonsToMature), () =>
        {
            if (Deleted)
            {
                return;
            }

            var tree = new MahaonTree(_species);
            tree.PlantAt(Location, Map);
            Delete();
        });
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        // Simple approximation on server restart — just restart the same wait rather than
        // tracking exact remaining time. Not perfectly accurate but never gets stuck.
        ScheduleGrowth();
    }
}
