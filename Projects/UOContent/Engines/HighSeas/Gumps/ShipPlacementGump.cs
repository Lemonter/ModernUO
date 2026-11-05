/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Gumps/ShipPlacementGump.cs, класс
///     <c>BoatPlacementGump</c>).
///
///     Выбор направления, в котором встанет галера, перед обычной постановкой дея. Сама постановка —
///     штатная <c>BaseBoatDeed.OnPlacement</c>: окно лишь запоминает направление в
///     <see cref="BaseGalleonDeed.Facing" /> и запускает <c>MultiTarget</c> с повёрнутым ID мульти.
///
///     Замены: в ServUO использовались <c>BaseBoat.GetID(multiID, direction)</c> и
///     <c>OnPlacement(from, p, itemId, facing)</c> — таких перегрузок в этом движке нет, вместо них
///     <see cref="BaseGalleonDeed.GetMultiId" /> и <c>OnPlacement(from, p)</c>. Ветки проверки региона
///     взяты из <c>BaseDockedBoat.InternalTarget</c>, чтобы совпадать с классическими лодками.
/// </summary>
using Server.Engines.CannedEvil;
using Server.Gumps;
using Server.Network;
using Server.Regions;
using Server.Targeting;

namespace Server.Engines.HighSeas;

public class ShipPlacementGump : Gump
{
    private const int LabelColor = 0x7FFF;

    private readonly BaseGalleonDeed _deed;

    private ShipPlacementGump(BaseGalleonDeed deed) : base(0, 0)
    {
        _deed = deed;

        AddPage(0);

        AddBackground(0, 0, 220, 170, 0x13BE);
        AddBackground(10, 10, 200, 150, 0xBB8);

        AddHtml(20, 20, 180, 70, "<CENTER>Выберите направление, в котором встанет корабль.</CENTER>", false, false);

        AddButton(20, 100, 0xFA5, 0xFA7, 1, GumpButtonType.Reply, 0);
        AddLabel(55, 100, LabelColor, "ЗАПАД");

        AddButton(115, 100, 0xFA5, 0xFA7, 2, GumpButtonType.Reply, 0);
        AddLabel(150, 100, LabelColor, "СЕВЕР");

        AddButton(20, 125, 0xFA5, 0xFA7, 3, GumpButtonType.Reply, 0);
        AddLabel(55, 125, LabelColor, "ЮГ");

        AddButton(115, 125, 0xFA5, 0xFA7, 4, GumpButtonType.Reply, 0);
        AddLabel(150, 125, LabelColor, "ВОСТОК");
    }

    public static void DisplayTo(Mobile from, BaseGalleonDeed deed)
    {
        if (from?.NetState == null || deed?.Deleted != false)
        {
            return;
        }

        from.CloseGump<ShipPlacementGump>();
        from.SendGump(new ShipPlacementGump(deed));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _deed?.Deleted != false || !_deed.IsChildOf(from.Backpack))
        {
            return;
        }

        Direction facing;

        switch (info.ButtonID)
        {
            case 1:
                facing = Direction.West;
                break;
            case 2:
                facing = Direction.North;
                break;
            case 3:
                facing = Direction.South;
                break;
            case 4:
                facing = Direction.East;
                break;
            default:
                return;
        }

        _deed.Facing = facing;

        if (Core.SE)
        {
            from.SendLocalizedMessage(502482); // Where do you wish to place the ship?
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502482);
        }

        from.Target = new InternalTarget(_deed);
    }

    private class InternalTarget : MultiTarget
    {
        private readonly BaseGalleonDeed _deed;

        public InternalTarget(BaseGalleonDeed deed) : base(deed.GetMultiId(deed.Facing), deed.Offset) => _deed = deed;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is not IPoint3D ip)
            {
                return;
            }

            var p = ip switch
            {
                Item item => item.GetWorldTop(),
                Mobile m  => m.Location,
                _         => new Point3D(ip)
            };

            var region = Region.Find(p, from.Map);

            if (region.IsPartOf<DungeonRegion>())
            {
                from.SendLocalizedMessage(502488); // You can not place a ship inside a dungeon.
            }
            else if (region.IsPartOf<HouseRegion>() || region.IsPartOf<ChampionSpawnRegion>())
            {
                from.SendLocalizedMessage(1042549); // A boat may not be placed in this area.
            }
            else
            {
                _deed.OnPlacement(from, p);
            }
        }
    }
}
