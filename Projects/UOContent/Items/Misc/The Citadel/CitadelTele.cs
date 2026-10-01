using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>The Citadel's entrance — the "Citadel Crate" on Isamu-Jima. Spawned by the
/// XmlSpawner entry named "Citadel Crate" (Data/XmlSpawner/tokuno.xml, Tokuno 1344,769,21),
/// which was silently failing because no class of this name existed: the whole Citadel
/// cluster had been ported (Mobiles/The Citadel/, the DragonFlameKey/SerpentFangKey trophy
/// keys, MantleOfTheFallen, ResonantStaffofEnlightenment) but the dungeon had no way in.
///
/// NOT ported from a ServUO original — that source wasn't available here, so this is
/// written against what the shipped data already asserts:
///   - regions.json defines "The Citadel" as a Malas DungeonRegion covering 60,1855 ..
///     195,1990, GoLocation 106,1884,0 — that GoLocation is the arrival point used below,
///     the only interior coordinate ModernUO itself vouches for as standable.
///   - The same regions.json entry carries Entrance 1349,769,14, which matches the Tokuno
///     spawner's 1344,769 in y and to within 5 in x. The Entrance field has no map of its
///     own (it's implied to be the region's), so it can't express a Tokuno entrance to a
///     Malas interior — hence it reads as Malas there. The spawner is the authority on the
///     facet; this is the cross-facet entrance the region JSON can't spell.
///   - MondainsLegacy.IsMLRegion already lists "The Citadel", so the ML expansion gate
///     below uses the existing helper rather than a new check.
///
/// Shaped after BedlamTeleporter (Engines/ML Quests/Items/BedlamTeleporter.cs), the closest
/// existing analogue: a fixed, non-movable Item that teleports on double-click and brings
/// pets along. Unlike Bedlam this one is ungated — OSI's Citadel is freely enterable, the
/// gating is on Travesty's boss room via the two trophy keys, not on the dungeon door.
///
/// One-way by design: no return teleporter is created here, because nothing in the shipped
/// data names a safe tile inside the dungeon to put one on.</summary>
[SerializationGenerator(0, false)]
public partial class CitadelTele : Item
{
    private static readonly Point3D PointDest = new(106, 1884, 0);
    private static readonly Map MapDest = Map.Malas;

    [Constructible]
    public CitadelTele() : base(0x0E3D) // a crate
    {
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (!MondainsLegacy.CheckML(from))
        {
            return;
        }

        if (from is not PlayerMobile mobile)
        {
            return;
        }

        BaseCreature.TeleportPets(mobile, PointDest, MapDest);
        mobile.MoveToWorld(PointDest, MapDest);
    }
}
