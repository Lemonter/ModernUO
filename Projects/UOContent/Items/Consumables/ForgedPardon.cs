using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Статическая таблица помилований не сохраняется в мире (хуков
///     статического Save/Load здесь нет), поэтому после перезапуска сбрасывается.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ForgedPardon : Item
{
    private static readonly Dictionary<Mobile, DateTime> _table = new();

    [Constructible]
    public ForgedPardon() : base(10289) => Hue = 1177;

    public override int LabelNumber => 1116234;

    public static bool CanUsePardon(Mobile from)
    {
        Defrag();

        if (_table.ContainsKey(from))
        {
            from.SendLocalizedMessage(1116587); // You must wait 24 hours before using another forged pardon.
            return false;
        }

        if (Spells.SpellHelper.CheckCombat(from))
        {
            from.SendLocalizedMessage(1116588); // You cannot use a forged pardon while in combat.
            return false;
        }

        return true;
    }

    public static void Defrag()
    {
        var toRemove = new List<Mobile>();

        foreach (var kvp in _table)
        {
            if (kvp.Value < Core.Now)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var mob in toRemove)
        {
            _table.Remove(mob);
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042004); // That must be in your pack for you to use it.
            return;
        }

        if (from.Kills <= 0)
        {
            from.SendMessage("Вам этот предмет не нужен.");
            return;
        }

        if (!CanUsePardon(from))
        {
            return;
        }

        from.Kills--;

        from.SendLocalizedMessage(1116208); // Your murder count has been successfully updated.
        Delete();

        _table[from] = Core.Now + TimeSpan.FromHours(24);
    }
}
