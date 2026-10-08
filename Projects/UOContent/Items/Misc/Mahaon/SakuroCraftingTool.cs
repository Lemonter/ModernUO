using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SakuroCraftingTool : Item
{
    [Constructible]
    public SakuroCraftingTool() : base(0x0FB4)
    {
        Weight = 5.0;
        Name = "резцовый набор сакуро";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи голову монстра, из которой хочешь сделать сакуро-украшение.");
        from.Target = new SakuroCraftTarget();
    }
}

public class SakuroCraftTarget : Target
{
    public SakuroCraftTarget() : base(2, false, TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is not MonsterHead head || !head.IsChildOf(from.Backpack))
        {
            from.SendMessage("В твоём рюкзаке нет такой головы монстра.");
            return;
        }

        var knownTypes = Systems.MahaonSoulStones.SakuroBlueprintKnowledge.KnownTypes(from);

        var type = knownTypes.Count > 0
            ? Utility.RandomList(new List<SakuroType>(knownTypes).ToArray())
            : Utility.RandomList(
                SakuroType.Power,
                SakuroType.Agility,
                SakuroType.Wisdom,
                SakuroType.Balance,
                SakuroType.Fox,
                SakuroType.Bear,
                SakuroType.Owl,
                SakuroType.Titan
            );

        head.Delete();
        from.Backpack?.DropItem(new SakuroRing(type));
        from.SendMessage(0x59, $"Ты обрабатываешь голову и получаешь сакуро-перстень: {SakuroTypeRu(type)}.");
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
