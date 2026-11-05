using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     A climbing vine on the Eodon cliff face, ported from ServUO
///     (Scripts/Services/Expansions/Time Of Legends/Items/ClimbingVine.cs). Double-clicking a vine
///     that has a destination set moves the player to it, which is how the Valley of the Myrmidex
///     walls are scaled. Two vines are placed by <see cref="Initialize" /> on Ter Mur and adopted
///     from the world if a previous save already has them.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ClimbingVine : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _climbLocation;

    [Constructible]
    public ClimbingVine() : this(Point3D.Zero)
    {
    }

    public ClimbingVine(Point3D climbLocation) : base(0x1AA1) => ClimbLocation = climbLocation;

    public override int LabelNumber => 1023307; // vines

    public override void OnDoubleClick(Mobile from)
    {
        // *The vines look as though they may be strong enough to support climbing...*
        from.SayTo(from, 1156290, 1153);

        if (ClimbLocation == Point3D.Zero || !from.InRange(GetWorldLocation(), 2) || Z < from.Z)
        {
            return;
        }

        from.MoveToWorld(ClimbLocation, Map);
    }

    public static ClimbingVine Vine1 { get; set; }
    public static ClimbingVine Vine2 { get; set; }

    private static ClimbingVine FindVine(Point3D p)
    {
        foreach (var vine in Map.TerMur.GetItemsInRange<ClimbingVine>(p, 0))
        {
            return vine;
        }

        return null;
    }

    public static void Initialize()
    {
        if (!Core.TOL)
        {
            return;
        }

        Vine1 = FindVine(new Point3D(687, 1759, 40));
        Vine2 = FindVine(new Point3D(687, 1759, 60));

        if (Vine1 == null)
        {
            Vine1 = new ClimbingVine(new Point3D(679, 1757, 100));
            Vine1.MoveToWorld(new Point3D(678, 1759, 40), Map.TerMur);
        }

        if (Vine2 == null)
        {
            Vine2 = new ClimbingVine(new Point3D(679, 1757, 100));
            Vine2.MoveToWorld(new Point3D(678, 1759, 60), Map.TerMur);
        }
    }
}
