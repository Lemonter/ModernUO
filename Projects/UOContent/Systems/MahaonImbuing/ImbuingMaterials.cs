using ModernUO.Serialization;

namespace Server.Items;

/// <summary>Наложение чар — базовый материал. Падает с обычных монстров, самый частый
/// расходник для младших ярусов интенсивности.</summary>
[SerializationGenerator(0, false)]
public partial class MagicalResidue : Item
{
    [Constructible]
    public MagicalResidue(int amount = 1) : base(0xF8F)
    {
        Stackable = true;
        Amount = amount;
        Weight = 0.1;
        Hue = 0x481;
        Name = "магический остаток";
    }
}

/// <summary>Редкий материал — падает только с сильных существ (высокий Fame). Нужен для
/// верхних ярусов интенсивности наложения.</summary>
[SerializationGenerator(0, false)]
public partial class RelicFragment : Item
{
    [Constructible]
    public RelicFragment(int amount = 1) : base(0xF80)
    {
        Stackable = true;
        Amount = amount;
        Weight = 0.1;
        Hue = 0x489;
        Name = "осколок реликвии";
    }
}

/// <summary>Получается только через расколдовывание (Unravel) ненужных магических вещей —
/// нет прямого дропа, замыкает материальный цикл.</summary>
[SerializationGenerator(0, false)]
public partial class EnchantedEssence : Item
{
    [Constructible]
    public EnchantedEssence(int amount = 1) : base(0x4077)
    {
        Stackable = true;
        Amount = amount;
        Weight = 0.1;
        Hue = 0x48E;
        Name = "зачарованная эссенция";
    }
}

/// <summary>Опциональный расходник — предотвращает потерю прочности предмета при
/// наложении чар. Крафтится алхимиком (см. ImbuingCraftHooks).</summary>
[SerializationGenerator(0, false)]
public partial class PowderOfFortifying : Item
{
    [Constructible]
    public PowderOfFortifying(int amount = 1) : base(0x26B8)
    {
        Stackable = true;
        Amount = amount;
        Weight = 0.1;
        Hue = 0x47E;
        Name = "порошок закрепления";
    }
}
