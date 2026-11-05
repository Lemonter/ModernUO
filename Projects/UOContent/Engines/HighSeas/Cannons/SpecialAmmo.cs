/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/SpecialAmmo.cs
///     и CannonBall.cs — ветка особых снарядов).
///
///     «Особые» боеприпасы High Seas: ледяное и огненное ядро. От обычного ядра отличаются стихией
///     урона и цифрами (<see cref="AmmoInfo" />): огненное бьёт как ядро (5000), но половиной урона
///     огнём; ледяное — слабое (30–50) и половиной урона холодом.
///
///     В ServUO файл <c>SpecialAmmo.cs</c> целиком закомментирован (это были <c>Light*</c>/<c>Heavy*</c>
///     варианты дробовых/зажигательных снарядов, вырезанные с приходом <c>Core.EJ</c>), а сами особые
///     ядра лежали в <c>CannonBall.cs</c>. Здесь они собраны в этом файле — по смыслу его имени.
///
///     Meta: <c>ICommodity</c> в этом движке требует <c>DescriptionNumber</c> (int); имена — русские,
///     cliloc-и предметов (1116267, 1116759) не тронуты.
/// </summary>
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class FrostCannonball : Cannonball, ICommodity
{
    [Constructible]
    public FrostCannonball() : this(1)
    {
    }

    [Constructible]
    public FrostCannonball(int amount) : this(amount, 16939)
    {
    }

    [Constructible]
    public FrostCannonball(int amount, int itemID) : base(amount, itemID)
    {
        Name = "ледяное ядро";
    }

    public override int LabelNumber => 1116267; // frost cannonball

    public override AmmunitionType AmmoType => AmmunitionType.FrostCannonball;

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class FlameCannonball : Cannonball, ICommodity
{
    [Constructible]
    public FlameCannonball() : this(1)
    {
    }

    [Constructible]
    public FlameCannonball(int amount) : this(amount, 17601)
    {
    }

    [Constructible]
    public FlameCannonball(int amount, int itemID) : base(amount, itemID)
    {
        Name = "огненное ядро";
    }

    public override int LabelNumber => 1116759; // flame cannonball

    public override AmmunitionType AmmoType => AmmunitionType.FlameCannonball;

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;
}
