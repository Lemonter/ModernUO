using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

public class BarEncounter : ShadowguardEncounter
{
    public const int LiquorCount = 10;

    public override Type AddonType => typeof(BarAddon);

    public List<Mobile> Pirates { get; set; }
    public int Wave { get; set; }

    public List<ShadowguardBottleOfLiquor> Bottles { get; set; }

    public BarEncounter() : base(EncounterType.Bar)
    {
    }

    public BarEncounter(ShadowguardInstance instance) : base(EncounterType.Bar, instance)
    {
    }

    public override void Setup()
    {
        Pirates = new List<Mobile>();
        Bottles = new List<ShadowguardBottleOfLiquor>();
        Wave = 0;

        var toSpawn = Math.Max(3, PartySize() * 3);

        for (var i = 0; i < toSpawn; i++)
        {
            SpawnRandomPirate();
        }

        for (var i = 0; i < LiquorCount; i++)
        {
            SpawnRandomLiquor();
        }
    }

    public override void CheckEncounter()
    {
        if (Completed || Bottles == null)
        {
            return;
        }

        var liquorCount = Bottles.Count(b => b is { Deleted: false });

        if (liquorCount < LiquorCount)
        {
            SpawnRandomLiquor();
        }
    }

    private void SpawnRandomLiquor()
    {
        var row = Utility.Random(4);
        var rec = SpawnRecs[Utility.Random(SpawnRecs.Length)];

        ConvertOffset(ref rec);

        var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width);
        var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height);
        const int z = -14;

        x += 9 * row;

        var bottle = new ShadowguardBottleOfLiquor(this);
        bottle.MoveToWorld(new Point3D(x, y, z), Map.TerMur);

        Bottles.Add(bottle);
    }

    private void SpawnRandomPirate()
    {
        if (Pirates == null)
        {
            return;
        }

        var row = Utility.Random(8);
        var ranPnt = SpawnPoints[Utility.Random(SpawnPoints.Length)];

        ConvertOffset(ref ranPnt);

        var a = row % 2 == 0 ? 0 : 3;
        var startX = ranPnt.X + a;
        var x = startX + row / 2 * 9;

        var pirate = new ShadowguardPirate();
        pirate.MoveToWorld(new Point3D(x, ranPnt.Y, ranPnt.Z), Map.TerMur);
        Pirates.Add(pirate);
    }

    public override void OnCreatureKilled(BaseCreature bc)
    {
        if (bc is not ShadowguardPirate || Pirates == null)
        {
            return;
        }

        Pirates.Remove(bc);

        if (Pirates.Count > 0)
        {
            return;
        }

        Wave++;
        Pirates.Clear();

        var toSpawn = Math.Max(3, PartySize() * 3);

        if (Wave < 4)
        {
            for (var i = 0; i < toSpawn; i++)
            {
                SpawnRandomPirate();
            }
        }
        else if (Wave == 4)
        {
            var pirate = new ShantyThePirate();
            var p = SpawnPoints[Utility.Random(SpawnPoints.Length)];
            ConvertOffset(ref p);
            pirate.MoveToWorld(p, Map.TerMur);
            Pirates.Add(pirate);
        }
        else
        {
            CompleteEncounter();
        }
    }

    public override void ClearItems()
    {
        if (Bottles != null)
        {
            foreach (var bottle in Bottles.Where(b => b is { Deleted: false }).ToList())
            {
                bottle.Delete();
            }

            Bottles = null;
        }

        Pirates = null;
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(Wave);

        writer.WriteEncodedInt(Pirates?.Count ?? 0);
        if (Pirates != null)
        {
            foreach (var p in Pirates)
            {
                writer.Write(p);
            }
        }

        writer.WriteEncodedInt(Bottles?.Count ?? 0);
        if (Bottles != null)
        {
            foreach (var b in Bottles)
            {
                writer.Write(b);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
        reader.ReadEncodedInt(); // version

        Pirates = new List<Mobile>();
        Bottles = new List<ShadowguardBottleOfLiquor>();

        Wave = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Mobile>() is { } p)
            {
                Pirates.Add(p);
            }
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Item>() is ShadowguardBottleOfLiquor b)
            {
                Bottles.Add(b);
            }
        }

        if (Pirates.Count < 6)
        {
            var toSpawn = 6 - Pirates.Count;

            for (var i = 0; i < toSpawn; i++)
            {
                SpawnRandomPirate();
            }
        }
    }
}
