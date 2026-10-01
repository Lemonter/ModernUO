using Xunit;

namespace Server.Tests.Maps;

[Collection("Sequential Server Tests")]
public class SectorActivatorTests
{
    private sealed class Activator : Mobile
    {
        public Activator() : base(World.NewMobile) => DefaultMobileInit();

        public override bool ActivatesSectors => true;
    }

    private sealed class SleepCounter : Mobile
    {
        public SleepCounter() : base(World.NewMobile) => DefaultMobileInit();

        public int Deactivations { get; private set; }

        public override void OnSectorDeactivate() => Deactivations++;
    }

    // Far from anything other tests place, so no stray activator keeps these sectors awake.
    private static readonly Point3D Origin = new(3000, 3000, 0);

    private static Map.Sector SectorAt(Map map, int x, int y) => map.GetSector(x, y);

    [Fact]
    public void Activator_WakesSectorsInRange_AndSleepsThemWhenGone()
    {
        var map = Map.Felucca;
        var mobile = new Activator();

        try
        {
            mobile.MoveToWorld(Origin, map);

            var edge = Origin.X + (Map.SectorActiveRange << Map.SectorShift);
            Assert.True(SectorAt(map, Origin.X, Origin.Y).Active);
            Assert.True(SectorAt(map, edge, Origin.Y).Active);
            Assert.False(SectorAt(map, edge + (2 << Map.SectorShift), Origin.Y).Active);

            mobile.Delete();

            Assert.False(SectorAt(map, Origin.X, Origin.Y).Active);
            Assert.Equal(0, SectorAt(map, Origin.X, Origin.Y).ActivatorCount);
        }
        finally
        {
            mobile.Delete();
        }
    }

    [Fact]
    public void Activator_MovingAcrossSectors_KeepsOverlapAwake()
    {
        var map = Map.Felucca;
        var mobile = new Activator();
        var watcher = new SleepCounter();

        try
        {
            mobile.MoveToWorld(Origin, map);

            // A plain mobile in a sector both ranges cover.
            var overlap = new Point3D(Origin.X + (1 << Map.SectorShift), Origin.Y, 0);
            watcher.MoveToWorld(overlap, map);
            var overlapSector = SectorAt(map, overlap.X, overlap.Y);
            Assert.True(overlapSector.Active);

            // Step into the next sector east.
            mobile.Location = new Point3D(Origin.X + (1 << Map.SectorShift), Origin.Y, 0);

            Assert.True(overlapSector.Active);
            Assert.Equal(0, watcher.Deactivations);
            Assert.Equal(0, SectorAt(map, Origin.X, Origin.Y).ActivatorCount);
            Assert.Equal(1, SectorAt(map, mobile.X, mobile.Y).ActivatorCount);
        }
        finally
        {
            mobile.Delete();
            watcher.Delete();
        }
    }
}
