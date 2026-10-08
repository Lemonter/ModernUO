using Server.Network;
using Server.Systems.MahaonLooting;
using Server.Targeting;

namespace Server.Gumps;

/// <summary>
///     Player-facing editor for the extensible autoloot list (see
///     MahaonAutoLootListSystem) — opened from the "Автолут" button on MahaonStatusGump.
///     Each row is a tracked item type: icon, name, an "Enabled" checkbox ("лутать" — kept
///     in the list either way, just paused), and a delete button. "Добавить" starts an
///     item-target cursor; whatever gets targeted is added.
/// </summary>
public class MahaonAutoLootGump : DynamicGump
{
    private readonly Mobile _player;
    private readonly bool _collapsed;

    public override bool Singleton => true;

    public MahaonAutoLootGump(Mobile player, bool collapsed = false) : base(400, 100)
    {
        _player = player;
        _collapsed = collapsed;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (_collapsed)
        {
            BuildCollapsed(ref builder);
            return;
        }

        var list = MahaonAutoLootListSystem.GetList(_player);

        const int width = 320;
        var height = 90 + list.Count * 24 + (list.Count == 0 ? 20 : 0);

        builder.AddPage();
        builder.AddBackground(0, 0, width, height, 5054);
        builder.AddAlphaRegion(10, 10, width - 20, height - 20);

        builder.AddHtml(15, 15, 220, 20, "Автолут");
        builder.AddButton(280, 15, 2437, 2436, 2); // collapse, top-right corner

        builder.AddButton(15, 42, 4005, 4007, 1);
        builder.AddHtml(50, 44, 250, 20, "Добавить предмет (цель)");

        var y = 70;

        for (var i = 0; i < list.Count; i++)
        {
            var entry = list[i];

            builder.AddItem(15, y, entry.ItemID, entry.Hue);
            builder.AddHtml(50, y + 3, 150, 20, entry.DisplayName);
            builder.AddCheckbox(210, y + 3, 0xD2, 0xD3, entry.Enabled, i);
            builder.AddButton(245, y + 3, 4017, 4019, 100 + i);

            y += 24;
        }

        if (list.Count == 0)
        {
            builder.AddHtml(15, y, 280, 20, "Список пуст — нажми «Добавить» и укажи предмет.");
        }
    }

    private void BuildCollapsed(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 150, 40, 5054);
        builder.AddAlphaRegion(5, 5, 140, 30);
        builder.AddButton(5, 8, 4005, 4007, 2); // expand
        builder.AddHtml(35, 10, 110, 20, "Автолут");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (from == null)
        {
            return;
        }

        if (info.ButtonID == 0 || info.ButtonID == 2)
        {
            from.SendGump(new MahaonAutoLootGump(from, !_collapsed));
            return;
        }

        var list = MahaonAutoLootListSystem.GetList(from);

        // Чекбоксы применяются независимо от того, какая кнопка нажата — гамп
        // отправляется целиком на любой ответ. Свёрнутый вид чекбоксов не рисует,
        // так что до сюда только в развёрнутом виде.
        for (var i = 0; i < list.Count; i++)
        {
            list[i].Enabled = info.IsSwitched(i);
        }

        if (info.ButtonID == 1)
        {
            from.SendMessage(0x59, "Цель — предмет, который добавить в автолут.");
            from.Target = new MahaonAutoLootTarget();
            return;
        }

        if (info.ButtonID is >= 100 and < 1000)
        {
            var index = info.ButtonID - 100;
            if (index >= 0 && index < list.Count)
            {
                list.RemoveAt(index);
            }
        }

        from.SendGump(new MahaonAutoLootGump(from));
    }

    private class MahaonAutoLootTarget : Target
    {
        public MahaonAutoLootTarget() : base(12, false, TargetFlags.None)
        {
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Item item)
            {
                from.SendMessage(0x22, "Нужно выбрать предмет.");
                return;
            }

            if (MahaonAutoLootListSystem.AddEntry(from, item))
            {
                from.SendMessage(0x59, $"Добавлено в автолут: {item.Name ?? item.GetType().Name}.");
            }
            else
            {
                from.SendMessage(0x22, "Этот предмет уже в списке автолута.");
            }

            from.SendGump(new MahaonAutoLootGump(from));
        }
    }
}
