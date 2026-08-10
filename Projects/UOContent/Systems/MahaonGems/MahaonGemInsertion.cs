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

        from.SendMessage("Укажи оружие, броню или украшение, куда вставить камень.");
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

        if (targeted is not Item item || item is not (BaseWeapon or BaseArmor or BaseJewel))
        {
            from.SendMessage("Сюда нельзя вставить — только оружие, броня и украшения.");
            return;
        }

        if (!item.IsChildOf(from.Backpack) && item.Parent != from)
        {
            from.SendMessage("Предмет должен быть у тебя в руках или надет.");
            return;
        }

        if (GemSocketingSystem.SocketCount(item) >= 3)
        {
            from.SendMessage("В этот предмет больше не вставить камней — все слоты заняты.");
            return;
        }

        if (!from.CheckSkill(SkillName.Tinkering, 0.0, 100.0))
        {
            from.SendMessage(0x22, "Не получилось — камень треснул при вставке.");
            _gem.Delete();
            return;
        }

        if (GemSocketingSystem.TrySocket(from, item, _gem))
        {
            from.SendMessage(0x59, $"Ты вставляешь {_gem.Name ?? _gem.GetType().Name} в {item.Name ?? "предмет"}.");
            _gem.Delete();
        }
        else
        {
            from.SendMessage("Не получилось.");
        }
    }
}
