using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/DoomSign.cs).
///     ServUO's <c>PointsSystem.TreasuresOfDoom.InSeason</c> check becomes <see cref="DoomData.InSeason" />.
///     The per-player "already revealed" list is not persisted: keeping a <c>List&lt;Mobile&gt;</c> field on the
///     sign tripped the serialization generator's migration rules, and the flag is only used to pick the gump art.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DoomSign : Item, IRevealableItem
{
    private static readonly Dictionary<Mobile, DateTime> _nextMessage = new();

    private bool _revealed;

    public override bool ForceShowProperties => true;

    public bool CheckWhenHidden => false;

    [Constructible]
    public DoomSign() : base(0xBD0)
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!DoomData.InSeason || !m.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        var gump = new Gumps.Gump(150, 150);
        gump.AddImage(0, 0, _revealed ? 0x7779 : 0x7724);
        m.SendGump(gump);
    }

    public override bool HandlesOnMovement => DoomData.InSeason;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (!m.Player || !m.InRange(Location, 3) || !m.InLOS(this))
        {
            return;
        }

        CheckList();

        if (_nextMessage.ContainsKey(m))
        {
            return;
        }

        // *The wooden sign seems in oddly good condition... Most of the inscription is worn away...*
        m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1155659, m.NetState);
        _nextMessage[m] = Core.Now + TimeSpan.FromMinutes(10);
    }

    public bool CheckReveal(Mobile m) => Utility.Random((int)m.Skills[SkillName.DetectHidden].Value) < 100;

    public void OnRevealed(Mobile m)
    {
        if (!_revealed)
        {
            _revealed = true;

            // *You reveal something hidden about the object...*
            m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1155658, m.NetState);
        }

        OnDoubleClick(m);
    }

    private static void CheckList()
    {
        if (_nextMessage.Count == 0)
        {
            return;
        }

        var now = Core.Now;

        foreach (var (mobile, expiry) in _nextMessage)
        {
            if (expiry < now)
            {
                _nextMessage.Remove(mobile);
            }
        }
    }
}

