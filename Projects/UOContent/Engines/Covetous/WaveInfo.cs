using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.VoidPool;

/// <summary>One wave of the Void Pool battle: the creatures it spawned and who gets credit for them.</summary>
public class WaveInfo
{
    public WaveInfo(int index, List<BaseCreature> creatures)
    {
        Wave = index;
        Creatures = creatures;
        Credit = new List<Mobile>();
    }

    public int Wave { get; }
    public List<BaseCreature> Creatures { get; }
    public List<Mobile> Credit { get; }
    public bool Cleared => Creatures.Count == 0;
}
