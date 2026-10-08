using Server.Commands;
using Server.Items;

namespace Server.Engines.Peerless;

/// <summary>Places the six peerless encounters in the world: each altar, the teleporter that
/// gets players back out, the Prism of Light's six pedestals and the Paroxysmus arena gate.
///
/// Coordinates are ServUO's, from the ML generation routine in
/// Scripts/Services/Expansions/MondainsLegacy.cs. An earlier pass through this content reported
/// that ServUO ships no altar placement and that they had to be set by hand — that was wrong,
/// and came from reading a truncated listing rather than the file.
///
/// Placement is idempotent: an altar already standing on its tile is left alone, so the command
/// is safe to re-run. Everything it creates is tagged in WeakEntityCollection under "peerless",
/// so `[DeletePeerless` takes the whole lot back out — the same pattern as `[SetupDespise`.</summary>
public static class PeerlessGeneration
{
    private const string Tag = "peerless";

    public static void Initialize()
    {
        CommandSystem.Register("GenPeerless", AccessLevel.Developer, GenPeerless_OnCommand);
        CommandSystem.Register("DeletePeerless", AccessLevel.Developer, DeletePeerless_OnCommand);
    }

    [Usage("GenPeerless")]
    [Description("Places the six peerless altars, their exit teleporters and fittings.")]
    private static void GenPeerless_OnCommand(CommandEventArgs e)
    {
        var from = e.Mobile;
        var placed = 0;

        // Bedlam - Malas
        placed += Place(new BedlamAltar(), new Point3D(86, 1627, 0), Map.Malas, new Point3D(99, 1617, 50));

        // Blighted Grove - both facets
        placed += Place(new BlightedGroveAltar(), new Point3D(6502, 875, 0), Map.Trammel, new Point3D(6511, 949, 26));
        placed += Place(new BlightedGroveAltar(), new Point3D(6502, 875, 0), Map.Felucca, new Point3D(6511, 949, 26));

        // Palace of Paroxysmus - both facets, plus the arena gate
        placed += PlaceParoxysmus(Map.Trammel);
        placed += PlaceParoxysmus(Map.Felucca);

        // Prism of Light - both facets, plus six pedestals
        placed += PlacePrism(Map.Trammel);
        placed += PlacePrism(Map.Felucca);

        // The Citadel - Malas
        placed += Place(new CitadelAltar(), new Point3D(89, 1885, 0), Map.Malas, new Point3D(111, 1955, 0));

        // Twisted Weald - Ilshenar
        placed += Place(
            new TwistedWealdAltar(),
            new Point3D(2170, 1255, -60),
            Map.Ilshenar,
            new Point3D(2139, 1271, -57)
        );

        from.SendMessage($"Peerless generation complete: {placed} altar(s) placed.");
    }

    [Usage("DeletePeerless")]
    [Description("Removes everything [GenPeerless placed.")]
    private static void DeletePeerless_OnCommand(CommandEventArgs e)
    {
        WeakEntityCollection.Delete(Tag);
        e.Mobile.SendMessage("Peerless altars removed.");
    }

    private static int Place(PeerlessAltar altar, Point3D location, Map map, Point3D telePoint)
    {
        if (Exists(altar.GetType(), location, map))
        {
            altar.Delete();
            return 0;
        }

        WeakEntityCollection.Add(Tag, altar);
        altar.MoveToWorld(location, map);

        var tele = new PeerlessTeleporter(altar) { PointDest = altar.ExitDest };
        WeakEntityCollection.Add(Tag, tele);
        tele.MoveToWorld(telePoint, map);

        return 1;
    }

    private static int PlaceParoxysmus(Map map)
    {
        var altar = new ParoxysmusAltar();
        var placed = Place(altar, new Point3D(6511, 506, -34), map, new Point3D(6518, 365, 46));

        if (placed == 0)
        {
            return 0;
        }

        var gate = new ParoxysmusIronGate { Altar = altar };
        WeakEntityCollection.Add(Tag, gate);
        gate.MoveToWorld(new Point3D(6518, 492, -50), map);

        return placed;
    }

    /// <summary>The Prism's altar is invisible and its offering goes on six pedestals standing
    /// around it — three in one colour, three in another, as in the original. Its exit
    /// teleporter is the one that's actually visible, drawn as a moongate.</summary>
    private static int PlacePrism(Map map)
    {
        var altar = new PrismOfLightAltar();

        if (Exists(typeof(PrismOfLightAltar), new Point3D(6509, 167, 6), map))
        {
            altar.Delete();
            return 0;
        }

        WeakEntityCollection.Add(Tag, altar);
        altar.MoveToWorld(new Point3D(6509, 167, 6), map);

        var tele = new PeerlessTeleporter(altar)
        {
            PointDest = altar.ExitDest,
            Visible = true,
            ItemID = 0xDDA
        };

        WeakEntityCollection.Add(Tag, tele);
        tele.MoveToWorld(new Point3D(6501, 137, -20), map);

        AddPillar(altar, 0, 0x581, new Point3D(6506, 167, 0), map);
        AddPillar(altar, 1, 0x581, new Point3D(6509, 164, 0), map);
        AddPillar(altar, 2, 0x581, new Point3D(6506, 164, 0), map);
        AddPillar(altar, 3, 0x481, new Point3D(6512, 167, 0), map);
        AddPillar(altar, 4, 0x481, new Point3D(6509, 170, 0), map);
        AddPillar(altar, 5, 0x481, new Point3D(6512, 170, 0), map);

        return 1;
    }

    private static void AddPillar(PrismOfLightAltar altar, int id, int hue, Point3D location, Map map)
    {
        var pillar = new PrismOfLightPillar(altar, hue) { PillarId = id };

        altar.AddToPedestals(pillar);
        WeakEntityCollection.Add(Tag, pillar);

        pillar.MoveToWorld(location, map);
    }

    private static bool Exists(System.Type type, Point3D location, Map map)
    {
        if (map == null)
        {
            return false;
        }

        foreach (var item in map.GetItemsAt(location))
        {
            if (item.GetType() == type)
            {
                return true;
            }
        }

        return false;
    }
}
