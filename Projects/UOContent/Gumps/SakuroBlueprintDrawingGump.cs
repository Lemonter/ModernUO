using Server.Items;
using Server.Network;
using Server.Systems.MahaonSoulStones;

namespace Server.Gumps;

public class SakuroBlueprintDrawingGump : DynamicGump
{
    private readonly Mobile _player;
    private readonly SakuroType[] _unknown;

    public override bool Singleton => true;

    public SakuroBlueprintDrawingGump(Mobile player) : base(50, 50)
    {
        _player = player;

        var list = SakuroBlueprintKnowledge.UnknownTypes(player);
        _unknown = list.ToArray();
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var height = 60 + _unknown.Length * 25 + 20;

        builder.AddPage();
        builder.AddBackground(0, 0, 340, height, 5054);
        builder.AddAlphaRegion(10, 10, 320, height - 20);
        builder.AddHtml(15, 15, 310, 20, "Начертить чертёж (навсегда, 0.5% от цены):");

        if (_unknown.Length == 0)
        {
            builder.AddHtml(15, 45, 310, 20, "Ты уже знаешь все чертежи сакуро.");
        }

        for (var i = 0; i < _unknown.Length; i++)
        {
            var y = 45 + i * 25;
            var type = _unknown[i];
            var fullPrice = SakuroBlueprintKnowledge.LibrarianPrice[type];
            var drawPrice = System.Math.Max(1, fullPrice / 200); // 0.5%

            builder.AddButton(15, y, 4005, 4007, i + 1);
            builder.AddLabel(45, y, 0x480, $"Сакуро-перстень: {SakuroTypeRu(type)} — {drawPrice} золота");
        }

        builder.AddButton(15, height - 30, 4017, 4019, 0);
        builder.AddHtml(50, height - 30, 100, 20, "Закрыть");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID < 1 || info.ButtonID > _unknown.Length)
        {
            return;
        }

        var type = _unknown[info.ButtonID - 1];
        var fullPrice = SakuroBlueprintKnowledge.LibrarianPrice[type];
        var drawPrice = System.Math.Max(1, fullPrice / 200);

        var backpack = _player.Backpack;

        if (backpack == null || !backpack.ConsumeTotal(typeof(Gold), drawPrice))
        {
            _player.SendMessage(0x22, $"Нужно {drawPrice} золота, чтобы начертить этот чертёж.");
            _player.SendGump(new SakuroBlueprintDrawingGump(_player));
            return;
        }

        SakuroBlueprintKnowledge.Learn(_player, type);
        _player.SendMessage(0x59, $"Ты аккуратно чертишь схему сакуро-перстня: {SakuroTypeRu(type)}. Ты никогда её не забудешь.");
        _player.SendGump(new SakuroBlueprintDrawingGump(_player));
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
