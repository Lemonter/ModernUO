using System;
using System.Collections.Generic;
using Server.Commands;

namespace Server.Systems.MahaonSoulStones;

public enum TattooType
{
    SoulCatcher // required to have a chance at soul stone drops from monsters
    // Room to add more here (the original server had others the player couldn't recall
    // specifics of) — extend this enum and TattooDurations/handle effects as they're decided.
}

/// <summary>
///     Mahaon tattoos: temporary marks that grant a bonus/effect for a fixed duration
///     (originally a week). Implemented as a lightweight expiry-tracked flag rather than a
///     worn item, since classic UO has no server-side skin-decal layer — the "tattoo" is
///     conceptually applied directly to the character.
/// </summary>
public class TattooSystem : GenericPersistence
{
    private static TattooSystem _instance;

    private static readonly TimeSpan DefaultDuration = TimeSpan.FromDays(7);

    private static readonly Dictionary<Mobile, Dictionary<TattooType, DateTime>> ActiveTattoos = new();

    public TattooSystem() : base("MahaonTattoos", 1)
    {
    }

    public static void Configure()
    {
        _instance = new TattooSystem();
        CommandSystem.Register("ApplyTattoo", AccessLevel.GameMaster, ApplyTattoo_OnCommand);
    }

    public static void Initialize()
    {
        Timer.DelayCall(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10), CleanupExpired);
    }

    public static void ApplyTattoo(Mobile m, TattooType type, TimeSpan? duration = null)
    {
        if (!ActiveTattoos.TryGetValue(m, out var tattoos))
        {
            ActiveTattoos[m] = tattoos = new Dictionary<TattooType, DateTime>();
        }

        tattoos[type] = Core.Now + (duration ?? DefaultDuration);
        m.SendMessage(0x59, $"Тату «{type}» приживается. Оно исчезнет через {(duration ?? DefaultDuration).TotalDays:F0} дн.");
    }

    public static bool HasActiveTattoo(Mobile m, TattooType type) =>
        ActiveTattoos.TryGetValue(m, out var tattoos)
        && tattoos.TryGetValue(type, out var expires)
        && Core.Now < expires;

    private static void CleanupExpired()
    {
        List<Mobile> emptyOwners = null;

        foreach (var (owner, tattoos) in ActiveTattoos)
        {
            List<TattooType> expired = null;

            foreach (var (type, expires) in tattoos)
            {
                if (Core.Now >= expires)
                {
                    (expired ??= new List<TattooType>()).Add(type);
                }
            }

            if (expired != null)
            {
                foreach (var type in expired)
                {
                    tattoos.Remove(type);
                }
            }

            if (tattoos.Count == 0)
            {
                (emptyOwners ??= new List<Mobile>()).Add(owner);
            }
        }

        if (emptyOwners != null)
        {
            foreach (var owner in emptyOwners)
            {
                ActiveTattoos.Remove(owner);
            }
        }
    }

    [Usage("ApplyTattoo <player> <type>")]
    [Description("GM tool: applies a tattoo to a player for testing.")]
    private static void ApplyTattoo_OnCommand(CommandEventArgs e)
    {
        if (e.Mobile is not { } gm || e.Length < 2)
        {
            e.Mobile.SendMessage("Использование: [ApplyTattoo <игрок> <тип>");
            return;
        }

        var playerName = e.GetString(0);
        Mobile target = null;

        foreach (var m in World.Mobiles.Values)
        {
            if (m.RawName.InsensitiveEquals(playerName))
            {
                target = m;
                break;
            }
        }

        if (target == null)
        {
            gm.SendMessage($"Игрок '{playerName}' не найден.");
            return;
        }

        if (!Enum.TryParse<TattooType>(e.GetString(1), true, out var type))
        {
            gm.SendMessage("Неизвестный тип тату.");
            return;
        }

        ApplyTattoo(target, type);
        gm.SendMessage($"Тату «{type}» применено к {target.Name}.");
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(ActiveTattoos.Count);

        foreach (var (owner, tattoos) in ActiveTattoos)
        {
            writer.Write(owner);
            writer.WriteEncodedInt(tattoos.Count);

            foreach (var (type, expires) in tattoos)
            {
                writer.WriteEncodedInt((int)type);
                writer.Write(expires);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var ownerCount = reader.ReadEncodedInt();
        for (var i = 0; i < ownerCount; i++)
        {
            var owner = reader.ReadEntity<Mobile>();
            var tattooCount = reader.ReadEncodedInt();

            var tattoos = new Dictionary<TattooType, DateTime>();
            for (var j = 0; j < tattooCount; j++)
            {
                var type = (TattooType)reader.ReadEncodedInt();
                var expires = reader.ReadDateTime();
                tattoos[type] = expires;
            }

            if (owner != null)
            {
                ActiveTattoos[owner] = tattoos;
            }
        }
    }
}
