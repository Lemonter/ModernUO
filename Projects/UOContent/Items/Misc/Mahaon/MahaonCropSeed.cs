using ModernUO.Serialization;
using Server.Systems.MahaonWorld;
using Server.Targeting;

namespace Server.Items;

/// <summary>Carried seed — same "double-click, target ground" flow as MahaonSapling, just
/// for crop plots instead of trees. Plants an actual MahaonCropTile the player (or anyone
/// else — plots are shared, first to click the ripe crop gets it, same as any other
/// MahaonCropTile) can harvest once it cycles round to autumn.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonCropSeed : Item
{
    [SerializableField(0)]
    private MahaonCropType _cropType;

    [Constructible]
    public MahaonCropSeed(MahaonCropType cropType = MahaonCropType.Onion) : base(0x0DCF)
    {
        // Невесомые и складываются в стопку: их теперь дают с каждого урожая, и мешок,
        // забитый пригоршнями семян по одной, превратил бы земледелие в возню с рюкзаком.
        Weight = 0.0;
        Stackable = true;
        _cropType = cropType;
        Name = $"семена ({MahaonCropTable.Data[(int)cropType].NameRu})";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи свободный участок земли, чтобы посадить семена.");
        from.Target = new CropSeedPlantTarget(this);
    }
}

public class CropSeedPlantTarget : Target
{
    private readonly MahaonCropSeed _seed;

    public CropSeedPlantTarget(MahaonCropSeed seed) : base(2, true, TargetFlags.None) => _seed = seed;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_seed.Deleted || targeted is not IPoint3D p)
        {
            return;
        }

        var map = from.Map;
        if (map == null)
        {
            return;
        }

        var loc = new Point3D(p.X, p.Y, p.Z);

        if (!from.InRange(loc, 2))
        {
            from.SendMessage("Слишком далеко.");
            return;
        }

        // Вспаханная земля — не помеха, а приглашение: сеют именно в неё, и грядка встаёт
        // на саму пашню, а не рядом с ней. Проверку места при этом пропускаем: сам дёрн
        // (0x0911-0x0914) помечен в tiledata как Surface, так что CanFit увидел бы в нём
        // препятствие и не дал бы посеять ровно там, где вспахано. Место уже проверил плуг,
        // когда клал сюда пашню.
        var tilled = MahaonTilledEarth.Find(loc, map);

        if (tilled != null)
        {
            loc.Z = tilled.Z;
        }
        else if (!map.CanFit(loc.X, loc.Y, loc.Z, 16))
        {
            from.SendMessage("Здесь нет места для грядки.");
            return;
        }

        var tile = new MahaonCropTile(_seed.CropType);
        tile.MoveToWorld(loc, map);
        _seed.Delete();

        from.SendMessage(
            0x59,
            tilled != null
                ? "Ты сеёшь в подготовленную землю. Осенью такая грядка даст вдвое больше."
                : "Ты сажаешь семена. Грядка пройдёт через сезоны и даст урожай осенью."
        );
    }
}
