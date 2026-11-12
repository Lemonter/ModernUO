using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Multis;
using Server.Systems.MahaonMapEdits;

namespace Server.Items;

/// <summary>
///     Деревья, убранные с линии ограды дома (MahaonHouseFenceSystem): статика карты — ствол
///     и крона — и посаженные игроками деревья-предметы. Ограду снимают или дом сносят —
///     всё возвращается на место, как было.
///
///     Статику убирает и возвращает MahaonMapEdits, то есть по-настоящему: и на сервере
///     (дерево больше не мешает пройти), и у клиентов. Посаженные деревья лежат во
///     внутренней карте и возвращаются теми же предметами — со своим возрастом и урожаем.
///
///     Сам невидим и лежит во внутренней карте.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonFenceClearing : Item
{
    [SerializableField(0)]
    private BaseHouse _house;

    [SerializableField(1)]
    private Map _area;

    // Убранная статика: место с высотой, картинка, оттенок — три параллельных списка.
    [SerializableField(2)]
    private List<Point3D> _spots = [];

    [SerializableField(3)]
    private List<int> _graphics = [];

    [SerializableField(4)]
    private List<int> _hues = [];

    // Посаженные деревья (ствол и крона — отдельные предметы) и где они стояли.
    [SerializableField(5)]
    private List<Item> _trees = [];

    [SerializableField(6)]
    private List<Point3D> _treeSpots = [];

    public MahaonFenceClearing(BaseHouse house, Map area) : base(0x1F14)
    {
        _house = house;
        _area = area;
        Movable = false;
        Visible = false;
    }

    public bool IsEmpty => Spots.Count == 0 && Trees.Count == 0;

    /// <summary>Убирает деревья с клетки ограды: всё, что выше земли на этой клетке и
    /// относится к дереву, — ствол, крону, посаженное дерево.</summary>
    public void Clear(int x, int y, int groundZ)
    {
        foreach (var s in MahaonMapEdits.StaticsAt(Area, x, y))
        {
            if (s.Z < groundZ - 5 || s.Z > groundZ + 60 || !IsTreePart(s.Graphic))
            {
                continue;
            }

            if (MahaonMapEdits.RemoveStatic(Area, x, y, s.Graphic, s.Z))
            {
                Spots.Add(new Point3D(x, y, s.Z));
                Graphics.Add(s.Graphic);
                Hues.Add(s.Hue);
            }
        }

        var planted = new List<Item>();
        foreach (var item in Area.GetItemsAt(x, y))
        {
            if (item is MahaonTree or MahaonTreeFoliage)
            {
                planted.Add(item);
            }
        }

        foreach (var item in planted)
        {
            Trees.Add(item);
            TreeSpots.Add(item.Location);
            item.MoveToWorld(item.Location, Map.Internal);
        }

        this.MarkDirty();
    }

    /// <summary>Ствол или крона: что рубит лесоруб, и всё, что тайлдата зовёт листвой.</summary>
    public static bool IsTreePart(int graphic) =>
        MahaonSuccession.IsTree(graphic) ||
        (TileData.ItemTable[graphic & TileData.MaxItemValue].Flags & TileFlag.Foliage) != 0;

    /// <summary>Возвращает всё убранное на место и исчезает сам.</summary>
    public void Restore()
    {
        if (Area != null && Area != Map.Internal)
        {
            MahaonMapEdits.BeginBatch();
            try
            {
                for (var i = 0; i < Spots.Count; i++)
                {
                    var p = Spots[i];
                    if (!HasStatic(p, Graphics[i]))
                    {
                        MahaonMapEdits.AddStatic(Area, p.X, p.Y, p.Z, Graphics[i], Hues[i]);
                    }
                }
            }
            finally
            {
                MahaonMapEdits.EndBatch();
            }

            for (var i = 0; i < Trees.Count; i++)
            {
                if (Trees[i] is { Deleted: false } tree)
                {
                    tree.MoveToWorld(TreeSpots[i], Area);
                }
            }
        }
        else
        {
            foreach (var tree in Trees)
            {
                tree?.Delete();
            }
        }

        Spots.Clear();
        Graphics.Clear();
        Hues.Clear();
        Trees.Clear();
        TreeSpots.Clear();
        Delete();
    }

    private bool HasStatic(Point3D p, int graphic)
    {
        foreach (var s in MahaonMapEdits.StaticsAt(Area, p.X, p.Y))
        {
            if (s.Graphic == graphic && s.Z == p.Z)
            {
                return true;
            }
        }

        return false;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (House?.Deleted != false)
        {
            // Дом снесли, пока деревья не умели возвращаться, — вернуть при загрузке.
            Timer.DelayCall(Restore);
            return;
        }

        Systems.MahaonWorld.MahaonHouseFenceSystem.RegisterClearing(this);
    }

    public override void OnDelete()
    {
        Systems.MahaonWorld.MahaonHouseFenceSystem.UnregisterClearing(this);
        base.OnDelete();
    }
}
