using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Свойства PlayerMobile.DisabledPvpWarning в репозитории нет,
///     поэтому вариант «больше не спрашивать» отброшен.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PvPWarnTeleporter : Teleporter
{
    [Constructible]
    public PvPWarnTeleporter()
    {
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (m is PlayerMobile pm)
        {
            if (!pm.HasGump<PvpWarningGump>())
            {
                pm.SendGump(new PvpWarningGump(this));
            }

            return true;
        }

        return true;
    }
}

public class PvpWarningGump : StaticGump<PvpWarningGump>
{
    private readonly Teleporter _owner;

    public override bool Singleton => true;

    public PvpWarningGump(Teleporter teleporter) : base(150, 50) => _owner = teleporter;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddImage(0, 0, 0xE10);
        builder.AddImageTiled(0, 14, 15, 200, 0xE13);
        builder.AddImageTiled(380, 14, 14, 200, 0xE15);
        builder.AddImage(0, 201, 0xE16);
        builder.AddImageTiled(15, 201, 370, 16, 0xE17);
        builder.AddImageTiled(15, 0, 370, 16, 0xE11);
        builder.AddImage(380, 0, 0xE12);
        builder.AddImage(380, 201, 0xE18);
        builder.AddImageTiled(15, 15, 365, 190, 0xA40);

        builder.AddHtmlLocalized(30, 20, 330, 20, 1060635, 0x4040FE); // <CENTER>WARNING</CENTER>

        // You are about to enter a PvP area, where you can be attacked and stolen from by other players...
        builder.AddHtmlLocalized(30, 50, 330, 60, 1113792, 0xFFFFFF);

        // Yes, I wish to proceed
        builder.AddHtmlLocalized(65, 125, 300, 25, 1113794, 0xFFFFFF);
        builder.AddButton(30, 125, 0xFA5, 0xFA7, 1);

        // Yes, and do not ask me again
        builder.AddHtmlLocalized(65, 150, 300, 25, 1113795, 0xFFFFFF);
        builder.AddButton(30, 150, 0xFA5, 0xFA7, 2);

        // No, I do not wish to proceed
        builder.AddHtmlLocalized(65, 175, 300, 25, 1113793, 0xFFFFFF);
        builder.AddButton(30, 175, 0xFA5, 0xFA7, 0);

        builder.AddImageTiled(15, 14, 365, 1, 0x2393);
        builder.AddImageTiled(380, 14, 1, 190, 0x2391);
        builder.AddImageTiled(15, 205, 365, 1, 0x2393);
        builder.AddImageTiled(15, 14, 1, 190, 0x2391);
        builder.AddImageTiled(0, 0, 395, 1, 0x23C5);
        builder.AddImageTiled(394, 0, 1, 217, 0x23C3);
        builder.AddImageTiled(0, 216, 395, 1, 0x23C5);
        builder.AddImageTiled(0, 0, 1, 217, 0x23C3);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile pm || pm.InRange(_owner.Location, 5) == false)
        {
            return;
        }

        if (info.ButtonID is 1 or 2)
        {
            BaseCreature.TeleportPets(pm, _owner.PointDest, _owner.MapDest);
            pm.MoveToWorld(_owner.PointDest, _owner.MapDest);
        }
    }
}
