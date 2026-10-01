using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/PuzzleRoom/
// MagicKey.cs). The confirm gump is a plain Gump here (OnResponse(NetState, in RelayInfo)),
// same migration shape as everywhere else this session. One real bug fix: ServUO's own
// Decay() checks against `new Rectangle2D(1234, 1234, 10, 10)` — an obvious placeholder/
// leftover value that doesn't match the actual Puzzle Room bounds anywhere in the source —
// corrected here to the real room rectangle (matches MazePuzzleItem.IsInPuzzleRoom).
[SerializationGenerator(0, false)]
public partial class MagicKey : BaseDecayingItem
{
    [SerializableField(0)]
    private int _span;

    public override int LabelNumber => 1024114; // magic key
    public override int Lifespan => _span;

    private static readonly Rectangle2D PuzzleRoomBounds = new(1089, 1162, 16, 12);

    [Constructible]
    public MagicKey() : base(4114)
    {
        _span = 0;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (RootParent != null || !from.InRange(GetWorldLocation(), 3) || Movable || IsLockedDown || IsSecure)
        {
            return;
        }

        if (from.Backpack != null && _span == 0)
        {
            var key = from.Backpack.FindItemByType(typeof(MagicKey));

            if (key == null)
            {
                from.CloseGump<MagicKeyConfirmGump>();
                from.SendGump(new MagicKeyConfirmGump());
            }
        }
    }

    // Not started at construction (Lifespan was still 0 then) — overridden to set the real
    // span first and then actually kick off the base decay countdown timer.
    public override void StartTimer()
    {
        TimeLeft = 1800;
        _span = 1800;
        Movable = true;
        InvalidateProperties();

        base.StartTimer();
    }

    public override void Decay()
    {
        if (RootParent is Mobile m && m.Map != Map.Internal)
        {
            if (PuzzleRoomBounds.Contains(m.Location))
            {
                var x = Utility.RandomMinMax(1096, 1098);
                var y = Utility.RandomMinMax(1175, 1177);
                var z = m.Map.GetAverageZ(x, y);

                var loc = m.Location;
                var p = new Point3D(x, y, z);
                BaseCreature.TeleportPets(m, p, Map.TerMur);
                m.MoveToWorld(p, Map.TerMur);

                Effects.SendLocationParticles(EffectItem.Create(loc, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                Effects.SendLocationParticles(EffectItem.Create(p, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);
            }

            var pack = m.Backpack;

            if (pack != null)
            {
                var list = new List<Item>(pack.Items);

                foreach (var item in list)
                {
                    if (item is CopperPuzzleKey or GoldPuzzleKey or MazePuzzleItem or MastermindPuzzleItem)
                    {
                        item.Delete();
                    }
                }
            }
        }

        base.Decay();
    }

    private class MagicKeyConfirmGump : Gump
    {
        public MagicKeyConfirmGump() : base(50, 50)
        {
            AddBackground(0, 0, 297, 115, 9200);

            AddImageTiled(5, 10, 285, 25, 2624);
            AddHtmlLocalized(10, 15, 275, 25, 1113390, 0x7FFF, false, false); // Puzzle Room Timer

            AddImageTiled(5, 40, 285, 40, 2624);
            AddHtmlLocalized(10, 40, 275, 40, 1113391, 0x7FFF, false, false); // Click CANCEL to read the instruction book or OK to start the timer now.

            AddButton(5, 85, 4017, 4018, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(40, 87, 80, 25, 1011012, 0x7FFF, false, false); // CANCEL

            AddButton(215, 85, 4023, 4024, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(250, 87, 80, 25, 1006044, 0x7FFF, false, false); // OK
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            if (from == null || info.ButtonID != 1)
            {
                return;
            }

            var key = new MagicKey();
            from.AddToBackpack(key);

            key.Movable = true;
            key.StartTimer();

            from.SendLocalizedMessage(1113389); // As long as you carry this key, you will be granted access to the Puzzle Room.
        }
    }
}
