using ModernUO.Serialization;
using Server.Engines.MLQuests.Gumps;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     ServUO-гумп RaceChangeConfirmGump конфликтует с гуммом системы ML-квестов, поэтому
///     токен использует её IRaceChanger/RaceChangeConfirmGump. Собственный enum GumpMode и
///     HeritageQuester/HeritagePacket в репозитории отсутствуют, их роль выполняет выбор расы
///     в собственном гумпе токена.
/// </summary>
[SerializationGenerator(0, false)]
public partial class RaceChangeToken : Item, IRaceChanger
{
    [Constructible]
    public RaceChangeToken() : base(0x2AAA)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1070997; // a promotional token

    public TextDefinition ItemName => 1113656; // race change

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1070998, ItemName.GetValue()); // Use this to redeem<br>Your ~1_PROMO~ : race change
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        if (from is PlayerMobile pm)
        {
            pm.SendGump(new RaceSelectGump(pm, this));
        }
    }

    public bool CheckComplete(PlayerMobile pm)
    {
        if (Deleted)
        {
            return false;
        }

        if (!IsChildOf(pm.Backpack))
        {
            pm.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return false;
        }

        return true;
    }

    public void ConsumeNeeded(PlayerMobile pm) => Delete();

    public void OnCancel(PlayerMobile pm)
    {
    }

    private class RaceSelectGump : StaticGump<RaceSelectGump>
    {
        private readonly PlayerMobile _pm;
        private readonly RaceChangeToken _token;

        public override bool Singleton => true;

        public RaceSelectGump(PlayerMobile pm, RaceChangeToken token) : base(100, 100)
        {
            _pm = pm;
            _token = token;
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 291, 159, 9200);
            builder.AddImageTiled(5, 5, 281, 20, 2702);
            builder.AddImageTiled(5, 30, 281, 100, 2702);

            // Change your character's race.
            builder.AddHtmlLocalized(8, 5, 279, 20, 1113641, 0x7FFF);

            builder.AddButton(5, 132, 0xFB1, 0xFB3, 0);
            builder.AddHtmlLocalized(40, 132, 100, 20, 1011012, 0x7FFF); // CANCEL

            var y = 35;
            var button = 2;

            if (_pm.Race != Race.Human)
            {
                builder.AddButton(8, y, 0xFB7, 0xFB9, button++);
                builder.AddHtmlLocalized(100, y, 150, 20, 1111906, 0x7FFF); // Make me a human!
                y += 25;
            }

            if (_pm.Race != Race.Elf)
            {
                builder.AddButton(8, y, 0xFB7, 0xFB9, button++);
                builder.AddHtmlLocalized(100, y, 150, 20, 1111910, 0x7FFF); // Make me an elf!
                y += 25;
            }

            if (_pm.Race != Race.Gargoyle)
            {
                builder.AddButton(8, y, 0xFB7, 0xFB9, button);
                builder.AddHtmlLocalized(100, y, 150, 20, 1111900, 0x7FFF); // Make me a gargoyle!
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_token.Deleted || !_token.IsChildOf(_pm.Backpack))
            {
                return;
            }

            if (info.ButtonID < 2)
            {
                return;
            }

            var races = new System.Collections.Generic.List<Race>(3);

            if (_pm.Race != Race.Human)
            {
                races.Add(Race.Human);
            }

            if (_pm.Race != Race.Elf)
            {
                races.Add(Race.Elf);
            }

            if (_pm.Race != Race.Gargoyle)
            {
                races.Add(Race.Gargoyle);
            }

            var index = info.ButtonID - 2;

            if (index < 0 || index >= races.Count)
            {
                return;
            }

            RaceChangeConfirmGump.DisplayTo(_pm, _token, races[index]);
        }
    }
}
