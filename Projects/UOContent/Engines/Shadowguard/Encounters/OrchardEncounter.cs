using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

public class OrchardEncounter : ShadowguardEncounter
{
    public List<ShadowguardCypress> Trees { get; set; }
    public List<BaseCreature> Spawn { get; set; }

    public Item Bones { get; set; }
    public ShadowguardApple Apple { get; set; }

    public override Type AddonType => typeof(OrchardAddon);

    public OrchardEncounter() : base(EncounterType.Orchard)
    {
    }

    public OrchardEncounter(ShadowguardInstance instance) : base(EncounterType.Orchard, instance)
    {
    }

    public override void Setup()
    {
        Trees = new List<ShadowguardCypress>();
        Spawn = new List<BaseCreature>();

        var points = new List<Point3D>();
        for (var i = 0; i < SpawnPoints.Length; i++)
        {
            var p = SpawnPoints[i];
            ConvertOffset(ref p);
            points.Add(p);
        }

        foreach (VirtueType i in Enum.GetValues(typeof(VirtueType)))
        {
            if ((int)i > 7)
            {
                break;
            }

            var tree = new ShadowguardCypress(this, i);
            var p = points[Utility.Random(points.Count)];

            tree.MoveToWorld(p, Map.TerMur);
            points.Remove(p);
            Trees.Add(tree);

            tree = new ShadowguardCypress(this, (VirtueType)((int)i + 8));
            p = points[Utility.Random(points.Count)];

            tree.MoveToWorld(p, Map.TerMur);
            points.Remove(p);
            Trees.Add(tree);
        }

        Item bones = new WitheringBones();
        var pnt = new Point3D(-15, -11, 0);
        ConvertOffset(ref pnt);
        bones.MoveToWorld(pnt, Map.TerMur);
        Bones = bones;
    }

    public override void CheckEncounter()
    {
        if (Trees == null)
        {
            return;
        }

        var treeCount = Trees.Count(tree => tree is { Deleted: false });

        if (treeCount <= 0)
        {
            CompleteEncounter();
        }
    }

    public override void CompleteEncounter()
    {
        base.CompleteEncounter();
        ClearSpawn();
    }

    private void ClearSpawn()
    {
        if (Spawn == null)
        {
            return;
        }

        foreach (var spawn in Spawn.Where(e => e is { Alive: true }).ToList())
        {
            spawn.Delete();
        }

        Spawn = null;
    }

    public void AddSpawn(BaseCreature bc) => Spawn?.Add(bc);

    public override void ClearItems()
    {
        ClearSpawn();

        Apple?.Delete();

        if (Trees != null)
        {
            foreach (var tree in Trees.Where(t => t is { Deleted: false }).ToList())
            {
                tree.Delete();
            }

            Trees = null;
        }

        if (Bones != null)
        {
            Bones.Delete();
            Bones = null;
        }
    }

    public void OnApplePicked()
    {
        if (Trees == null)
        {
            return;
        }

        foreach (var tree in Trees.Where(t => t is { Deleted: false }))
        {
            if (tree.Foilage != null)
            {
                tree.Foilage.ItemID--;
            }
        }
    }

    public void OnAppleDeleted()
    {
        if (Trees == null)
        {
            return;
        }

        foreach (var tree in Trees.Where(t => t is { Deleted: false }))
        {
            if (tree.Foilage != null)
            {
                tree.Foilage.ItemID++;
            }
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(Trees?.Count ?? 0);
        if (Trees != null)
        {
            foreach (var t in Trees)
            {
                writer.Write(t);
            }
        }

        writer.WriteEncodedInt(Spawn?.Count ?? 0);
        if (Spawn != null)
        {
            foreach (var s in Spawn)
            {
                writer.Write(s);
            }
        }

        writer.Write(Bones);
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
        reader.ReadEncodedInt(); // version

        Trees = new List<ShadowguardCypress>();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Item>() is ShadowguardCypress tree)
            {
                Trees.Add(tree);
            }
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            Spawn ??= new List<BaseCreature>();

            if (reader.ReadEntity<Mobile>() is BaseCreature bc)
            {
                Spawn.Add(bc);
            }
        }

        Bones = reader.ReadEntity<Item>();
    }
}
