/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/CannonBall.cs).
///
///     Пушечное ядро — обычный боеприпас пушек: 5000 урона по корпусу и по мобам, чисто физический.
///     Складывается в стопку, продаётся как товар (<see cref="ICommodity" />).
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             Портировано только общее ядро: <c>LightCannonball</c>/<c>HeavyCannonball</c> в ServUO при
///             <c>Core.EJ</c> заменялись на <c>Cannonball</c> через <c>Replacer.Replace</c>, отдельных
///             боеприпасов у пушек High Seas больше нет.
///         </item>
///         <item>
///             <c>ICommodity</c> в этом движке требует <c>DescriptionNumber</c> (int), а не
///             <c>TextDefinition</c> — отдаём <c>LabelNumber</c>.
///         </item>
///         <item>
///             <c>Name</c> задан по-русски; <c>LabelNumber</c> (cliloc 1116266) оставлен как в ServUO.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class Cannonball : Item, ICommodity, ICannonAmmo
{
    [Constructible]
    public Cannonball() : this(1)
    {
    }

    [Constructible]
    public Cannonball(int amount) : this(amount, 16932)
    {
    }

    [Constructible]
    public Cannonball(int amount, int itemID) : base(itemID)
    {
        Stackable = true;
        Amount = amount;
        Name = "пушечное ядро";
    }

    public override int LabelNumber => 1116266; // cannonball

    public override double DefaultWeight => 1.0;

    public virtual AmmunitionType AmmoType => AmmunitionType.Cannonball;

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;
}
