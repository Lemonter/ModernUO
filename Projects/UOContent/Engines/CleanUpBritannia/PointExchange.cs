using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CleanUpBritannia;

/// <summary>
///     Камень обмена очками Clean Up Britannia: позволяет переложить очки персонажа на аккаунт
///     и забрать их обратно.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PointExchanceStone : Item
{
    [Constructible]
    public PointExchanceStone() : base(0xEDD)
    {
        Hue = 1037;
        Movable = false;
    }

    public override int LabelNumber => 1158449; // Cleanup Point Exchange

    public override bool ForceShowProperties => true;

    public override void OnDoubleClick(Mobile m)
    {
        if (!CleanUpBritanniaData.Enabled)
        {
            base.OnDoubleClick(m);
            return;
        }

        if (!m.InRange(GetWorldLocation(), 3))
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (m is PlayerMobile pm)
        {
            pm.SendGump(new ExchangeGump(pm));
        }
    }

    private class ExchangeGump : DynamicGump
    {
        private const int Highlight = 0x35;

        public ExchangeGump(PlayerMobile user) : base(100, 100) => User = user;

        public PlayerMobile User { get; }

        public override bool Singleton => true;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var points = CleanUpBritanniaData.GetPoints(User);
            var accountPoints = (int)CleanUpBritanniaData.GetPointsFromExchange(User);

            builder.AddPage();

            builder.AddBackground(0, 0, 370, 245, 0x53);
            builder.AddHtmlLocalized(0, 15, 370, 20, 1158449, 0x7FFF); // Cleanup Point Exchange

            // Your Cleanup Point Balance: ~1_VALUE~
            builder.AddHtmlLocalized(15, 40, 340, 20, 1158454, $"{points:N0}", 0x7FFF);
            // Points Currently in Exchange: ~1_VALUE~
            builder.AddHtmlLocalized(15, 70, 340, 20, 1158455, $"{accountPoints:N0}", 0x7FFF);

            builder.AddButton(20, 105, 4005, 4006, 1);
            builder.AddHtmlLocalized(60, 105, 100, 20, 1158466, 0x7FFF); // Deposit Points

            builder.AddButton(20, 127, 4005, 4006, 2);
            builder.AddHtmlLocalized(60, 127, 100, 20, 1158465, 0x7FFF); // Withdraw Points

            // You can currently deposit ~1_VALUE~ points.
            builder.AddHtmlLocalized(15, 170, 340, 20, 1158459, $"{points:N0}", Highlight);
            // You can currently withdraw ~1_VALUE~ points.
            builder.AddHtmlLocalized(15, 200, 340, 20, 1158458, $"{accountPoints:N0}", Highlight);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case 1:
                    {
                        CleanUpBritanniaData.AddPointsToExchange(User);
                        break;
                    }
                case 2:
                    {
                        CleanUpBritanniaData.RemovePointsFromExchange(User);
                        break;
                    }
                default:
                    {
                        return;
                    }
            }

            User.SendGump(new ExchangeGump(User));
        }
    }
}
