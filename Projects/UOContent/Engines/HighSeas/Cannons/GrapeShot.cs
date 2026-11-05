/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/GrapeShot.cs).
///
///     Картечь: единственный боеприпас, который бьёт не одним снарядом, а веером по всем, кто стоит
///     у линии полёта (см. <c>CannonShot.HitShip</c> — 15 дробин по мобам на палубе). По корпусу
///     урон маленький (100–150 против 5000 у ядра). Складывается в стопку, товар.
///
///     Отличия: портирована только общая картечь (<c>LightGrapeshot</c>/<c>HeavyGrapeshot</c> в ServUO
///     при <c>EJ</c> заменялись на неё же); <c>Name</c> по-русски, cliloc 1116030 не тронут.
/// </summary>
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class Grapeshot : Item, ICommodity, ICannonAmmo
{
    [Constructible]
    public Grapeshot() : this(1)
    {
    }

    [Constructible]
    public Grapeshot(int amount) : base(0xA2BF)
    {
        Stackable = true;
        Amount = amount;
        Name = "картечь";
    }

    public override int LabelNumber => 1116030; // grapeshot

    public override double DefaultWeight => 3.5;

    public AmmunitionType AmmoType => AmmunitionType.Grapeshot;

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;
}
