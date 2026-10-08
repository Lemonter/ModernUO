using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The hidden way out of the Crystal Field, the north-west pocket of the Prism of
/// Light. Spawned by one XmlSpawner entry per facet (felucca.xml / trammel.xml, 6509,87,-4),
/// which was silently failing because no class of this name existed.
///
/// NOT ported from a ServUO original — no such class exists in ServUO or anywhere findable,
/// so this is reconstructed from what the shipped data shows:
///   - 6509,87 sits inside the "The Prism of Light" DungeonRegion (6400,0 .. 6621,255).
///   - Its immediate neighbour at 6511,80 spawns a PrismaticCrystal, the ML quest item for
///     UnfadingMemoriesPartOne — so this pocket is the Crystal Field, and players are sent
///     here by a quest.
///   - teleporters.json already carries a one-way leg INTO the pocket
///     (6469,97,-50 -> 6503,88,0, "back": false) and no leg back out. That missing return
///     trip is what this item is for; community descriptions of the dungeon likewise mention
///     a hidden teleport in the Crystal Field.
///
/// DESTINATION IS AN INFORMED GUESS. The real OSI target isn't recorded in any file in this
/// project. It sends players to the Prism of Light's own GoLocation (6474,188,0 in
/// regions.json) — the one interior coordinate ModernUO itself vouches for as standable, and
/// far enough from the inbound teleporter at 6469,97 that there's no risk of bouncing
/// straight back into the Crystal Field. Change PointDest below if you learn the real one.
///
/// MapDest is deliberately left null: Teleporter.DoTeleport falls back to the mobile's own
/// map, so this single class serves both the Felucca and the Trammel copy without a facet
/// baked in.</summary>
[SerializationGenerator(0, false)]
public partial class CrystalFieldTele : Teleporter
{
    [Constructible]
    public CrystalFieldTele() : base(new Point3D(6474, 188, 0))
    {
        Visible = false;
    }
}
