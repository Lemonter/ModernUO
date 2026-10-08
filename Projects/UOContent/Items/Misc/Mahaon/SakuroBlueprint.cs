using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SakuroBlueprint : Item
{
    [SerializableField(0)]
    private SakuroType _type;

    [Constructible]
    public SakuroBlueprint(SakuroType type) : base(0x0FF0)
    {
        _type = type;
    }

    public override double DefaultWeight => 1.0;

    public override string DefaultName => $"чертёж: сакуро-перстень «{SakuroTypeRu(_type)}»";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (Systems.MahaonSoulStones.SakuroBlueprintKnowledge.Knows(from, _type))
        {
            from.SendMessage("Ты уже знаешь этот чертёж.");
            return;
        }

        Systems.MahaonSoulStones.SakuroBlueprintKnowledge.Learn(from, _type);
        from.SendMessage(0x59, $"Ты навсегда изучаешь, как крафтить сакуро-перстень «{SakuroTypeRu(_type)}».");
        Delete();
    }
    private static string SakuroTypeRu(SakuroType type) => type switch
    {
        SakuroType.Power   => "Сила",
        SakuroType.Agility => "Ловкость",
        SakuroType.Wisdom  => "Мудрость",
        SakuroType.Balance => "Баланс",
        SakuroType.Fox     => "Лис",
        SakuroType.Bear    => "Медведь",
        SakuroType.Owl     => "Сова",
        SakuroType.Titan   => "Титан",
        _                  => type.ToString()
    };
}
