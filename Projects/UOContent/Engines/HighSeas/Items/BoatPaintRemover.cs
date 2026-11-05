/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/BoatPaintRemover.cs).
///
///     Смывка краски: снимает с галеры временную краску и возвращает корпус к базовому цвету
///     (<see cref="BaseGalleon.RemovePaint" />). Постоянная краска — это уже базовый цвет корпуса,
///     поэтому она остаётся (как и в ServUO).
///
///     Отличие: в ServUO пользоваться смывкой мог любой, кто стоит на палубе; здесь, как и с краской,
///     требуется уровень капитана — иначе пассажир мог бы испортить чужую окраску.
/// </summary>
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class BoatPaintRemover : Item
{
    [Constructible]
    public BoatPaintRemover() : base(4011)
    {
    }

    public override int LabelNumber => 1116766; // Boat Paint Remover

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
        private readonly BoatPaintRemover _remover;

        public InternalTarget(BoatPaintRemover remover) : base(5, false, TargetFlags.None) => _remover = remover;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not IPoint3D pnt)
            {
                return;
            }

            var galleon = BaseGalleon.FindGalleonAt(pnt, from.Map);

            if (galleon == null || !galleon.Contains(from))
            {
                from.SendLocalizedMessage(1116612); // You must target the main mast of the ship you wish to dye.
                return;
            }

            if (galleon.GetSecurityLevel(from) < SecurityLevel.Captain)
            {
                from.SendMessage(0x22, "Смывать краску может только капитан.");
                return;
            }

            if (galleon.RemovePaint())
            {
                _remover.Delete();
            }
        }
    }
}
