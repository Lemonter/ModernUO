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
    private const byte StaticGraphicOverridePacketId = 0xF9;

    // 1 (id) + 2 (originalGraphic) + 2 (x) + 2 (y) + 1 (z) + 2 (replacementGraphic)
    // + 4 (overrideId) + 4 (durationSeconds) + 1 (action) = 19 bytes, fixed size.
    private const int PacketLength = 19;

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
        writer.Write(originalGraphic);
        writer.Write((ushort)x);
        writer.Write((ushort)y);
        writer.Write((sbyte)z);
        writer.Write(replacementGraphic);
        writer.Write(overrideId);
        writer.Write(durationSeconds);
        writer.Write((byte)action);

        ns.Send(writer.Span);
    }

    // Checked the same way as 0xF9 above — cross-referenced against every client
    // Handler.Add registration and every PacketsTable entry, not just a quick grep.
    private const byte WindStatePacketId = 0xFC;

    // 1 (id) + 2 (direction, whole degrees) + 2 (strength*1000) + 2 (gust*1000)
    // + 4 (transition duration, ms) = 11 bytes, fixed size.
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
