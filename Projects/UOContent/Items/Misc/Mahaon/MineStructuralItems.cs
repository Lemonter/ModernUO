using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Walkable floor inside a dug mine shaft. Placeholder graphic (dirt patch) — reskin freely.</summary>
[SerializationGenerator(0, false)]
public partial class MineFloorTile : Item
{
    [Constructible]
    public MineFloorTile() : base(0x53B)
    {
        Movable = false;
        Name = "пол шахты";
    }
}

/// <summary>
///     Undug rock at the frontier of a mine shaft. Impassable — dig it to expand the
///     floor. Placeholder graphic (boulder) — reskin freely.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MineRockWall : Item
{
    [SerializableField(0)]
    private string _veinResourceName;

    [SerializableField(1)]
    private Point3D _originCenter;

    [SerializableField(2)]
    private int _oreReserve;

    [SerializableField(3)]
    private bool _walkable;

    [SerializableField(4)]
    private int _growthDx;

    [SerializableField(5)]
    private int _growthDy;

    [Constructible]
    public MineRockWall() : base(0x8E8)
    {
        Movable = false;
        Name = "скальная стена";
    }

    public bool IsVein => !string.IsNullOrEmpty(_veinResourceName);

    public void MarkAsVein(string resourceTypeName, string resourceNameRu, int hue)
    {
        _veinResourceName = resourceTypeName;
        Hue = hue;
        Name = $"скальная стена — жила ({resourceNameRu})";
    }
}

/// <summary>
///     A support beam. Floor tiles dug without one nearby will collapse after a delay —
///     placeholder graphic (stacked boards) — reskin freely.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MineSupportBeam : Item
{
    [Constructible]
    public MineSupportBeam() : base(0x1BD7)
    {
        Weight = 10.0;
        Name = "опорная балка";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи место в шахте, куда вклинить балку.");
        from.Target = new SupportBeamPlaceTarget(this);
    }
}

public class SupportBeamPlaceTarget : Server.Targeting.Target
{
    private readonly MineSupportBeam _beam;

    public SupportBeamPlaceTarget(MineSupportBeam beam) : base(2, true, Server.Targeting.TargetFlags.None) =>
        _beam = beam;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_beam.Deleted || targeted is not IPoint3D p)
        {
            return;
        }

        var map = from.Map;
        if (map == null || !from.InRange(new Point3D(p.X, p.Y, p.Z), 2))
        {
            from.SendMessage("Слишком далеко.");
            return;
        }

        Systems.MahaonMining.MineComplexSystem.PlaceSupportBeam(from, _beam, new Point3D(p.X, p.Y, p.Z), map);
    }
}
