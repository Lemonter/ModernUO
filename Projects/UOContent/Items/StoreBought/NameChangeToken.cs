using ModernUO.Serialization;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     ServUO-интерфейс IPromotionalToken в этом репозитории отсутствует; имя и подпись
///     выводятся напрямую. Проверка имени вынесена в NameVerification.ValidatePlayerName.
/// </summary>
[SerializationGenerator(0, false)]
public partial class NameChangeToken : Item
{
    [Constructible]
    public NameChangeToken() : base(0x2AAA)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1070997; // a promotional token

    public TextDefinition ItemName => 1075247; // name change

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1070998, ItemName.GetValue()); // Use this to redeem<br>Your ~1_PROMO~ : name change
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        if (from is not PlayerMobile pm)
        {
            return;
        }

        from.SendGump(new NameChangeConfirmGump(pm, this));
    }
}

public class NameChangeConfirmGump : StaticGump<NameChangeConfirmGump>
{
    private readonly NameChangeToken _token;
    private readonly PlayerMobile _pm;

    public override bool Singleton => true;

    public NameChangeConfirmGump(PlayerMobile pm, NameChangeToken token) : base(100, 100)
    {
        _pm = pm;
        _token = token;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 291, 179, 9200);
        builder.AddImageTiled(5, 5, 281, 20, 2702);
        builder.AddImageTiled(5, 30, 281, 120, 2702);

        builder.AddHtmlLocalized(8, 5, 279, 20, 1075241, 0x7FFF); // Change your character's name

        // Enter your new name (16 characters max, English characters only):
        builder.AddHtmlLocalized(8, 30, 279, 16, 1075242, 0x7FFF);
        builder.AddImageTiled(8, 50, 276, 20, 0xDB0);
        builder.AddTextEntry(9, 50, 275, 20, 0, 0, "");

        // Clicking OK will permanently change your character's name...
        builder.AddHtmlLocalized(8, 70, 279, 80, 1075561, 0x7FFF);

        builder.AddButton(5, 152, 0xFB1, 0xFB3, 0);
        builder.AddHtmlLocalized(40, 152, 100, 20, 1011012, 0x7FFF); // CANCEL

        builder.AddButton(126, 152, 0xFB7, 0xFB9, 1);
        builder.AddHtmlLocalized(160, 152, 120, 20, 1075243, 0x7FFF); // Change my name!
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1 || !_token.IsChildOf(_pm.Backpack))
        {
            return;
        }

        var text = info.GetTextEntry(0);

        if (string.IsNullOrEmpty(text))
        {
            _pm.SendLocalizedMessage(1075245); // Your name cannot be blank.
        }
        else if (text.Length > 16)
        {
            _pm.SendLocalizedMessage(1075244); // That name is too long.
        }
        else if (!NameVerification.ValidatePlayerName(text))
        {
            _pm.SendLocalizedMessage(1075246); // That name is not valid.
        }
        else
        {
            _pm.Name = text;
            _token.Delete();

            _pm.SendMessage("Вы успешно изменили имя.");
        }
    }
}
