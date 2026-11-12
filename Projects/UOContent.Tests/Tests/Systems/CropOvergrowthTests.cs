using Server.Items;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;
using Xunit;

namespace Server.Tests.Systems;

[Collection("Sequential UOContent Tests")]
public class CropOvergrowthTests
{
    public CropOvergrowthTests() => MahaonCropTile.Configure();

    private static void Year()
    {
        SeasonSystem.SetSeason(MahaonSeason.Summer);
        SeasonSystem.SetSeason(MahaonSeason.Autumn);
        SeasonSystem.SetSeason(MahaonSeason.Winter);
        SeasonSystem.SetSeason(MahaonSeason.Spring);
    }

    [Fact]
    public void SownPlot_NobodyHarvests_OvergrowsInAYear_ButAMapFieldStays()
    {
        var season = SeasonSystem.CurrentSeason;
        SeasonSystem.SetSeason(MahaonSeason.Spring);

        var sown = new MahaonCropTile(MahaonCropType.Wheat);
        sown.MarkPlanted();
        sown.MoveToWorld(new Point3D(2210, 1810, 0), Map.Felucca);

        var field = new MahaonCropTile(MahaonCropType.Wheat); // поле карты: не посеяно семенами
        field.MoveToWorld(new Point3D(2212, 1810, 0), Map.Felucca);

        try
        {
            Year();

            Assert.True(sown.Deleted);
            Assert.False(field.Deleted);
        }
        finally
        {
            sown.Delete();
            field.Delete();
            SeasonSystem.SetSeason(season);
        }
    }

    [Fact]
    public void SownPlot_ThatIsHarvested_Stays()
    {
        var season = SeasonSystem.CurrentSeason;
        SeasonSystem.SetSeason(MahaonSeason.Spring);

        var sown = new MahaonCropTile(MahaonCropType.Wheat);
        sown.MarkPlanted();
        sown.MoveToWorld(new Point3D(2214, 1810, 0), Map.Felucca);

        try
        {
            SeasonSystem.SetSeason(MahaonSeason.Summer);
            SeasonSystem.SetSeason(MahaonSeason.Autumn);
            sown.Harvested = true; // кто-то собрал
            SeasonSystem.SetSeason(MahaonSeason.Winter);
            SeasonSystem.SetSeason(MahaonSeason.Spring);

            Assert.False(sown.Deleted);
        }
        finally
        {
            sown.Delete();
            SeasonSystem.SetSeason(season);
        }
    }
}
