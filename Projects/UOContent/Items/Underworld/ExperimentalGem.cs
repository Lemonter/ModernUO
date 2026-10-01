using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

public enum Room
{
    RoomZero  = 0,
    RoomOne   = 1,
    RoomTwo   = 2,
    RoomThree = 3,
    RoomFour  = 4
}

// Ported from real OSI/ServUO content (Scripts/Services/Underworld/ExperimentalRoom/
// ExperimentalGem.cs) — carrying an activated gem opens a 30-minute self-destruct window.
// The gem's hue cycles between a "neutral" state and one of several colors; standing in the
// matching-colored floor tile brings it back to neutral, while lingering lets it advance to
// an "extreme" hue that expels you from the room on the next miss. Clearing 3 progressively
// harder rooms completes the puzzle. `GetFloorString` (defined but never called anywhere in
// the real source either — dead debug leftover) and the public-but-never-read `RegularHues`
// field are dropped. One real upstream bug fixed while porting: GetRevertedHue's DarkGreen
// case returned `Orange` (a no-op — floorHue was already Orange) instead of `LightGreen`
// (DarkGreen's actual Normal state per the room's own Extreme/Normal/SlowOpposite/
// FastOpposite table below) when standing on its SlowOpposite tile — every other case
// follows that table correctly, this one alone copy-pasted the wrong target color.
[SerializationGenerator(0, false)]
public partial class ExperimentalGem : BaseDecayingItem
{
    private const int Neutral    = 0x356;
    private const int Red        = 0x26;
    private const int White      = 0x481;
    private const int Blue       = 0x4;
    private const int Pink       = 0x4B2;
    private const int Orange     = 0x30;
    private const int LightGreen = 0x3D;
    private const int DarkGreen  = 0x557;
    private const int Brown      = 0x747;

    private static readonly TimeSpan HueToHueDelay = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan HueToLocDelay = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan RoomToRoomDelay = TimeSpan.FromSeconds(20);

    private static readonly Rectangle2D EntranceRec = new(980, 1117, 17, 3);

    private bool _isExtremeHue;
    private Room _currentRoom;
    private double _completed;
    private double _toComplete;
    private int _lastIndex;
    private int _currentHue;
    private bool _holding;
    private Timer _timer;
    private DateTime _expire;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Active { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsExtremeHue => _isExtremeHue;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool Complete { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Room CurrentRoom
    {
        get => _currentRoom;
        set => _currentRoom = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public double Completed
    {
        get => _completed;
        set => _completed = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public double ToComplete
    {
        get => _toComplete;
        set => _toComplete = value;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public int CurrentHue => _currentHue;

    [CommandProperty(AccessLevel.GameMaster)]
    public int LastIndex => _lastIndex;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [SerializableField(1)]
    private int _span;

    public override int Lifespan => _span;

    public override int LabelNumber =>
        Active ? 1113409 : 1113380; // An Experimental Gem [Activated] : An Experimental Gem

    [Constructible]
    public ExperimentalGem() : base(6463)
    {
        _lastIndex = -1;
        _currentHue = Neutral;
        _currentRoom = Room.RoomZero;
        _expire = DateTime.MaxValue;

        Hue = Neutral;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1054107); // This item must be in your backpack.
        }
        else if (ExperimentalRoomController.IsInCooldown(from))
        {
            from.SendLocalizedMessage(1113413); // You have recently participated in this challenge. You must wait 24 hours to try again.
        }
        else if (!Active && EntranceRec.Contains(from.Location) && from.Map == Map.TerMur)
        {
            from.CloseGump<InternalGump>();
            from.SendGump(new InternalGump(this));
        }
        else if (Active)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1113408); // The gem is already active. You must clear the rooms before it is destroyed!
        }
    }

    public override void Decay()
    {
        // The gem never expires from BaseDecayingItem's normal countdown — Reset()/
        // OnPuzzleFailed() handle the 30-minute self-destruct window instead.
    }

    public void Activate(Mobile from)
    {
        Active = true;

        StartTimer();

        _currentRoom = Room.RoomOne;
        _toComplete = Utility.RandomMinMax(5, 8);

        Timer.DelayCall(TimeSpan.FromSeconds(5), BeginRoom);

        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1113405); // Your gem is now active. You may enter the Experimental Room.

        InvalidateProperties();
    }

    private void BeginRoom()
    {
        _timer = new InternalTimer(this);
        _lastIndex = -1;
        SelectNewHue();
    }

    public override void StartTimer()
    {
        if (_span == 0)
        {
            TimeLeft = 1800;
            _span = 1800;
            base.StartTimer();
            InvalidateProperties();
        }
    }

    private void SelectNewHue()
    {
        var list = GetRoomHues();
        int index;

        do
        {
            index = Utility.Random(list.Length);
        } while (index == _lastIndex);

        _expire = Core.Now + HueToLocDelay;
        _holding = false;
        _lastIndex = index;

        if (IsExtreme(list[index]))
        {
            _isExtremeHue = true;
        }

        Hue = list[index];
        _currentHue = Hue;
    }

    public void OnTick()
    {
        if (_holding || _expire > Core.Now)
        {
            return;
        }

        var m = RootParent as Mobile;
        var floorHue = GetFloorHue(m);
        var nextHue = GetRevertedHue(m, _currentHue, floorHue);

        if (m != null && nextHue >= 0) // Standing in the right spot
        {
            if (_isExtremeHue && nextHue != Neutral) // from extreme back to regular
            {
                _completed += 0.5;

                Hue = nextHue;
                _currentHue = Hue;
                _isExtremeHue = false;
                _lastIndex = GetIndexFor(nextHue);
                _expire = Core.Now + HueToLocDelay;
                m.PlaySound(0x51);

                _completed += 0.5;
            }
            else // Neutralized, new color
            {
                _completed++;

                _isExtremeHue = false;
                _holding = true;
                Hue = Neutral;
                _currentHue = Neutral;

                if (_completed < _toComplete)
                {
                    Timer.DelayCall(HueToHueDelay, SelectNewHue);
                    m.PlaySound(0x51);
                }
            }

            if (_completed >= _toComplete) // next room or complete!
            {
                if (_currentRoom == Room.RoomThree) // puzzle completed
                {
                    _holding = true;
                    Hue = Neutral;
                    _currentHue = Neutral;
                    CompletePuzzle();

                    m.PlaySound(0x1FF);
                    m.LocalOverheadMessage(MessageType.Regular, 0x21, 1113403); // Congratulations!! The last room has been unlocked!! Hurry through to claim your reward!
                }
                else // on to the next room!
                {
                    _currentRoom++;

                    _holding = true;
                    Hue = Neutral;
                    _currentHue = Neutral;

                    _completed = 0;

                    _toComplete = _currentRoom switch
                    {
                        Room.RoomTwo   => Utility.RandomMinMax(10, 15),
                        Room.RoomThree => Utility.RandomMinMax(15, 25),
                        _              => Utility.RandomMinMax(5, 8)
                    };

                    _lastIndex = -1;
                    Timer.DelayCall(RoomToRoomDelay, SelectNewHue);

                    m.PlaySound(0x1FF);
                    m.LocalOverheadMessage(MessageType.Regular, 0x21, 1113402); // The next room has been unlocked! Hurry through the door before your gem's state changes again!
                }
            }
        }
        else if (_isExtremeHue) // Already extreme, failed
        {
            if (m != null && m.AccessLevel == AccessLevel.Player)
            {
                OnPuzzleFailed(m);
            }
            else
            {
                m?.SendMessage("Как ГМ, ты получаешь ещё одну попытку!");
                _expire = Core.Now + HueToLocDelay;
            }

            m?.LocalOverheadMessage(MessageType.Regular, 0x21, 1113400); // You fail to neutralize the gem in time and are expelled from the room!!
        }
        else if (Hue != -1) // set to extreme hue
        {
            var hue = GetExtreme(_currentHue);

            Hue = hue;
            _currentHue = hue;
            _isExtremeHue = true;
            _lastIndex = GetIndexFor(hue);

            m?.LocalOverheadMessage(MessageType.Regular, 0x21, 1113401); // The state of your gem worsens!!

            _expire = Core.Now + HueToLocDelay;
        }
    }

    private void CompletePuzzle()
    {
        _timer?.Stop();
        _timer = null;

        Complete = true;
        Hue = Neutral;
        _currentHue = Hue;

        _currentRoom = Room.RoomFour;
    }

    private void OnPuzzleFailed(Mobile m)
    {
        if (m != null)
        {
            var x = Utility.RandomMinMax(EntranceRec.X, EntranceRec.X + EntranceRec.Width);
            var y = Utility.RandomMinMax(EntranceRec.Y, EntranceRec.Y + EntranceRec.Height);
            var z = m.Map.GetAverageZ(x, y);

            var from = m.Location;
            var p = new Point3D(x, y, z);

            m.PlaySound(0x1FE);
            Effects.SendLocationParticles(EffectItem.Create(from, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
            Effects.SendLocationParticles(EffectItem.Create(p, m.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

            BaseCreature.TeleportPets(m, p, Map.TerMur);
            m.MoveToWorld(p, Map.TerMur);
        }

        Reset();
    }

    private void Reset()
    {
        _timer?.Stop();
        _timer = null;

        Complete = false;
        _completed = 0;
        _toComplete = 0;
        _isExtremeHue = false;
        Active = false;
        _currentRoom = Room.RoomOne;
        _lastIndex = -1;
        _expire = DateTime.MaxValue;
        _currentHue = Neutral;
        Hue = Neutral;
        InvalidateProperties();
    }

    public override void OnDelete()
    {
        base.OnDelete();

        _timer?.Stop();

        if (_owner != null)
        {
            ExperimentalRoomController.AddToTable(_owner);
        }
    }

    private class InternalTimer : Timer
    {
        private readonly ExperimentalGem _gem;

        public InternalTimer(ExperimentalGem gem) : base(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5))
        {
            _gem = gem;
            Start();
        }

        protected override void OnTick()
        {
            if (_gem?.Deleted == false)
            {
                _gem.OnTick();
            }
            else
            {
                Stop();
            }
        }
    }

    private int[] GetRoomHues() =>
        _currentRoom switch
        {
            Room.RoomTwo   => RoomHues[1],
            Room.RoomThree => RoomHues[2],
            _              => RoomHues[0]
        };

    private Rectangle2D GetRoomRec() =>
        _currentRoom switch
        {
            Room.RoomTwo   => RoomRecs[1],
            Room.RoomThree => RoomRecs[2],
            _              => RoomRecs[0]
        };

    public int GetIndexFor(int hue)
    {
        var hues = GetRoomHues();

        for (var i = 0; i < hues.Length; i++)
        {
            if (hue == hues[i])
            {
                return i;
            }
        }

        return White; // Oops, something happened, this should never happen.
    }

    public static int GetExtreme(int hue) =>
        hue switch
        {
            Pink       => Red,       // Pink to Red
            White      => Blue,      // White to Blue
            Orange     => Brown,     // Orange to Brown
            LightGreen => DarkGreen, // LightGreen to DarkGreen
            _          => -1
        };

    public static int GetRegular(int hue) =>
        hue switch
        {
            Red       => Pink,       // Red to Pink
            Blue      => White,      // Blue to White
            Brown     => Orange,     // Brown to Orange
            DarkGreen => LightGreen, // DarkGreen to LightGreen
            _         => -1
        };

    /*  Extreme     Normal      SlowOpposite        FastOpposite
     *
     *  Red         Pink        White               Blue
     *  Blue        White       Pink                Red
     *  Brown       Orange      LightGreen          DarkGreen
     *  DarkGreen   LightGreen  Orange              Brown
     */

    // Checks the location the player is standing in relative to the gem's current hue.
    // Returns -1 if they're in the wrong spot, or the new gem hue if they're in the right one.
    public int GetRevertedHue(Mobile from, int oldHue, int floorHue)
    {
        if (from == null || !GetRoomRec().Contains(from.Location))
        {
            return -1;
        }

        switch (oldHue)
        {
            case White:
                if (floorHue is Pink or Red)
                {
                    return Neutral;
                }

                break;
            case Pink:
                if (floorHue is White or Blue)
                {
                    return Neutral;
                }

                break;
            case Orange:
                if (floorHue is LightGreen or DarkGreen)
                {
                    return Neutral;
                }

                break;
            case LightGreen:
                if (floorHue is Orange or Brown)
                {
                    return Neutral;
                }

                break;
            case Red:
                if (floorHue == White)
                {
                    return Pink;
                }

                if (floorHue == Blue)
                {
                    return Neutral;
                }

                break;
            case Blue:
                if (floorHue == Pink)
                {
                    return White;
                }

                if (floorHue == Red)
                {
                    return Neutral;
                }

                break;
            case DarkGreen:
                if (floorHue == Orange)
                {
                    return LightGreen;
                }

                if (floorHue == Brown)
                {
                    return Neutral;
                }

                break;
            case Brown:
                if (floorHue == LightGreen)
                {
                    return Orange;
                }

                if (floorHue == DarkGreen)
                {
                    return Neutral;
                }

                break;
        }

        return -1;
    }

    public static int GetFloorHue(Mobile from)
    {
        if (from == null || from.Map != Map.TerMur)
        {
            return 0;
        }

        for (var i = 0; i < FloorRecs.Length; i++)
        {
            if (FloorRecs[i].Contains(from.Location))
            {
                return FloorHues[i];
            }
        }

        return 0;
    }

    public bool IsExtreme(int hue)
    {
        foreach (var i in ExtremeHues)
        {
            if (i == hue)
            {
                return true;
            }
        }

        return false;
    }

    private static readonly int[] ExtremeHues = { Red, Blue, Brown, DarkGreen };

    private static readonly int[][] RoomHues =
    {
        new[] { White, Pink, Red, Blue },                                        // Room One
        new[] { Pink, Blue, Red, Orange, LightGreen, White },                    // Room Two
        new[] { Blue, Pink, DarkGreen, Orange, Brown, LightGreen, Red, White }    // Room Three
    };

    private static readonly Rectangle2D[] FloorRecs =
    {
        // Room One
        new(977, 1104, 5, 5), // White, opposite of pink
        new(987, 1104, 5, 5), // Pink, opposite of white
        new(977, 1109, 5, 5), // Blue, opposite of red
        new(987, 1109, 5, 5), // Red, opposite of Blue

        // Room Two
        new(977, 1092, 6, 3), // White, opposite of pink
        new(986, 1092, 6, 3), // Red, opposite of Blue
        new(977, 1095, 6, 3), // Blue, opposite of red
        new(986, 1095, 6, 3), // LightGreen, opposite of Orange
        new(977, 1098, 6, 3), // Orange, opposite of LightGreen
        new(986, 1098, 6, 3), // Pink, opposite of white

        // Room Three
        new(977, 1074, 3, 5), // Red, opposite of Blue
        new(980, 1074, 3, 5), // White, opposite of Pink
        new(986, 1074, 3, 5), // Brown, opposite of DarkGreen
        new(989, 1074, 3, 5), // LightGreen, opposite of Orange
        new(977, 1079, 3, 5), // DarkGreen, opposite of Brown
        new(980, 1079, 3, 5), // Orange, opposite of LightGreen
        new(986, 1079, 3, 5), // Blue, opposite of red
        new(989, 1079, 3, 5)  // Pink, opposite of White
    };

    private static readonly int[] FloorHues =
    {
        // Room One
        White, Pink, Red, Blue,

        // Room Two
        White, Red, Blue, LightGreen, Orange, Pink,

        // Room Three
        Red, White, Brown, LightGreen, DarkGreen, Orange, Blue, Pink
    };

    private static readonly Rectangle2D[] RoomRecs =
    {
        new(977, 1104, 15, 10), // RoomOne
        new(977, 1092, 15, 9),  // RoomTwo
        new(977, 1074, 15, 10)  // RoomThree
    };

    [AfterDeserialization]
    private void AfterDeserialization() => Reset();

    private class InternalGump : Gump
    {
        private readonly ExperimentalGem _gem;

        public InternalGump(ExperimentalGem gem) : base(50, 50)
        {
            _gem = gem;

            AddPage(0);
            AddBackground(0, 0, 297, 115, 9200);

            AddImageTiled(5, 10, 285, 25, 2624);
            AddHtmlLocalized(10, 15, 275, 25, 1113407, 0x7FFF, false, false); // Experimental Room Access

            AddImageTiled(5, 40, 285, 40, 2624);
            AddHtmlLocalized(10, 40, 275, 40, 1113391, 0x7FFF, false, false); // Click CANCEL to read the instruction book or OK to start the timer now.

            AddButton(5, 85, 4017, 4018, 0);
            AddHtmlLocalized(40, 87, 80, 25, 1011012, 0x7FFF, false, false); // CANCEL

            AddButton(215, 85, 4023, 4024, 1);
            AddHtmlLocalized(250, 87, 80, 25, 1006044, 0x7FFF, false, false); // OK
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                _gem.Activate(state.Mobile);
            }
        }
    }
}
