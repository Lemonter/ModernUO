using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Свойство Macaw.MacawSpawner в репозитории отсутствует, поэтому
///     обратная ссылка на гнездо у птицы не выставляется; дроп GoldFoil не портирован.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MacawSpawner : Item
{
    private static readonly Point3D[] _spawnLocs =
    [
        new(491, 1863, 95), new(496, 1865, 85), new(496, 1852, 85), new(490, 1867, 75), new(494, 1859, 95),
        new(498, 1845, 75), new(502, 1859, 55), new(484, 1871, 55), new(476, 1867, 75), new(500, 1864, 65),
        new(484, 1865, 85), new(500, 1852, 65), new(493, 1847, 94), new(479, 1869, 65), new(502, 1871, 55),
        new(502, 1848, 55), new(498, 1860, 75), new(498, 1849, 75), new(500, 1845, 65), new(472, 1871, 55),
        new(495, 1869, 65), new(490, 1871, 55), new(478, 1865, 85)
    ];

    [SerializableField(0)]
    private List<BaseCreature> _spawn;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private MacawNest _addon;

    [Constructible]
    public MacawSpawner() : base(0x9C48)
    {
        _spawn = [];
        Movable = false;
        Hue = 1281;

        _addon = new MacawNest { Foil = this };
    }

    public override int LabelNumber => 1124032; // foil sheet

    public bool Moving { get; private set; }

    public override bool VerifyMove(Mobile from)
    {
        if (Visible && Map != null && Map != Map.Internal && Utility.RandomBool())
        {
            SpawnBirdies(from);
        }

        return base.VerifyMove(from);
    }

    public void SpawnBirdies(Mobile from)
    {
        var count = Utility.RandomMinMax(3, 5);

        for (var i = 0; i < count; i++)
        {
            var p = Location;

            for (var j = 0; j < 10; j++)
            {
                var x = Utility.RandomMinMax(X - 1, X + 1);
                var y = Utility.RandomMinMax(Y - 1, Y + 2);
                var z = Map.GetAverageZ(x, y);

                if (Map.CanSpawnMobile(x, y, z))
                {
                    p = new Point3D(x, y, z);
                    break;
                }
            }

            var macaw = new Macaw();
            macaw.MoveToWorld(p, Map);
            _spawn.Add(macaw);

            Visible = false;
            Timer.StartTimer(TimeSpan.FromSeconds(Utility.RandomMinMax(60, 90)), () => Visible = true);
        }

        foreach (var macaw in _spawn)
        {
            macaw.Combatant = from;
        }
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        if (Location != oldLocation && _addon != null && !Moving)
        {
            MoveToNest();
        }

        base.OnLocationChange(oldLocation);
    }

    public override void OnMapChange()
    {
        if (_addon != null)
        {
            _addon.Map = Map;
        }

        base.OnMapChange();
    }

    private void MoveToNest()
    {
        _addon.MoveToWorld(Location, Map);
        Moving = true;
        MoveToWorld(new Point3D(_addon.X + 1, _addon.Y, _addon.Z + 9), _addon.Map);
        Moving = false;
    }

    public override void Delete()
    {
        base.Delete();

        if (_addon != null)
        {
            _addon.Foil = null;
            _addon.Delete();
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_addon != null)
        {
            _addon.Foil = this;
        }

        _spawn ??= [];
    }

    public static void Generate()
    {
        foreach (var pnt in _spawnLocs)
        {
            var nest = new MacawNest();
            nest.MoveToWorld(pnt, Map.TerMur);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class MacawNest : BaseAddon
{
    [CommandProperty(AccessLevel.GameMaster)]
    public MacawSpawner Foil { get; set; }

    [Constructible]
    public MacawNest()
    {
        AddComponent(new AddonComponent(3235), 0, 0, 0);
        AddComponent(new AddonComponent(3232), 0, 0, 0);
        AddComponent(new AddonComponent(3236), 1, 0, 0);
        AddComponent(new AddonComponent(3232), 1, -1, 0);
        AddComponent(new AddonComponent(3231), 0, -1, 0);
    }

    public override void Delete()
    {
        base.Delete();

        if (Foil != null)
        {
            Foil.Addon = null;
            Foil.Delete();
        }
    }
}
