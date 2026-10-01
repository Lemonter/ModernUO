using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonCityHouseSign : Item
{
    [SerializableField(0)]
    private MahaonCityHouse _house;

    [Constructible]
    public MahaonCityHouseSign(MahaonCityHouse house) : base(0xBD2)
    {
        _house = house;
        Movable = false;
        RefreshName();
    }

    public void RefreshName()
    {
        Name = House?.Owner == null
            ? $"{House?.Label} (свободно — кликни, чтобы заявить права)"
            : $"{House?.Label} — дом {House.Owner.Name}";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (House?.Deleted != false)
        {
            from.SendMessage(0x22, "Этот дом больше не существует.");
            return;
        }

        if (House.Owner == null)
        {
            from.SendGump(new MahaonCityHouseClaimGump(House, this));
            return;
        }

        if (House.IsFriend(from))
        {
            from.SendGump(new MahaonCityHouseGump(House, this));
            return;
        }

        from.SendMessage(0x59, $"«{House.Label}» — дом игрока {House.Owner.Name}.");
    }
}

public class MahaonCityHouseClaimGump : StaticGump<MahaonCityHouseClaimGump>
{
    private readonly MahaonCityHouse _house;
    private readonly MahaonCityHouseSign _sign;

    public override bool Singleton => true;
    protected override bool Cached => false;

    public MahaonCityHouseClaimGump(MahaonCityHouse house, MahaonCityHouseSign sign) : base(100, 100)
    {
        _house = house;
        _sign = sign;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 320, 150, 5054);
        builder.AddAlphaRegion(10, 10, 300, 130);

        builder.AddHtml(20, 15, 280, 20, $"«{_house.Label}» — дом свободен");
        builder.AddHtml(20, 42, 280, 40, "Заявить права на этот дом? Ты станешь владельцем и сможешь управлять доступом друзей.");

        builder.AddButton(20, 95, 4005, 4007, 1);
        builder.AddHtml(55, 97, 200, 20, "Заявить права");

        builder.AddButton(20, 120, 4017, 4019, 0);
        builder.AddHtml(55, 122, 200, 20, "Отмена");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _house?.Deleted != false)
        {
            return;
        }

        if (info.ButtonID == 1 && _house.Owner == null)
        {
            _house.Owner = from;
            _sign.RefreshName();
            from.SendMessage(0x59, $"Теперь ты владелец дома «{_house.Label}».");
        }
    }
}
