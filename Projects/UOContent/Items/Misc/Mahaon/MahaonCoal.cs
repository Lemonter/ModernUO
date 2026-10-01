using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonCoal : Item
{
    /// <summary>
    ///     Через DefaultWeight, а не присваиванием Weight в конструкторе.
    ///
    ///     Разница в том, что достаётся уже лежащему в мире углю. Явного веса он никогда не
    ///     получал, то есть берёт его отсюда — значит новый вес подхватят и старые стопки.
    ///     Присваивание же в конструкторе поправило бы только то, что выкопают после
    ///     перезапуска, а накопленное у игроков так и осталось бы тяжёлым.
    /// </summary>
    public override double DefaultWeight => 0.1;

    [Constructible]
    public MahaonCoal(int amount = 1) : base(0x19B8)
    {
        Stackable = true;
        Amount = amount;
        Hue = 0x966;
        Name = "уголь";
    }
}
