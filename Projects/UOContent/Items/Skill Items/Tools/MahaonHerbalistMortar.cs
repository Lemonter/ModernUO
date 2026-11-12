using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Ступка травника — инструмент для DefHerbalism. Отдельный предмет от алхимической
///     ступки: у каждого ремесла свой инструмент и своё меню, и двойной клик не должен
///     гадать, что из двух ты имел в виду. Цвет зелёный, чтобы их не путать в сумке.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonHerbalistMortar : BaseTool
{
    [Constructible]
    public MahaonHerbalistMortar() : base(0xE9B)
    {
        Name = "ступка травника";
        Hue = 0x0595;
    }

    [Constructible]
    public MahaonHerbalistMortar(int uses) : base(uses, 0xE9B)
    {
        Name = "ступка травника";
        Hue = 0x0595;
    }

    public override double DefaultWeight => 1.0;

    public override CraftSystem CraftSystem => DefHerbalism.CraftSystem;
}
