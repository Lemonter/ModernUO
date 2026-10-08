using ModernUO.Serialization;
using Server.Systems.MahaonGems;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GemEncrustingTool : Item
{
    [Constructible]
    public GemEncrustingTool() : base(0x1EBA)
    {
        Weight = 2.0;
        Name = "инкрустационный набор";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (from.Skills[SkillName.Tinkering].Value < 40.0)
        {
            from.SendMessage("Для инкрустации нужно хотя бы 40 Tinkering.");
            return;
        }

        from.SendMessage("Укажи камень, который хочешь вставить.");
        from.Target = new GemPickTarget();
    }
}

public class GemPickTarget : Target
{
    public GemPickTarget() : base(2, false, TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is not Item gem || !gem.IsChildOf(from.Backpack) ||
            GemSocketingSystem.BonusTypeFor(gem.GetType()) == null)
        {
            from.SendMessage("Это не тот камень, который можно вставить.");
            return;
        }

        from.SendMessage("Теперь укажи вещь, куда вставить камень — подойдёт всё, что надеваешь.");
        from.Target = new GemSocketTarget(gem);
    }
}

public class GemSocketTarget : Target
{
    private readonly Item _gem;

    public GemSocketTarget(Item gem) : base(2, false, TargetFlags.None) => _gem = gem;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_gem.Deleted)
        {
            return;
        }

        if (targeted is not Item item || !Systems.MahaonGems.GemSocketingSystem.IsSocketable(item))
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
