using ModernUO.Serialization;
using Server.Systems.MahaonSeasons;
using Server.Targeting;

namespace Server.Items;

/// <summary>Carried sapling — remembers which species it'll grow into. Plant it, and after
/// a couple seasons it becomes a real tree of that exact species.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonSapling : Item
{
    [SerializableField(0)]
    private MahaonTreeSpecies _species;

    [Constructible]
    public MahaonSapling(MahaonTreeSpecies species = MahaonTreeSpecies.Apple) : base(0x0CE9)
    {
        Weight = 1.0;
        _species = species;
        Name = $"саженец ({MahaonTreeSpeciesTable.Get(species).NameRu})";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи свободный участок земли, чтобы посадить саженец.");
        from.Target = new SaplingPlantTarget(this);
    }
}

public class SaplingPlantTarget : Target
{
    private readonly MahaonSapling _sapling;

    public SaplingPlantTarget(MahaonSapling sapling) : base(2, true, TargetFlags.None) => _sapling = sapling;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_sapling.Deleted || targeted is not IPoint3D p)
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

        if (!map.CanFit(loc.X, loc.Y, loc.Z, 16))
        {
            from.SendMessage("Здесь нет места, чтобы посадить дерево.");
            return;
        }

        var planted = new MahaonPlantedSapling(_sapling.Species);
        planted.MoveToWorld(loc, map);
        _sapling.Consume();

        from.SendMessage(0x59, "Ты сажаешь саженец. Через пару сезонов из него вырастет настоящее дерево.");
    }
}
