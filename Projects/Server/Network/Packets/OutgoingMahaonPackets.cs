using System;
using System.Buffers;

namespace Server.Network;

public enum StaticOverrideAction : byte
{
    Add = 0,
    Remove = 1,
    Clear = 2
}

public static class OutgoingMahaonPackets
{
    // 0xF9 — cross-checked against every Handler.Add registration AND every declared
    // PacketsTable length. The only other hits for this byte anywhere in the Network
    // folder are inside the Blowfish/Twofish cipher key tables (random bytes, not packet
    // IDs). 0xF7 and 0xF6 both turned out to already be real vanilla packets (PacketList,
    // BoatMoving) registered later in the same client constructor, silently overwriting
    // ours — this time verified with a full cross-reference, not just a quick grep.
    //
    // Mahaon: by the time quest markers needed their own packet, every other byte from
    // 0xF0-0xFF was confirmed taken by real vanilla packets (re-checked directly against
    // the client's own PacketHandlers.cs/OutgoingPackets.cs/PacketsTable.cs) — rather than
    // gamble on an unverified ID somewhere in the middle of the space, 0xF9 now carries a
    // leading subtype byte. Subtype 0 is the static-graphic-override format, subtype 1 is
    // quest markers, subtype 2 is the skill tree sync.
    //
    // CRITICAL: 0xF9 is registered client-side (PacketsTable.cs) as VARIABLE length, not
    // fixed — every send below MUST follow [id:1][length:ushort BE][payload...] and call
    // WritePacketLength() as the very last step before Send. This was a real, live bug:
    // the client table originally had 0xF9 hardcoded to a fixed 19 bytes (correct only for
    // the original subtype-0-only single-purpose packet); the moment subtype 1 was added
    // it started either truncating reads or eating bytes belonging to the NEXT packet on
    // the wire, corrupting the entire stream after the first one sent. Never revert this
    // packet to a fixed-length, headerless format — any new subtype MUST fit the
    // [id][length][payload] shape or the client desyncs silently.
    private const byte MahaonMultiPacketId = 0xF9;
    private const byte SubtypeStaticGraphicOverride = 0;
    private const byte SubtypeQuestMarker = 1;
    private const byte SubtypeSkillTree = 2;

    private const byte StaticGraphicOverridePacketId = MahaonMultiPacketId;

    // 1 (id) + 2 (length) + 1 (subtype) + 2 (originalGraphic) + 2 (x) + 2 (y) + 1 (z)
    // + 2 (replacementGraphic) + 4 (overrideId) + 4 (durationSeconds) + 1 (action) = 22.
    private const int PacketLength = 22;

    public static void SendStaticGraphicOverride(
        this NetState ns, uint overrideId, int x, int y, int z,
        ushort originalGraphic, ushort replacementGraphic, uint durationSeconds, StaticOverrideAction action
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[PacketLength]);

        writer.Write(StaticGraphicOverridePacketId);
        writer.Write((ushort)0); // length placeholder — overwritten by WritePacketLength()
        writer.Write(SubtypeStaticGraphicOverride);
        writer.Write(originalGraphic);
        writer.Write((ushort)x);
        writer.Write((ushort)y);
        writer.Write((sbyte)z);
        writer.Write(replacementGraphic);
        writer.Write(overrideId);
        writer.Write(durationSeconds);
        writer.Write((byte)action);

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }

    // 1 (id) + 2 (length) + 1 (subtype) + 4 (npc serial) + 1 (state) = 9 bytes, fixed size.
    private const int QuestMarkerPacketLength = 9;

    /// <summary>state: 0=None (clears the marker), 1=Available ("!"), 2=InProgress ("?"
    /// grey), 3=Complete ("?" yellow) — matches Systems.MahaonQuests.QuestMarkerState on
    /// the server 1:1.</summary>
    public static void SendQuestMarker(this NetState ns, uint npcSerial, byte state)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[QuestMarkerPacketLength]);

        writer.Write(MahaonMultiPacketId);
        writer.Write((ushort)0); // length placeholder
        writer.Write(SubtypeQuestMarker);
        writer.Write(npcSerial);
        writer.Write(state);

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }

    // Checked the same way as 0xF9 above — cross-referenced against every client
    // Handler.Add registration and every PacketsTable entry, not just a quick grep.
    private const byte WindStatePacketId = 0xFC;

    // 1 (id) + 2 (direction, whole degrees) + 2 (strength*1000) + 2 (gust*1000)
    // + 4 (transition duration, ms) = 11 bytes, fixed size. NOTE: 0xFC is registered
    // client-side as a genuinely FIXED-length packet (confirmed in PacketsTable.cs) — no
    // length header here, unlike 0xF9 above. Do not add one; that would itself desync it.
    private const int WindStatePacketLength = 11;

    /// <summary>
    ///     Server-authoritative wind state — direction/strength/gust plus how long the
    ///     client should take interpolating toward it (0 = already there / snap instantly,
    ///     used for login sync and periodic resync). No per-frame data, no float support in
    ///     SpanWriter — direction is whole degrees (0-360), strength/gust are fixed-point
    ///     (value * 1000, so 0-2000 / 0-1000 as sent).
    /// </summary>
    public static void SendWindState(
        this NetState ns, float direction, float strength, float gustStrength, uint transitionDurationMs
    )
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var writer = new SpanWriter(stackalloc byte[WindStatePacketLength]);

        writer.Write(WindStatePacketId);
        writer.Write((ushort)Math.Round(direction));
        writer.Write((ushort)Math.Round(strength * 1000f));
        writer.Write((ushort)Math.Round(gustStrength * 1000f));
        writer.Write(transitionDurationMs);

        ns.Send(writer.Span);
    }
}
