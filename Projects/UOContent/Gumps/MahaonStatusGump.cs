using Server.Network;
using Server.Systems.MahaonCombat;

namespace Server.Gumps;

/// <summary>
///     Always with the player once logged in (see PlayerMobile.OnLogin), positioned right
///     next to MahaonCombatMenuGump — same "never really closes" behavior (see that
///     gump's own doc comment for why). Hunger used to live inside the combat menu; moved
///     out here because the combat gump was getting crowded and hunger isn't really a
///     combat concern — this is the natural home for other non-combat status info later
///     too (buffs, whatever else needs a permanent-ish readout).
/// </summary>
public class MahaonStatusGump : DynamicGump
{
    private readonly Mobile _player;
    private readonly bool _collapsed;

    public override bool Singleton => true;

    public MahaonStatusGump(Mobile player, bool collapsed = false) : base(370, 50)
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

        var hasBonus = HungerSystem.HasActiveBonus(_player);
        var isBleeding = BleedingSystem.IsBleeding(_player);
        var fracture = BoneFractureSystem.GetActiveFracture(_player);

        var extraRows = (hasBonus ? 1 : 0) + (isBleeding ? 1 : 0) + (fracture != null ? 1 : 0);
        var height = 130 + extraRows * 22;

        builder.AddPage();
        builder.AddBackground(0, 0, 300, height, 5054);
        builder.AddAlphaRegion(10, 10, 280, height - 20);

        builder.AddHtml(15, 15, 250, 20, "Состояние");
        builder.AddButton(260, 15, 2437, 2436, 1); // collapse, top-right corner

        var desiredFood = HungerSystem.GetDesiredFood(_player);
        builder.AddItem(15, 45, HungerSystem.GetGraphic(desiredFood));
        builder.AddHtml(50, 50, 230, 20, $"Хочется съесть: {HungerSystem.GetFoodNameRu(desiredFood)}");

        var y = 75;

        if (hasBonus)
        {
            var left = HungerSystem.GetBonusTimeLeft(_player);
            builder.AddHtml(50, y, 230, 20, $"Бонус +10 к статам ещё {left.Hours}ч {left.Minutes}м");
            y += 22;
        }

        if (isBleeding)
        {
            builder.AddHtml(
                15, y, 265, 20,
                $"Кровотечение! Ещё ~{BleedingSystem.TicksLeft(_player)} сек, лечится бинтом.", "C40000"
            );
            y += 22;
        }

        if (fracture != null)
        {
            var left = BoneFractureSystem.GetTimeLeft(_player);
            builder.AddHtml(
                15, y, 265, 20,
                $"Перелом ({BoneFractureSystem.RuLocationName(fracture.Value)}) — срастётся через {left.Minutes}м {left.Seconds}с.",
                "C48000"
            );
            y += 22;
        }

        builder.AddButton(15, y, 4005, 4007, 2);
        builder.AddHtml(50, y + 2, 230, 20, "Автолут");
    }

    private void BuildCollapsed(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 150, 40, 5054);
        builder.AddAlphaRegion(5, 5, 140, 30);
        builder.AddButton(5, 8, 4005, 4007, 1); // expand
        builder.AddHtml(35, 10, 110, 20, "Состояние");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0 || info.ButtonID == 1)
        {
            _player.SendGump(new MahaonStatusGump(_player, !_collapsed));
            return;
        }

        if (info.ButtonID == 2)
        {
            _player.SendGump(new MahaonAutoLootGump(_player));
        }
    }
}
