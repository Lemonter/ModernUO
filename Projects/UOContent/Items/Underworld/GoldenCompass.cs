using ModernUO.Serialization;
using Server.Gumps;
using Server.Regions;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/Maze of
// Death/GoldenCompass.cs) — carrying one shows the direction gump while inside the Maze of
// Death's trap corridor. `SendTimeRemainingMessage` (a ServUO BaseDecayingItem helper) isn't
// part of our BaseDecayingItem — dropped, GetProperties already shows the remaining lifespan.
[SerializationGenerator(0, false)]
public partial class GoldenCompass : BaseDecayingItem
{
    [SerializableField(0)]
    private int _span;

    public override int Lifespan => _span;
    public override int LabelNumber => 1113578; // a golden compass

    [Constructible]
    public GoldenCompass() : base(0x1CB)
    {
        Weight = 1;
        Hue = 1177;
        _span = 0;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            if (from.Region?.IsPartOf<MazeOfDeathRegion>() == true)
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1113585); // The compass' arrows flicker. You must be near the right location.
            }
            else
            {
                from.SendLocalizedMessage(1155663); // Nothing happens.
            }
        }
        else if (RootParent == null && !Movable && !IsLockedDown && !IsSecure)
        {
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (from.Backpack == null || _span != 0)
            {
                return;
            }

            if (from.Backpack.FindItemByType(typeof(GoldenCompass)) != null)
            {
                from.SendLocalizedMessage(501885); // You already own one of those!
                return;
            }

            var gc = new GoldenCompass();

            if (from.PlaceInBackpack(gc))
            {
                gc.StartTimer();
                from.SendLocalizedMessage(1072223); // An item has been placed in your backpack.
            }
            else
            {
                gc.Delete();
            }
        }
    }

    // Not started at construction (Lifespan was still 0 then, so the base class's own
    // constructor-time StartTimer() no-opped) — overridden to set the real span first and
    // then actually kick off the base decay countdown timer.
    public override void StartTimer()
    {
        TimeLeft = 7200;
        _span = 7200;
        Movable = true;
        InvalidateProperties();

        base.StartTimer();
    }

    public override void OnDelete()
    {
        base.OnDelete();

        if (RootParent is Mobile m)
        {
            m.CloseGump<CompassDirectionGump>();
        }
    }

    public override bool OnDroppedToMobile(Mobile from, Mobile target)
    {
        from.SendLocalizedMessage(1076256); // That item cannot be traded.
        return false;
    }

    public override bool DropToItem(Mobile from, Item target, Point3D p)
    {
        from.SendLocalizedMessage(1076254); // That item cannot be dropped.
        return false;
    }

    public override bool OnDroppedToWorld(Mobile from, Point3D p)
    {
        from.SendLocalizedMessage(1076254); // That item cannot be dropped.
        return false;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_span > 0)
        {
            StartTimer();
        }
    }
}
