using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Multis;
using Server.Systems.MahaonWorld;

namespace Server.Items;

/// <summary>
///     Подвал ставящегося дома (BaseHouse): помнит дом, свои детали — пол, стены, факелы,
///     люк и ступени — и клетки пола. Сам невидим и лежит во внутренней карте; строит и
///     сносит его MahaonHouseBasementSystem. Городские квартиры держат подвал у себя
///     (MahaonCityHouse.Basement).
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonHouseBasement : Item
{
    [SerializableField(0)]
    private BaseHouse _house;

    [SerializableField(1)]
    private List<Item> _parts = [];

    // Клетки пола; Z у всех одна — высота пола.
    [SerializableField(2)]
    private List<Point3D> _cells = [];

    private HashSet<Point2D> _columns;

    public MahaonHouseBasement(BaseHouse house) : base(0x1F14)
    {
        _house = house;
        Movable = false;
        Visible = false;
    }

    public int FloorZ => Cells.Count > 0 ? Cells[0].Z : 0;

    public HashSet<Point2D> Columns
    {
        get
        {
            if (_columns == null)
            {
                _columns = new HashSet<Point2D>();
                foreach (var c in Cells)
                {
                    _columns.Add(new Point2D(c.X, c.Y));
                }
            }

            return _columns;
        }
    }

    public void ResetColumns() => _columns = null;

    // Пол и высота человека над ним — то же окно, что у региона городской квартиры.
    public bool Contains(Point3D p) =>
        Cells.Count > 0 && p.Z >= FloorZ - 3 && p.Z <= FloorZ + 20 && Columns.Contains(new Point2D(p.X, p.Y));

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (House?.Deleted != false)
        {
            // Дом снесли, пока подвал не умел уходить вместе с ним, — убрать при загрузке.
            Timer.DelayCall(() => MahaonHouseBasementSystem.Destroy(this));
            return;
        }

        MahaonHouseBasementSystem.Register(this);
    }

    public override void OnDelete()
    {
        MahaonHouseBasementSystem.Unregister(this);
        base.OnDelete();
    }
}

/// <summary>Люк и ступени подвала ставящегося дома: вниз пускают хозяев и друзей этого дома.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonHouseBasementHatch : MahaonBasementHatch
{
    [SerializableField(0)]
    private BaseHouse _ownerHouse;

    public MahaonHouseBasementHatch(BaseHouse house, Point3D destination, string name, int graphic)
        : base(null, destination, name, graphic) => _ownerHouse = house;

    protected override bool IsHousehold(Mobile m) =>
        OwnerHouse?.IsFriend(m) == true || m?.AccessLevel >= AccessLevel.GameMaster;
}
