using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Items/DoomPlaque.cs).
///     ServUO's <c>PointsSystem.TreasuresOfDoom.InSeason</c> check becomes <see cref="DoomData.InSeason" />
///     (<c>Enabled</c> defaults to false). <c>Prompt.MessageCliloc</c> does not exist here, so the prompt message
///     is sent before the prompt is assigned.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DoomPlaque : Item
{
    public static readonly Point3D TeleportDestination = new(76, 224, 4);

    private static readonly Dictionary<Mobile, DateTime> _nextMessage = new();

    public override int LabelNumber => 1155662; // Plaque

    public override bool ForceShowProperties => true;

    [Constructible]
    public DoomPlaque() : base(0x4B20)
    {
        Movable = false;
        Hue = 2500;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (m.InRange(GetWorldLocation(), 2))
        {
            m.SendLocalizedMessage(1155661); // *You run your fingers over the runes...*
            m.Prompt = new DoomPlaquePrompt();
        }
    }

    public override bool HandlesOnMovement => true;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (!DoomData.InSeason || !m.Player || !m.InRange(Location, 3) || !m.InLOS(this))
        {
            return;
        }

        CheckList();

        if (_nextMessage.ContainsKey(m))
        {
            return;
        }

        // *You examine the plaque...there appears to be a series of runic characters that are raised up...*
        m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1155660, m.NetState);
        _nextMessage[m] = Core.Now + TimeSpan.FromMinutes(10);
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

    private class DoomPlaquePrompt : Prompt
    {
        public override void OnResponse(Mobile from, string text)
        {
            if (!string.IsNullOrEmpty(text) && text.Trim().ToLower() == "wolfgang")
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(from.Location, Map.Malas, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023
                );
                from.MoveToWorld(TeleportDestination, Map.Malas);
                Effects.SendLocationParticles(
                    EffectItem.Create(TeleportDestination, Map.Malas, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023
                );
                return;
            }

            from.SendLocalizedMessage(1155663); // Nothing Happens
        }
    }
}
