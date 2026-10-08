using System;
using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

public enum CastingChannel : byte
{
    Hands = 0, // always available — the classic baseline
    Voice = 1, // needs VoiceMagerySkillRequired Magery
    Mind = 2   // needs MindMagerySkillRequired Magery
}

/// <summary>
///     How a player channels their spells — picked on the "Магия" tab of
///     MahaonCombatMenuGump, persists until changed (survives a restart). Each channel has
///     its own weak point: get hit there while it's your active channel and you can't cast
///     for a few seconds. Voice/Mind unlock as Magery grows; Hands is always available.
/// </summary>
public static class CastingChannelSystem
{
    private const double VoiceMagerySkillRequired = 50.0;
    private const double MindMagerySkillRequired = 100.0;

    private static readonly Dictionary<Mobile, CastingChannel> Channel = new();
    private static readonly Dictionary<Mobile, DateTime> BlockUntil = new();
    private static readonly TimeSpan BlockDuration = TimeSpan.FromSeconds(5);

    private static Persistence _persistence;

    public static void Configure()
    {
        _persistence = new Persistence();
    }

    public static CastingChannel GetChannel(Mobile m) =>
        Channel.TryGetValue(m, out var channel) ? channel : CastingChannel.Hands;

    public static bool CanUseChannel(Mobile m, CastingChannel channel)
    {
        var magery = m.Skills[SkillName.Magery].Value;

        return channel switch
        {
            CastingChannel.Voice => magery >= VoiceMagerySkillRequired,
            CastingChannel.Mind  => magery >= MindMagerySkillRequired,
            _                     => true
        };
    }

    public static void SetChannel(Mobile m, CastingChannel channel) => Channel[m] = channel;

    // Which hit location interrupts each channel — Hands channel is broken by a hands hit
    // (classic "your hands are busy/hurt"), Voice by a hit to the neck/throat, Mind by a
    // hit to the head.
    private static HitLocation? BlockingLocation(CastingChannel channel) => channel switch
    {
        CastingChannel.Hands => HitLocation.Hands,
        CastingChannel.Voice => HitLocation.Neck,
        CastingChannel.Mind  => HitLocation.Head,
        _                     => null
    };

    /// <summary>Called from HitLocationSystem.ConsumeHitBonus right after a called shot
    /// resolves — only interrupts casting if the location that was actually hit matches the
    /// DEFENDER's own currently-chosen channel (a hands hit does nothing to someone casting
    /// with their voice, and vice versa).</summary>
    public static void TryDisruptCasting(Mobile defender, HitLocation location)
    {
        var channel = GetChannel(defender);

        if (BlockingLocation(channel) != location)
        {
            return;
        }

        BlockUntil[defender] = Core.Now + BlockDuration;
    }

    public static bool IsCastingBlocked(Mobile m) =>
        BlockUntil.TryGetValue(m, out var until) && Core.Now < until;

    public static string GetBlockMessage(Mobile m) => GetChannel(m) switch
    {
        CastingChannel.Voice => "Тебе перебили горло — колдовать голосом сейчас невозможно.",
        CastingChannel.Mind  => "Тебя ударили по голове — сосредоточиться и колдовать мыслью сейчас невозможно.",
        _                     => "Твои руки слишком повреждены, чтобы колдовать."
    };

    public static string RuChannelName(CastingChannel channel) => channel switch
    {
        CastingChannel.Hands => "Руками",
        CastingChannel.Voice => "Голосом",
        CastingChannel.Mind  => "Мыслью",
        _                     => channel.ToString()
    };

    private sealed class Persistence : GenericPersistence
    {
        public Persistence() : base("MahaonCastingChannel", 1)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version
            writer.WriteEncodedInt(Channel.Count);

            foreach (var (mobile, channel) in Channel)
            {
                writer.Write(mobile);
                writer.Write((byte)channel);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version

            var count = reader.ReadEncodedInt();
            for (var i = 0; i < count; i++)
            {
                var mobile = reader.ReadEntity<Mobile>();
                var channel = (CastingChannel)reader.ReadByte();

                if (mobile != null)
                {
                    Channel[mobile] = channel;
                }
            }
        }
    }
}
