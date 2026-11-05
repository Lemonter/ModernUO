using System;
namespace Server.Items;

/// <summary>
///     The lifespan tooltip used by the two Exploring the Deep quest items that are worn equipment
///     (<see cref="WillemHartesHat" /> and <see cref="MercutiosCutlass" />) rather than
///     <see cref="BaseDecayingItem" />s. ServUO carried two hand-written copies of this block, one of
///     which printed English text; both now go through the same clilocs the client already localises.
/// </summary>
public static class QuestItemLifespan
{
    public static void AddTo(IPropertyList list, int seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);

        if (t.Days / 7 > 1)
        {
            list.Add(1153092, $"{t.Days / 7}"); // Lifespan: ~1_val~ weeks
        }
        else if (t.Days > 1)
        {
            list.Add(1153091, $"{t.Days}"); // Lifespan: ~1_val~ days
        }
        else if (t.Hours > 1)
        {
            list.Add(1153090, $"{t.Hours}"); // Lifespan: ~1_val~ hours
        }
        else if (t.Minutes > 1)
        {
            list.Add(1153089, $"{t.Minutes}"); // Lifespan: ~1_val~ minutes
        }
        else
        {
            list.Add(1072517, $"{seconds}"); // Lifespan: ~1_val~ seconds
        }
    }
}
