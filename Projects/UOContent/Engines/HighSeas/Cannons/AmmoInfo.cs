/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/AmmoInfo.cs).
///
///     Каталог боеприпасов для пушек: тип боеприпаса (<see cref="AmmunitionType" />), урон, разброс
///     по ширине и стихии. Игровая логика берёт отсюда и цифры урона, и тип урона для
///     <c>AOS.Damage</c>, поэтому таблица — единственное место, где эти числа заданы.
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             <c>AmmoInfo.Initialize()</c> с <c>Dictionary&lt;Type, AmmoInfo&gt;</c> заменён статическим
///             массивом и поиском по <see cref="AmmunitionType" />: типов боеприпаса всего четыре,
///             а <c>Type</c> в ServUO использовался только затем, чтобы найти запись по «типу
///             загруженного снаряда»; здесь на снаряде уже стоит <see cref="ICannonAmmo.AmmoType" />.
///         </item>
///         <item>
///             Ветки <c>Core.EJ</c> и до-<c>EJ</c> свёрнуты в одну: <c>Light*</c>/<c>Heavy*</c> варианты
///             снарядов в ServUO при <c>EJ</c> всё равно заменялись на общие (<c>Replacer.Replace</c>),
///             поэтому портированы только общие.
///         </item>
///         <item>
///             Названия боеприпасов — русские строки (в ServUO — cliloc-и 1095804, 1095741, 1149633,
///             1149634). Cliloc-и снарядов на самих предметах не тронуты.
///         </item>
///     </list>
/// </summary>
namespace Server.Engines.HighSeas;

/// <summary>Снаряд, который можно зарядить в пушку.</summary>
public interface ICannonAmmo
{
    AmmunitionType AmmoType { get; }
}

/// <summary>Тип заряда в стволе (в ServUO — <c>AmmunitionType</c>).</summary>
public enum AmmunitionType
{
    Empty,
    Cannonball,
    Grapeshot,
    FrostCannonball,
    FlameCannonball
}

/// <summary>Характеристики одного типа боеприпаса.</summary>
public sealed class AmmoInfo
{
    private static readonly AmmoInfo[] _infos =
    [
        // ServUO (EJ): ядро и картечь общие для всех пушек; огненное и ледяное — «особые» снаряды.
        new(AmmunitionType.Cannonball, "ядро", 5000, 5000, 3, 100, 0, 0, 0, 0),
        new(AmmunitionType.Grapeshot, "картечь", 100, 150, 3, 100, 0, 0, 0, 0),
        new(AmmunitionType.FrostCannonball, "ледяное ядро", 30, 50, 3, 50, 0, 50, 0, 0),
        new(AmmunitionType.FlameCannonball, "огненное ядро", 5000, 5000, 3, 50, 50, 0, 0, 0)
    ];

    private AmmoInfo(
        AmmunitionType ammoType, string name, int minDamage, int maxDamage, int lateralOffset,
        int physical, int fire, int cold, int poison, int energy
    )
    {
        AmmoType = ammoType;
        Name = name;
        MinDamage = minDamage;
        MaxDamage = maxDamage;
        LateralOffset = lateralOffset;
        PhysicalDamage = physical;
        FireDamage = fire;
        ColdDamage = cold;
        PoisonDamage = poison;
        EnergyDamage = energy;
    }

    public AmmunitionType AmmoType { get; }

    /// <summary>Название для подсказки и окна пушки (в ServUO — <c>TextDefinition Name</c>).</summary>
    public string Name { get; }

    public int MinDamage { get; }

    public int MaxDamage { get; }

    /// <summary>Через сколько клеток полёта луч расширяется на клетку в сторону.</summary>
    public int LateralOffset { get; }

    /// <summary>
    ///     Нужна ли снаряду твёрдая поверхность под целью (в ServUO — <c>RequiresSurface</c>).
    ///     Ни одному снаряду High Seas она не нужна, поэтому всегда false.
    /// </summary>
    public bool RequiresSurface => false;

    public int PhysicalDamage { get; }

    public int FireDamage { get; }

    public int ColdDamage { get; }

    public int PoisonDamage { get; }

    public int EnergyDamage { get; }

    public int GetDamage() => Utility.RandomMinMax(MinDamage, MaxDamage);

    /// <summary>Новый предмет-снаряд этого типа (для выгрузки/возврата заряда).</summary>
    public Item CreateAmmo(int amount = 1) => AmmoType switch
    {
        AmmunitionType.Cannonball      => new Cannonball(amount),
        AmmunitionType.Grapeshot       => new Grapeshot(amount),
        AmmunitionType.FrostCannonball => new FrostCannonball(amount),
        AmmunitionType.FlameCannonball => new FlameCannonball(amount),
        _                              => null
    };

    public static AmmoInfo GetAmmoInfo(AmmunitionType ammoType)
    {
        for (var i = 0; i < _infos.Length; i++)
        {
            if (_infos[i].AmmoType == ammoType)
            {
                return _infos[i];
            }
        }

        return null;
    }

    /// <summary>Название заряда для подсказок; для пустого ствола — «нет».</summary>
    public static string GetAmmoName(AmmunitionType ammoType) => GetAmmoInfo(ammoType)?.Name ?? "нет";
}
