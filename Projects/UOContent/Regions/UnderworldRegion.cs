using System.Text.Json.Serialization;

namespace Server.Regions;

// Ported from JustUO (github.com/JustUO/JustUO, a ServUO fork) — ServUO itself never
// implemented Time of Legends chapter 2 content (confirmed empty on master/pub57/p58-wip).
// Base behavior (no recall/mark/gate/sacred journey, no player housing) is already covered
// by inheriting NoTravelSpellsAllowedRegion/DungeonRegion — only the entrance flavor message
// is new here.
public class UnderworldRegion : NoTravelSpellsAllowedRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public UnderworldRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public UnderworldRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    public UnderworldRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);

        // You observe the remains of four humans here. As you observe the tragic scene, ...
        m.SendLocalizedMessage(1094954);
    }
}
