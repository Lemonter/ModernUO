using Server.Systems.MahaonGems;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Shared logic behind every gem's own OnDoubleClick (see GemDirectInsert.cs) — double-
///     click the gem itself, target the item, done. Replaces having to go through
///     GemEncrustingTool's separate 3-step flow for the common case; the tool still works
///     too (nothing here removes it), this is just the simpler direct path the gems
///     themselves now offer. Same skill check and crack-on-fail as the tool version, for
///     balance — no free pass just because it's a shorter flow.
/// </summary>
public static class MahaonGemInsertion
{
    public static void StartInsert(Mobile from, Item gem)
    {
        if (!gem.IsChildOf(from.Backpack))
        {
            from.SendMessage("Камень должен быть у тебя в рюкзаке.");
            return;
        }

        from.SendMessage("Укажи вещь, куда вставить камень — подойдёт всё, что надеваешь.");
        from.Target = new MahaonGemDirectSocketTarget(gem);
    }
}

public class MahaonGemDirectSocketTarget : Target
{
    private readonly Item _gem;

    public MahaonGemDirectSocketTarget(Item gem) : base(2, false, TargetFlags.None) => _gem = gem;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_gem.Deleted)
        {
            return;
        }

        // Через GemSocketingSystem.IsSocketable, а не по трём базовым классам.
        //
        // Здесь и была причина, по которой робу инкрустировать не получалось. Общую
        // проверку давно переписали на «есть слой экипировки» — именно затем, чтобы
        // принимались роба, плащ, шляпа, сапоги, штаны, пояс, колчан, талисман и книга
        // заклинаний, ни один из которых не наследует BaseWeapon/BaseArmor/BaseJewel. Но
        // правка легла только в тулзу (GemEncrustingTool), а сюда — нет. А сюда и ходят
        // чаще: это прямой путь, двойной клик по самому камню, ради которого трёхшаговый
        // флоу тулзы и заводился как запасной.
        //
        // Дублировать условие нельзя в принципе: ровно от дубля оно и разъехалось. Один
        // источник истины — IsSocketable.
        if (targeted is not Item item || !GemSocketingSystem.IsSocketable(item))
        {
            from.SendMessage("Сюда нельзя вставить — только то, что надевают.");
            return;
        }

        if (!item.IsChildOf(from.Backpack) && item.Parent != from)
        {
            from.SendMessage("Предмет должен быть у тебя в руках или надет.");
            return;
        }

        if (GemSocketingSystem.SocketCount(item) >= GemSocketingSystem.MaxSocketsPerItem)
        {
            from.SendMessage("В этот предмет больше не вставить камней — все слоты заняты.");
            return;
        }

        GemSocketingSystem.BeginSocket(from, item, _gem);
    }
}
