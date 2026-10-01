using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Dungeons/Underworld/PuzzleRoom/
// MazePuzzleItem.cs) — the "close the circuit" trap board, now runnable against our own
// ICircuitTrap/CircuitTrapGump port (Gumps/Traps/CircuitTrap.cs). `BaseGump.SendGump(...)`
// (ServUO's own gump-framework helper) replaced with a plain `from.SendGump(...)`;
// closing PuzzleChest's PuzzleGump/StatusGump on double-click was dropped since both are
// private nested types on our PuzzleChest (see Engines/Khaldun/PuzzleChest.cs) — harmless,
// they close themselves via the Singleton gump pattern regardless.
[SerializationGenerator(0, false)]
public partial class MazePuzzleItem : BaseDecayingItem, ICircuitTrap
{
    [SerializableField(0)]
    private MagicKey _key;

    [SerializableField(1)]
    private List<int> _path;

    [SerializableField(2)]
    private List<int> _progress;

    // Path/Progress (codegen-generated public properties from the fields above) satisfy
    // ICircuitTrap directly — same shape as every other codegen-backed interface property
    // in this codebase (e.g. VvVTrap.Links).
    public CircuitCount Count => CircuitCount.ThirtySix;
    public int GumpTitle => 1153747; // <center>GENERATOR CONTROL PANEL</center>
    public int GumpDescription => 1153749; // <center>Close the Grid Circuit</center>
    public bool CanDecipher => true;

    public override int LabelNumber => 1113379; // Puzzle Board
    public override int Lifespan => 600;

    [Constructible]
    public MazePuzzleItem(MagicKey key) : base(0x2AAA)
    {
        Hue = 914;
        _key = key;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(500325); // I am too far away to do that.
        }
        else if (from is PlayerMobile pm && IsInPuzzleRoom(from))
        {
            from.CloseGump<CircuitTrapGump>();
            from.SendGump(new CircuitTrapGump(pm, this));
        }
    }

    private static readonly Rectangle2D RoomBounds = new(1089, 1162, 16, 12);

    public static bool IsInPuzzleRoom(Mobile from) =>
        from.Map == Map.TerMur && RoomBounds.Contains(new Point2D(from.X, from.Y));

    public override void OnDelete()
    {
        if (RootParent is Mobile m)
        {
            m.CloseGump<CircuitTrapGump>();
        }
    }

    public void OnSelfClose(Mobile m)
    {
    }

    public void OnProgress(Mobile m, int pick) => m.PlaySound(0x1F5);

    public void OnFailed(Mobile m) => DoDamage(m);

    public void OnComplete(Mobile m)
    {
        m.PlaySound(0x3D);
        OnPuzzleCompleted(m);
    }

    private Timer _damageTimer;

    private void DoDamage(Mobile m)
    {
        _damageTimer?.Stop();
        _damageTimer = new InternalTimer(this, m);
        _damageTimer.Start();
    }

    private void ApplyShock(Mobile m, int tick)
    {
        if (m == null || !m.Alive || Deleted)
        {
            _damageTimer?.Stop();
            return;
        }

        var damage = 75 / Math.Max(1, tick - 1) + Utility.RandomMinMax(1, 9);

        AOS.Damage(m, damage, 0, 0, 0, 0, 100);

        m.BoltEffect(0);

        m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.CenterFeet);
        m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist);
        m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Head);
        m.PlaySound(0x1DC);

        m.LocalOverheadMessage(MessageType.Regular, 0x21, 1114443); // * Your body convulses from electric shock *
        m.NonlocalOverheadMessage(MessageType.Regular, 0x21, 1114443, m.Name); // * ~1_NAME~ spasms from electric shock *
    }

    private class InternalTimer : Timer
    {
        private readonly MazePuzzleItem _item;
        private readonly Mobile _from;
        private DateTime _nextDamage;
        private int _tick;

        public InternalTimer(MazePuzzleItem item, Mobile from) : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
        {
            _item = item;
            _from = from;
            _nextDamage = Core.Now;

            item.ApplyShock(from, 0);
        }

        protected override void OnTick()
        {
            _tick++;

            if (_from == null || _item == null || !_from.Alive || _item.Deleted)
            {
                Stop();
                return;
            }

            if (Core.Now <= _nextDamage)
            {
                return;
            }

            _item.ApplyShock(_from, _tick);

            var delay = _tick switch
            {
                < 3 => 2,
                < 5 => 4,
                _   => 6
            };

            if (_tick >= 10)
            {
                Stop();
            }
            else
            {
                _nextDamage = Core.Now + TimeSpan.FromSeconds(delay);
            }
        }
    }

    private void OnPuzzleCompleted(Mobile m)
    {
        var pack = m.Backpack;

        if (pack != null)
        {
            var copperKey = pack.FindItemByType(typeof(CopperPuzzleKey));
            var goldKey = pack.FindItemByType(typeof(GoldPuzzleKey));

            if (copperKey == null)
            {
                pack.DropItem(new CopperPuzzleKey());
            }
            else if (goldKey == null)
            {
                pack.DropItem(new GoldPuzzleKey());
            }
            else
            {
                Timer.DelayCall(TimeSpan.FromSeconds(3), Delete);
                return;
            }

            m.SendLocalizedMessage(1113382); // You've solved the puzzle!! An item has been placed in your bag.
        }

        Timer.DelayCall(TimeSpan.FromSeconds(3), Delete);
    }
}
