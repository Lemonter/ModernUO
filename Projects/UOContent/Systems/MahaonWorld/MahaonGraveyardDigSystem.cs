using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonWorld;

/// <summary>
///     Digging with a Shovel on graveyard ground (a GM-placed MahaonMarkerArea of kind
///     Graveyard — see MahaonMarkerAreaSystem/MahaonAreaMarkingTool) produces a
///     MahaonPandorasBox instead of ore, and carries a real risk: disturbing the dead. The
///     player's Luck stat rolls which box color comes up (Yellow rarest/best, Red most
///     common/worst — see MahaonPandorasBox); Spirit Speak skill reduces the separate,
///     independent chance of angry undead spawning nearby.
/// </summary>
public static class MahaonGraveyardDigSystem
{
    private const double BaseDisturbChance = 0.40; // before any Spirit Speak mitigation
    private const double MaxSpiritSpeakMitigation = 0.35; // 120 Spirit Speak cuts disturb chance by up to 35 points

    public static bool IsGraveyard(Mobile from) =>
        MahaonMarkerAreaSystem.IsInside(from, MahaonMarkerAreaKind.Graveyard);

    public static void TryDig(Mobile from)
    {
        if (from?.Backpack == null)
        {
            return;
        }

        from.Animate(11, 5, 1, true, false, 0); // digging animation, same as vanilla treasure map digging
        from.PlaySound(0x125);

        var box = new MahaonPandorasBox(RollColor(from));

        if (from.Backpack.TryDropItem(from, box, false) != true)
        {
            box.MoveToWorld(from.Location, from.Map);
        }

        from.SendMessage(0x59, $"Лопата натыкается на старый ларец — {MahaonPandorasBox.ColorNameRu(box.Color)}.");

        TryDisturbTheDead(from);
    }

    /// <summary>Higher Luck shifts the roll toward the better colors — same "roll biased by
    /// a stat, never guaranteed" shape as the box's own tier weights.</summary>
    private static MahaonPandorasBoxColor RollColor(Mobile from)
    {
        var luck = from.Luck;

        // Luck on this shard runs roughly 0-1000 same as OSI — normalize to a 0-100ish
        // bonus roll range so a capped-Luck digger noticeably favors Yellow/Green without
        // making Red functionally impossible.
        var luckBonus = System.Math.Clamp(luck / 10, 0, 100);
        var roll = Utility.Random(100) + Utility.RandomMinMax(0, luckBonus / 2);

        return roll switch
        {
            >= 130 => MahaonPandorasBoxColor.Yellow,
            >= 100 => MahaonPandorasBoxColor.Green,
            >= 60  => MahaonPandorasBoxColor.Blue,
            _       => MahaonPandorasBoxColor.Red
        };
    }

    private static void TryDisturbTheDead(Mobile from)
    {
        var spiritSpeak = from.Skills[SkillName.SpiritSpeak].Value;
        var mitigation = System.Math.Min(MaxSpiritSpeakMitigation, spiritSpeak / 120.0 * MaxSpiritSpeakMitigation);
        var disturbChance = System.Math.Max(0.02, BaseDisturbChance - mitigation);

        if (Utility.RandomDouble() >= disturbChance)
        {
            if (spiritSpeak > 0)
            {
                from.SendMessage(0x59, "Ваши навыки спиритизма помогли вам не потревожить мёртвых.");
            }

            return;
        }

        from.SendMessage(0x22, "Вы потревожили покой мёртвых...");

        var count = Utility.RandomMinMax(1, 3);

        for (var i = 0; i < count; i++)
        {
            var undead = Utility.RandomBool() ? (BaseCreature)new Skeleton() : new Zombie();

            var loc = from.Location;
            var map = from.Map;

            for (var attempt = 0; attempt < 10; attempt++)
            {
                var dx = Utility.RandomMinMax(-3, 3);
                var dy = Utility.RandomMinMax(-3, 3);
                var candidate = new Point3D(from.X + dx, from.Y + dy, from.Z);

                if (map.CanSpawnMobile(candidate))
                {
                    loc = candidate;
                    break;
                }
            }

            undead.MoveToWorld(loc, map);
            undead.Combatant = from;
        }
    }
}
