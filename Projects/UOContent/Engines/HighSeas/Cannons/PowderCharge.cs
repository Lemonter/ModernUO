/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/PowderCharge.cs).
///
///     Пороховой заряд — первый шаг перезарядки: без него ствол не заряжен, а без заряда нельзя
///     забить снаряд. Тратится в <c>BaseShipCannon.TryCharge</c> / <c>BaseCannon.Charge</c>.
///
///     Отличия: портирован только общий заряд (<c>LightPowderCharge</c>/<c>HeavyPowderCharge</c> в ServUO
///     при <c>EJ</c> заменялись на него); <c>Name</c> по-русски, cliloc 1116160 не тронут.
/// </summary>
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class PowderCharge : Item, ICommodity
{
    [Constructible]
    public PowderCharge() : this(1)
    {
    }

    [Constructible]
    public PowderCharge(int amount) : base(0xA2BE)
    {
        Stackable = true;
        Amount = amount;
        Name = "пороховой заряд";
    }

    public override int LabelNumber => 1116160; // powder charge

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;
}
