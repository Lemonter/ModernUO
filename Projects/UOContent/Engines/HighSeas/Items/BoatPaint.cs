/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/BoatPaint.cs).
///
///     Банка корабельной краски: наводится на корпус галеры и наносит слой краски
///     (<see cref="BaseGalleon.TryPaintBoat" />) либо постоянную краску
///     (<see cref="BaseGalleon.TryPermanentPaintBoat" />).
///
///     Адаптация: в ServUO <c>PaintComponents</c> перекрашивал ещё и фикстуры палубы, которых в этом
///     этапе нет, поэтому красится только сам корпус (Hue мульти). Из-за этого нет и проверки
///     «целься в грот-мачту» по отдельным тайлам — проверяется попадание в футпринт корабля.
/// </summary>
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class BoatPaint : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _permanent;

    [Constructible]
    public BoatPaint(int hue = 0) : base(4011) => Hue = hue;

    public override int LabelNumber => 1116236; // Boat Paint

    public override double DefaultWeight => 10.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.Target = new InternalTarget(this);
        from.SendLocalizedMessage(1116613); // Select the main mast of the ship you wish to dye.
    }

    private class InternalTarget : Target
    {
        private readonly BoatPaint _paint;

        public InternalTarget(BoatPaint paint) : base(5, false, TargetFlags.None) => _paint = paint;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D pnt)
            {
                return;
            }

            var galleon = BaseGalleon.FindGalleonAt(pnt, from.Map);

            if (galleon == null || !galleon.Contains(from))
            {
                return;
            }

            if (galleon.GetSecurityLevel(from) < SecurityLevel.Captain)
            {
                from.SendMessage(0x22, "Красить корабль может только капитан.");
                return;
            }

            if (!galleon.Contains(pnt))
            {
                from.SendLocalizedMessage(1116612); // You must target the main mast of the ship you wish to dye.
                return;
            }

            var applied = _paint.Permanent
                ? galleon.TryPermanentPaintBoat(from, _paint.Hue)
                : galleon.TryPaintBoat(from, _paint.Hue);

            if (applied)
            {
                _paint.Delete();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class PermanentBoatPaint : BoatPaint
{
    [Constructible]
    public PermanentBoatPaint() : this(Utility.RandomMinMax(1954, 1997))
    {
    }

    [Constructible]
    public PermanentBoatPaint(int hue) : base(hue) => Permanent = true;

    public override int LabelNumber => 1116768; // Permanent Boat Paint

    public static PermanentBoatPaint DropRandom() => new(Utility.RandomMinMax(1954, 1997));
}
