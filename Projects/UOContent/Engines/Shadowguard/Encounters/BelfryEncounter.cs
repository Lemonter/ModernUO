using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

public class BelfryEncounter : ShadowguardEncounter
{
    public List<VileDrake> Drakes { get; set; }
    public ShadowguardGreaterDragon Dragon { get; set; }
    public List<Item> Bells { get; set; }

    public override Type AddonType => typeof(BelfryAddon);

    public BelfryEncounter() : base(EncounterType.Belfry)
    {
    }

    public BelfryEncounter(ShadowguardInstance instance) : base(EncounterType.Belfry, instance)
    {
    }

    public override void Setup()
    {
        Drakes = new List<VileDrake>();
        Bells = new List<Item>();

        var p = SpawnPoints[0];
        ConvertOffset(ref p);

        Dragon = new ShadowguardGreaterDragon();
        Dragon.MoveToWorld(p, Map.TerMur);

        AddBell(16, 6);
        AddBell(16, -7);
        AddBell(-20, -7);
        AddBell(-20, 6);
    }

    private void AddBell(int x, int y)
    {
        var bell = new FeedingBell();
        var p = new Point3D(x, y, 0);
        ConvertOffset(ref p);
        bell.MoveToWorld(p, Map.TerMur);
        Bells.Add(bell);
    }

    public void SpawnDrake(Point3D p, Mobile from)
    {
        if (Drakes == null)
        {
            return;
        }

        var rec = SpawnRecs[0];
        ConvertOffset(ref rec);

        foreach (var r in SpawnRecs)
        {
            var copy = r;
            ConvertOffset(ref copy);

            if (copy.Contains(p))
            {
                rec = copy;
                break;
            }
        }

        while (true)
        {
            var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width);
            var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height);
            var z = Map.TerMur.GetAverageZ(x, y);

            if (!Map.TerMur.CanSpawnMobile(x, y, z))
            {
                continue;
            }

            var drake = new VileDrake();
            drake.MoveToWorld(new Point3D(x, y, z), Map.TerMur);

            Timer.DelayCall(TimeSpan.FromSeconds(0.5), () => drake.Combatant = from);

            Drakes.Add(drake);
            break;
        }
    }

    public override void CheckEncounter()
    {
    }

    public override void OnCreatureKilled(BaseCreature bc)
    {
        if (bc is VileDrake drake && Drakes != null)
        {
            Drakes.Remove(drake);
        }

        if (bc == Dragon)
        {
            CompleteEncounter();
        }
    }

    public override void ClearItems()
    {
        if (Drakes != null)
        {
            foreach (var drake in Drakes.Where(d => d is { Deleted: false }).ToList())
            {
                drake.Delete();
            }

            Drakes = null;
        }

        if (Bells != null)
        {
            foreach (var bell in Bells.Where(b => b is { Deleted: false }).ToList())
            {
                bell.Delete();
            }

            Bells = null;
        }

        if (Dragon is { Alive: true })
        {
            Dragon.Delete();
        }

        Dragon = null;
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
        writer.WriteEncodedInt(0); // version

        writer.Write(Dragon);

        writer.WriteEncodedInt(Drakes?.Count ?? 0);
        if (Drakes != null)
        {
            foreach (var d in Drakes)
            {
                writer.Write(d);
            }
        }

        writer.WriteEncodedInt(Bells?.Count ?? 0);
        if (Bells != null)
        {
            foreach (var b in Bells)
            {
                writer.Write(b);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
        reader.ReadEncodedInt(); // version

        Drakes = new List<VileDrake>();
        Bells = new List<Item>();

        Dragon = reader.ReadEntity<Mobile>() as ShadowguardGreaterDragon;

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Mobile>() is VileDrake d)
            {
                Drakes.Add(d);
            }
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Item>() is FeedingBell b)
            {
                Bells.Add(b);
            }
        }

        if (Dragon is null or { Deleted: true })
        {
            Expire();
        }
    }
}
