using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Region.cs) — the jail is a safe zone: no player can be
///     harmed inside it, which is what makes the captured-and-returned loop survivable.
/// </summary>
public class WrongJailRegion : DungeonRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public WrongJailRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public WrongJailRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public WrongJailRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override bool AllowHarmful(Mobile from, Mobile target) => false;
}
