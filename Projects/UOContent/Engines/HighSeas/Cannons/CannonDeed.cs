/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/CannonDeed.cs).
///
///     Дей пушки: двойной клик в рюкзаке → прицел на палубу своего корабля → на выбранном «орудийном
///     пятаке» появляется пушка (<c>BaseGalleon.TryAddCannon</c>). В ServUO дей — единственный способ
///     получить пушку, кроме покупки готового корабля с пушками.
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             <c>BaseGalleon.FindGalleonAt</c> вызывается с картой игрока; прицел проверяет, что
///             выбранная точка лежит на том же корабле, что и до прицела (в ServUO так же).
///         </item>
///         <item>
///             <c>CannonPower.Pumpkin</c> оставлен в перечислении ради соответствия исходнику, но
///             «тыквенной» пушки (ивентовый <c>PumpkinCannon</c>) в этом порте нет — таких деев не
///             существует.
///         </item>
///         <item>
///             Русские имена деев; cliloc-и (1095793/1095794) не тронуты.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.HighSeas;

/// <summary>Класс пушки: по нему дей выбирает, что создать (в ServUO — <c>CannonPower</c>).</summary>
public enum CannonPower
{
    Light,
    Heavy,
    Massive,
    Pumpkin
}

[SerializationGenerator(0, false)]
public abstract partial class ShipCannonDeed : Item
{
    protected ShipCannonDeed() : base(5362)
    {
    }

    public abstract CannonPower CannonType { get; }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var galleon = BaseGalleon.FindGalleonAt(from, from.Map);

        if (galleon == null)
        {
            from.SendLocalizedMessage(1116625); // You must be on the ship to deploy a weapon.
            return;
        }

        if (!galleon.IsOwner(from) && from.AccessLevel < AccessLevel.GameMaster)
        {
            from.SendLocalizedMessage(1116627); // You must be the owner of the ship to do this.
            return;
        }

        from.Target = new InternalTarget(this, galleon);
    }

    private class InternalTarget : Target
    {
        private readonly ShipCannonDeed _deed;
        private readonly BaseGalleon _galleon;

        public InternalTarget(ShipCannonDeed deed, BaseGalleon galleon) : base(2, false, TargetFlags.None)
        {
            _deed = deed;
            _galleon = galleon;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_deed.Deleted || _galleon.Deleted)
            {
                return;
            }

            if (targeted is not IPoint3D point)
            {
                return;
            }

            var pnt = new Point3D(point);

            if (BaseGalleon.FindGalleonAt(pnt, from.Map) != _galleon)
            {
                from.SendLocalizedMessage(1116626); // You must use this on a ship weapon pad.
                return;
            }

            _galleon.TryAddCannon(from, pnt, _deed);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CulverinDeed : ShipCannonDeed
{
    [Constructible]
    public CulverinDeed()
    {
        Hue = 1117;
        Name = "дей кулеврины";
    }

    public override CannonPower CannonType => CannonPower.Light;

    public override int LabelNumber => 1095793;
}

[SerializationGenerator(0, false)]
public partial class CarronadeDeed : ShipCannonDeed
{
    [Constructible]
    public CarronadeDeed()
    {
        Hue = 1118;
        Name = "дей карронады";
    }

    public override CannonPower CannonType => CannonPower.Heavy;

    public override int LabelNumber => 1095794;
}

[SerializationGenerator(0, false)]
public partial class BlundercannonDeed : ShipCannonDeed
{
    [Constructible]
    public BlundercannonDeed()
    {
        Hue = 1126;
        Name = "дей бландерканнона";
    }

    public override CannonPower CannonType => CannonPower.Massive;

    public override int LabelNumber => 1095794;
}

[SerializationGenerator(0, false)]
public partial class LightShipCannonDeed : ShipCannonDeed
{
    [Constructible]
    public LightShipCannonDeed()
    {
        Hue = 1117;
        Name = "дей лёгкой пушки";
    }

    public override CannonPower CannonType => CannonPower.Light;

    public override int LabelNumber => 1095793;
}

[SerializationGenerator(0, false)]
public partial class HeavyShipCannonDeed : ShipCannonDeed
{
    [Constructible]
    public HeavyShipCannonDeed()
    {
        Hue = 1118;
        Name = "дей тяжёлой пушки";
    }

    public override CannonPower CannonType => CannonPower.Heavy;

    public override int LabelNumber => 1095794;
}
