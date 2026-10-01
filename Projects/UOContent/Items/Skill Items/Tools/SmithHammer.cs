using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

// Перековка отсюда убрана.
//
// Как было. Двойной клик открывал выбор «ковать обычно» или «перековать»; перековка
// просила навести на вещь, потом на стопку из десяти слитков одного металла, и ставила
// металл через MahaonMetalTracker. Это был ЕДИНСТВЕННЫЙ способ получить вещь из нашего
// металла: меню крафта про MahaonIngot не знало вовсе и видело только ванильное железо,
// которым торгуют лавки.
//
// Почему убрано. Металл теперь выбирается прямо в меню крафта, как любой другой материал
// (MahaonCraftMetals), и вещь получает его сразу при ковке. Второй путь к тому же
// результату — лишний: он требовал сначала сковать заведомо ненужную железную вещь, а
// потом извести на неё ещё десять слитков. Владелец шарда решил оставить один путь.
//
// Молот снова обычный инструмент: двойной клик открывает меню кузнечного дела, и ничего
// больше. Гамп выбора, оба таргета и IngotCost удалены целиком — BaseTool.OnDoubleClick
// делает ровно то, что нужно, и перекрывать его нечем.
[Flippable(0x13E3, 0x13E4)]
[SerializationGenerator(0, false)]
public partial class SmithHammer : BaseTool
{
    [Constructible]
    public SmithHammer() : base(0x13E3) => Layer = Layer.OneHanded;

    [Constructible]
    public SmithHammer(int uses) : base(uses, 0x13E3) => Layer = Layer.OneHanded;

    public override double DefaultWeight => 8.0;

    public override CraftSystem CraftSystem => DefBlacksmithy.CraftSystem;
}
