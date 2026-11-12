using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Собранная трава — сырьё для ступки травника (DefHerbalism).
///
///     Каждая трава отдельным классом, а не одним предметом с полем «вид»: система крафта
///     ищет ресурсы по типу, и иначе рецепт не смог бы потребовать именно подорожник.
///     Графика у всех одна — пучок сушёных трав, — различаются цветом и названием.
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class MahaonHerbItem : Item
{
    public MahaonHerbItem(string name, int hue, int amount) : base(0x0C42)
    {
        Stackable = true;
        Amount = amount;
        Name = name;
        Hue = hue;
    }

    // Как и реагенты на этом шарде: расходник ничего не весит.
    public override double DefaultWeight => 0;
}

[SerializationGenerator(0, false)]
public partial class MahaonPlantain : MahaonHerbItem
{
    [Constructible]
    public MahaonPlantain(int amount = 1) : base("подорожник", 0x0595, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonYarrow : MahaonHerbItem
{
    [Constructible]
    public MahaonYarrow(int amount = 1) : base("тысячелистник", 0x0481, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonStJohnsWort : MahaonHerbItem
{
    [Constructible]
    public MahaonStJohnsWort(int amount = 1) : base("зверобой", 0x08A5, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonComfrey : MahaonHerbItem
{
    [Constructible]
    public MahaonComfrey(int amount = 1) : base("окопник", 0x0497, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonWormwood : MahaonHerbItem
{
    [Constructible]
    public MahaonWormwood(int amount = 1) : base("полынь", 0x03B2, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonMountainRoot : MahaonHerbItem
{
    [Constructible]
    public MahaonMountainRoot(int amount = 1) : base("горный корень", 0x0455, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonSnowbloom : MahaonHerbItem
{
    [Constructible]
    public MahaonSnowbloom(int amount = 1) : base("снежноцвет", 0x0480, amount)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonMoonwort : MahaonHerbItem
{
    [Constructible]
    public MahaonMoonwort(int amount = 1) : base("лунница", 0x0490, amount)
    {
    }
}
