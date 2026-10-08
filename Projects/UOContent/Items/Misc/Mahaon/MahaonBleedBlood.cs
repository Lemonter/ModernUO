using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     A separate class from the vanilla Blood item specifically for
///     Systems.MahaonCombat.BleedingSystem — vanilla Blood self-deletes after 5 seconds,
///     which is fine for its usual "quick combat splash" use but doesn't match "kровь
///     держится минуту" for bleeding puddles specifically.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonBleedBlood : Item
{
    [Constructible]
    public MahaonBleedBlood(int itemId = 0x122A) : base(itemId)
    {
        Movable = false;
        Timer.StartTimer(TimeSpan.FromMinutes(1), Delete);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.StartTimer(TimeSpan.FromMinutes(1), Delete);
    }
}
