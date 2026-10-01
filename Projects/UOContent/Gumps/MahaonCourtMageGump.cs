using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonQuests;

namespace Server.Gumps;

/// <summary>
///     Придворный маг's own status gump — same shape as MahaonGuardSergeantGump, minus the
///     rewards shop (not asked for here). Shows rank/ManaMax bonus/quest status up front
///     rather than only as one-off chat lines, matching the fix already applied to Guido's
///     own gump.
/// </summary>
public class MahaonCourtMageGump : DynamicGump
{
    private readonly PlayerMobile _player;
    private readonly Mobile _mage;

    public override bool Singleton => true;

    public MahaonCourtMageGump(PlayerMobile player, Mobile mage) : base(50, 50)
    {
        _player = player;
        _mage = mage;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var rank = CourtMageSystem.GetRank(_player);
        var points = CourtMageSystem.GetPoints(_player);
        var toNext = CourtMageSystem.GetPointsToNextRank(_player);
        var manaBonus = CourtMageSystem.GetManaBonus(_player);
        var questLine = CourtMageQuestSystem.GetStatusLine(_player, _mage);

        const int height = 230;

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);

        builder.AddHtml(15, 15, 290, 20, "Придворный маг");

        builder.AddHtml(15, 40, 290, 20, $"Твоё звание: {CourtMageSystem.RankNames[rank]} ({points} очков)");

        builder.AddHtml(15, 62, 290, 20, $"Бонус к максимальной мане за звание: +{manaBonus}");

        builder.AddHtml(
            15, 84, 290, 20,
            toNext.HasValue
                ? $"До следующего звания: {toNext.Value} очков."
                : "Ты уже достиг предела познаний, что я могу оценить."
        );

        builder.AddHtml(15, 106, 290, 20, questLine);

        builder.AddButton(15, 140, 4005, 4007, 1);
        builder.AddHtml(50, 140, 240, 20, "Задание");

        builder.AddButton(15, 170, 4005, 4007, 0);
        builder.AddHtml(50, 170, 240, 20, "Закрыть");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1)
        {
            CourtMageQuestSystem.Talk(_player, _mage);
        }
    }
}
