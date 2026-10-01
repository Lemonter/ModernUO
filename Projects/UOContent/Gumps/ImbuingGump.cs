using Server.Items;
using Server.Network;
using Server.Systems.MahaonImbuing;

namespace Server.Gumps;

/// <summary>Двухшаговый гамп: список свойств → выбор яруса. Состояние (какое свойство
/// сейчас раскрыто) хранится через параметр конструктора и пересылку нового экземпляра
/// при каждом клике — тот же приём, что и у MahaonStatusGump.</summary>
public class ImbuingGump : DynamicGump
{
    private readonly Mobile _player;
    private readonly Item _item;
    private readonly ImbuingProperty _expanded;

    public override bool Singleton => true;

    public ImbuingGump(Mobile player, Item item, ImbuingProperty expanded = null) : base(60, 60)
    {
        _player = player;
        _item = item;
        _expanded = expanded;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (_item?.Deleted != false)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 320, 100, 5054);
            builder.AddHtml(15, 15, 290, 60, "Предмет больше недоступен.");
            return;
        }

        var itemType = ImbuingSystem.GetItemType(_item);

        builder.AddPage();
        builder.AddBackground(0, 0, 420, 480, 5054);
        builder.AddAlphaRegion(10, 10, 400, 460);

        builder.AddHtml(15, 12, 390, 20, $"Наложение чар — {_item.Name ?? _item.GetType().Name}");
        builder.AddHtml(15, 34, 390, 20, $"Навык: {_player.Skills[SkillName.Imbuing].Value:F1}");

        if (_expanded == null)
        {
            BuildPropertyList(ref builder, itemType);
        }
        else
        {
            BuildTierChoice(ref builder, itemType);
        }

        builder.AddButton(15, 445, 4017, 4019, 99);
        builder.AddHtml(50, 447, 200, 20, "Расколдовать вместо этого");
    }

    private void BuildPropertyList(ref DynamicGumpBuilder builder, ImbuingItemType itemType)
    {
        var y = 65;
        byte id = 1;

        foreach (var property in ImbuingPropertyTable.All)
        {
            if (!property.AppliesToItemType(itemType))
            {
                continue;
            }

            var tiers = ImbuingSystem.GetAvailableTiers(_player, property);
            var colorHex = tiers > 0 ? "#7FFF7F" : "#FF6B6B";

            // Чужую печать честнее назвать чужой, чем показывать «0/3» и оставлять игрока
            // качать навык в пустоту.
            var note = ImbuingSystem.CanImbueProperty(_player, property)
                ? $"доступно ярусов: {tiers}/3"
                : "только ремесленник";

            builder.AddButton(15, y, 4005, 4007, id);
            builder.AddHtml(50, y, 330, 20, $"{property.RuName} — {note}", colorHex);

            y += 24;
            id++;

            if (id >= 250) // safety margin — button IDs share the byte range with the fixed 99/98 controls below
            {
                break;
            }
        }
    }

    private void BuildTierChoice(ref DynamicGumpBuilder builder, ImbuingItemType itemType)
    {
        builder.AddHtml(15, 60, 390, 20, $"Свойство: {_expanded.RuName}");

        var unlocked = ImbuingSystem.GetAvailableTiers(_player, _expanded);
        var y = 90;

        var tierNames = new[] { "Низкий ярус", "Средний ярус", "Высокий ярус" };

        for (var tier = 0; tier < 3; tier++)
        {
            var locked = tier >= unlocked;
            var colorHex = locked ? "#FF6B6B" : "#7FFF7F";

            builder.AddHtml(15, y, 390, 20, $"{tierNames[tier]} (+{_expanded.MagnitudePerTier[tier]}, нужен навык {_expanded.MinSkillPerTier[tier]})", colorHex);
            y += 20;

            if (!locked)
            {
                var chance = ImbuingSystem.GetSuccessChance(_player, _expanded, tier) * 100;
                builder.AddHtml(30, y, 380, 20, $"Шанс успеха: {chance:F0}%   Золото: {_expanded.GoldPerTier[tier]}");
                y += 20;

                var matsText = "Материалы: ";
                foreach (var (material, amount) in _expanded.MaterialsPerTier[tier])
                {
                    matsText += $"{material.Name} x{amount}  ";
                }

                builder.AddHtml(30, y, 380, 20, matsText);
                y += 22;

                // Кнопка "Наложить" для этого яруса — id кодирует ярус: 200+tier обычное,
                // 210+tier с использованием порошка закрепления.
                builder.AddButton(30, y, 247, 248, 200 + tier);
                builder.AddHtml(65, y, 150, 20, "Наложить");

                builder.AddButton(220, y, 247, 248, 210 + tier);
                builder.AddHtml(255, y, 150, 20, "Наложить (+порошок)");

                y += 30;
            }
            else
            {
                y += 10;
            }
        }

        builder.AddButton(15, 410, 4014, 4016, 98);
        builder.AddHtml(50, 410, 200, 20, "← Назад к списку");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_item?.Deleted != false)
        {
            return;
        }

        if (info.ButtonID == 98)
        {
            _player.SendGump(new ImbuingGump(_player, _item));
            return;
        }

        if (info.ButtonID == 99)
        {
            var unravelResult = ImbuingSystem.Unravel(_player, _item);
            _player.SendMessage(unravelResult.Success ? 0x59 : 0x22, unravelResult.Message);
            return;
        }

        if (info.ButtonID >= 200 && info.ButtonID < 220 && _expanded != null)
        {
            var usePowder = info.ButtonID >= 210;
            var tier = info.ButtonID - (usePowder ? 210 : 200);

            var result = ImbuingSystem.TryImbue(_player, _item, _expanded, tier, usePowder);
            _player.SendMessage(result.Success ? 0x59 : 0x22, result.Message);

            if (!_item.Deleted)
            {
                _player.SendGump(new ImbuingGump(_player, _item));
            }

            return;
        }

        if (info.ButtonID >= 1 && info.ButtonID < 250 && _expanded == null)
        {
            var itemType = ImbuingSystem.GetItemType(_item);
            byte id = 1;

            foreach (var property in ImbuingPropertyTable.All)
            {
                if (!property.AppliesToItemType(itemType))
                {
                    continue;
                }

                if (id == info.ButtonID)
                {
                    _player.SendGump(new ImbuingGump(_player, _item, property));
                    return;
                }

                id++;
            }
        }
    }
}
