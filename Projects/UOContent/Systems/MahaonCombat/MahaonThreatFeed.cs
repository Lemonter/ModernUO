using System;
using System.Buffers;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Кто прямо сейчас дерётся против игрока — список, который клиент сам собрать не
///     может.
///
///     Автопереключение цели на клиенте (MahaonAutoTargetManager) до сих пор выбирало
///     «ближайшего недружественного» по цвету имени. Цвет имени о нападении не говорит
///     ничего: серым светится и мирный олень, и чужой питомец, и поднятая кем-то нежить,
///     поэтому после смерти цели прицел прыгал на первого попавшегося соседа. Кто на кого
///     напал, знает только сервер — Mobile.Combatant и списки агрессоров живут здесь.
///
///     Список уходит пакетом 0xF9, подтип 3, и только когда он изменился: в спокойной
///     обстановке он пуст и в сеть не идёт вовсе.
/// </summary>
public static class MahaonThreatFeed
{
    private const byte MahaonMultiPacketId = 0xF9;
    private const byte SubtypeThreatList = 3;

    /// <summary>Дальше этого противник до игрока всё равно не дотянется, а прицел туда
    /// прыгать не должен.</summary>
    private const int Range = 18;

    /// <summary>Потолок на список: если против игрока дерётся полсотни существ, ближайшие
    /// из них всё равно найдутся среди первых.</summary>
    private const int MaxThreats = 32;

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1.0);

    private static readonly Dictionary<Mobile, uint[]> _lastSent = new();

    public static void Initialize()
    {
        Timer.DelayCall(Interval, Interval, Tick);
    }

    private static void Tick()
    {
        var threats = new List<uint>(MaxThreats);

        foreach (var ns in NetState.Instances)
        {
            if (ns.Mobile is not PlayerMobile player || player.Deleted || player.Map == null ||
                player.Map == Map.Internal)
            {
                continue;
            }

            threats.Clear();
            Collect(player, threats);

            if (!Changed(player, threats))
            {
                continue;
            }

            _lastSent[player] = threats.ToArray();
            Send(ns, threats);
        }

        // Игроки уходят из игры чаще, чем этот словарь чистится сам.
        if (_lastSent.Count > 256)
        {
            Prune();
        }
    }

    private static void Collect(PlayerMobile player, List<uint> threats)
    {
        foreach (var m in player.GetMobilesInRange(Range))
        {
            if (threats.Count >= MaxThreats)
            {
                break;
            }

            if (m == player || m.Deleted || !m.Alive || m.Combatant != player)
            {
                continue;
            }

            // Свой же питомец, которому велели охранять, дерётся «против» хозяина в смысле
            // Combatant крайне редко, но чужой — запросто; проверка на хозяина отсекает
            // собственных зверей и призванных союзников.
            if (m is BaseCreature { Controlled: true } bc && bc.ControlMaster == player)
            {
                continue;
            }

            threats.Add(m.Serial.Value);
        }
    }

    private static bool Changed(Mobile player, List<uint> threats)
    {
        if (!_lastSent.TryGetValue(player, out var previous))
        {
            return threats.Count > 0;
        }

        if (previous.Length != threats.Count)
        {
            return true;
        }

        for (var i = 0; i < previous.Length; i++)
        {
            if (previous[i] != threats[i])
            {
                return true;
            }
        }

        return false;
    }

    private static void Prune()
    {
        var gone = new List<Mobile>();

        foreach (var m in _lastSent.Keys)
        {
            if (m.Deleted || m.NetState == null)
            {
                gone.Add(m);
            }
        }

        foreach (var m in gone)
        {
            _lastSent.Remove(m);
        }
    }

    // [subtype:1][count:1][serial:4 BE]*count
    private static void Send(NetState ns, List<uint> threats)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        // 1 (id) + 2 (длина) + 1 (подтип) + 1 (счётчик) + 4 на серийник
        var length = 5 + threats.Count * 4;
        var writer = new SpanWriter(stackalloc byte[length]);

        writer.Write(MahaonMultiPacketId);
        writer.Write((ushort)0); // место под длину — проставит WritePacketLength()
        writer.Write(SubtypeThreatList);
        writer.Write((byte)threats.Count);

        for (var i = 0; i < threats.Count; i++)
        {
            writer.Write(threats[i]);
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }
}
