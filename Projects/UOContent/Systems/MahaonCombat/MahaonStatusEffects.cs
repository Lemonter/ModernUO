using System;
using System.Buffers;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonCombat;

/// <summary>Состояния персонажа, которые клиент показывает в мире (как зелёный цвет яда).</summary>
public enum MahaonStatus : byte
{
    Burning = 1,
    Bleeding = 2,
    Fracture = 3
}

/// <summary>
///     Пакет 0xF9, подтип 9: «у этого существа такое-то состояние» — горит, истекает кровью,
///     сломана кость. Для Unity: рисовать огонь на персонаже, капли крови, хромоту.
///
///     Формат: byte subtype = 9, uint serial, byte status (MahaonStatus), byte active (0/1),
///     ushort secondsLeft, byte detail (для перелома — 0 руки, 1 ноги, 2 кисти; иначе 0).
///
///     Кому: всем в радиусе обзора, чей клиент понимает новый формат (прислал хоть один
///     входящий 0x10–0x18 — то же правило, что для подтипов 7 и 8; старый ClassicUO на
///     незнакомом подтипе ломается). Состояние шлётся при начале, на каждом тике (так узнаёт и
///     тот, кто подошёл позже) и при окончании.
/// </summary>
public static class MahaonStatusEffects
{
    public const byte Subtype = 9;
    private const int Length = 3 + 1 + 4 + 1 + 1 + 2 + 1;

    public static void Broadcast(Mobile m, MahaonStatus status, bool active, int secondsLeft, byte detail = 0)
    {
        if (m == null || m.Deleted)
        {
            return;
        }

        UpdateBuffBar(m, status, active, secondsLeft, detail);

        if (m.Map == null || m.Map == Map.Internal)
        {
            return;
        }

        var buffer = Build(m.Serial, status, active, secondsLeft, detail);

        foreach (var ns in m.Map.GetClientsInRange(m.Location))
        {
            if (MahaonCombatWindow.SpeaksNewUi(ns) && ns.Mobile?.CanSee(m) == true)
            {
                ns.Send(buffer);
            }
        }
    }

    // ---- Полоса бафов и дебафов (стандартный пакет 0xDF) -------------------------------------
    //
    // Тот же статус — значком в полосе дебафов у самого игрока: её рисует и старый клиент, и
    // Unity. Заголовки — готовые русские строки cliloc («Кровотечение», «Горение», «Ранение»),
    // описание — универсальная строка 1114057 «~1_val~» с нашим текстом.

    private const int TextCliloc = 1114057;

    private static void UpdateBuffBar(Mobile m, MahaonStatus status, bool active, int secondsLeft, byte detail)
    {
        if (m is not PlayerMobile pm)
        {
            return;
        }

        var (icon, title) = status switch
        {
            MahaonStatus.Burning  => (BuffIcon.FanDancerFanFire, 1161762), // «Горение»
            MahaonStatus.Bleeding => (BuffIcon.Bleed, 1075829),            // «Кровотечение»
            _                     => (BuffIcon.MortalStrike, 1161767)      // «Ранение» — перелом
        };

        if (!active)
        {
            pm.RemoveBuff(icon);
            return;
        }

        pm.AddBuff(new BuffInfo(icon, title, TextCliloc, TimeSpan.FromSeconds(Math.Max(1, secondsLeft)), BuffText(m, status, detail)));
    }

    private static string BuffText(Mobile m, MahaonStatus status, byte detail) => status switch
    {
        MahaonStatus.Burning  => $"Огонь жжёт каждую секунду ({BurningSystem.DamagePerTick(m)} урона). Вода или дождь потушат.",
        MahaonStatus.Bleeding => "Каждую секунду уходит 1–2% здоровья. Остановит только бинт.",
        _ => detail switch
        {
            0 => "Сломана рука: удары оружием слабее на 30%. Срастётся со временем.",
            1 => "Сломана нога: бегать нельзя, только шагом. Срастётся со временем.",
            _ => "Сломана кисть: каждое заклинание ещё и с шансом 50% сорвётся. Срастётся со временем."
        }
    };

    internal static byte[] Build(Serial serial, MahaonStatus status, bool active, int secondsLeft, byte detail)
    {
        var writer = new SpanWriter(stackalloc byte[Length]);

        writer.Write((byte)0xF9);
        writer.Write((ushort)Length);
        writer.Write(Subtype);
        writer.Write(serial);
        writer.Write((byte)status);
        writer.Write((byte)(active ? 1 : 0));
        writer.Write((ushort)Math.Clamp(secondsLeft, 0, ushort.MaxValue));
        writer.Write(detail);

        return writer.Span.ToArray();
    }
}
