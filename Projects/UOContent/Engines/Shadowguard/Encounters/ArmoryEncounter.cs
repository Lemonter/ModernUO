using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

public class ArmoryEncounter : ShadowguardEncounter
{
    public List<Item> Armor { get; set; }
    public List<Item> DestroyedArmor { get; set; }
    public List<Item> Items { get; set; }
    public List<BaseCreature> Spawn { get; set; }

    public override Type AddonType => typeof(ArmoryAddon);

    public ArmoryEncounter() : base(EncounterType.Armory)
    {
    }

    public ArmoryEncounter(ShadowguardInstance instance) : base(EncounterType.Armory, instance)
    {
    }

    public override void Setup()
    {
        Armor = new List<Item>();
        DestroyedArmor = new List<Item>();
        Spawn = new List<BaseCreature>();
        Items = new List<Item>();

        var toSpawn = 1 + PartySize() * 2;

        for (var i = 0; i < SpawnPoints.Length; i++)
        {
            var p = SpawnPoints[i];
            ConvertOffset(ref p);

            var armor = new CursedSuitOfArmor(this);
            armor.MoveToWorld(p, Map.TerMur);
            Armor.Add(armor);

            if (i > 13)
            {
                armor.ItemID = 0x1512;
            }
        }

        for (var i = 0; i < toSpawn; i++)
        {
            SpawnRandom();
        }

        AddStatic(-4, 2, 0);
        AddStatic(-4, -4, 0);
        AddStatic(2, -4, 0);

        AddFlames(-4, 2, 8);
        AddFlames(-4, -4, 8);
        AddFlames(2, -4, 8);
    }

    private void AddStatic(int x, int y, int z)
    {
        Item item = new Static(3633);
        var pnt = new Point3D(x, y, z);
        ConvertOffset(ref pnt);
        item.MoveToWorld(pnt, Map.TerMur);
        Items.Add(item);
    }

    private void AddFlames(int x, int y, int z)
    {
        Item item = new PurifyingFlames();
        var pnt = new Point3D(x, y, z);
        ConvertOffset(ref pnt);
        item.MoveToWorld(pnt, Map.TerMur);
        Items.Add(item);
    }

    public override void CheckEncounter()
    {
        if (Completed || Armor == null)
        {
            return;
        }

        if (Armor.Count(a => a is { Deleted: false }) == 0)
        {
            CompleteEncounter();
        }
    }

    public override void OnCreatureKilled(BaseCreature bc)
    {
        if (Spawn != null && Spawn.Contains(bc))
        {
            Spawn.Remove(bc);
            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 15)), SpawnRandom);
        }
    }

    public void AddDestroyedArmor(Item item) => DestroyedArmor?.Add(item);

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

        foreach (var spawn in Spawn.Where(s => s is { Deleted: false }).ToList())
        {
            spawn.Delete();
        }

        Spawn = null;
    }

    public override void ClearItems()
    {
        if (Armor != null)
        {
            foreach (var armor in Armor.Where(i => i is { Deleted: false }).ToList())
            {
                armor.Delete();
            }

            Armor = null;
        }

        if (DestroyedArmor != null)
        {
            foreach (var dest in DestroyedArmor.Where(i => i is { Deleted: false }).ToList())
            {
                dest.Delete();
            }

            DestroyedArmor = null;
        }

        if (Items != null)
        {
            foreach (var item in Items.Where(i => i is { Deleted: false }).ToList())
            {
                item.Delete();
            }

            Items = null;
        }
    }

    private void SpawnRandom()
    {
        if (Spawn == null)
        {
            return;
        }

        var rec = SpawnRecs[Utility.Random(SpawnRecs.Length)];
        ConvertOffset(ref rec);

        while (true)
        {
            var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width);
            var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height);
            var z = Map.TerMur.GetAverageZ(x, y);

            if (!Map.TerMur.CanSpawnMobile(x, y, z))
            {
                continue;
            }

            var armor = new EnsorcelledArmor(this);
            armor.MoveToWorld(new Point3D(x, y, z), Map.TerMur);
            Spawn.Add(armor);
            break;
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(Armor?.Count ?? 0);
        if (Armor != null)
        {
            foreach (var a in Armor)
            {
                writer.Write(a);
            }
        }

        writer.WriteEncodedInt(DestroyedArmor?.Count ?? 0);
        if (DestroyedArmor != null)
        {
            foreach (var a in DestroyedArmor)
            {
                writer.Write(a);
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

        writer.WriteEncodedInt(Items?.Count ?? 0);
        if (Items != null)
        {
            foreach (var i in Items)
            {
                writer.Write(i);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
        reader.ReadEncodedInt(); // version

        Armor = new List<Item>();
        DestroyedArmor = new List<Item>();
        Items = new List<Item>();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            var it = reader.ReadEntity<Item>();

            if (it == null)
            {
                continue;
            }

            if (it is CursedSuitOfArmor csoa)
            {
                csoa.Encounter = this;
            }

            Armor.Add(it);
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Item>() is { } it)
            {
                DestroyedArmor.Add(it);
            }
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            Spawn ??= new List<BaseCreature>();

            if (reader.ReadEntity<Mobile>() is not BaseCreature bc)
            {
                continue;
            }

            if (bc is EnsorcelledArmor ea)
            {
                ea.Encounter = this;
            }

            Spawn.Add(bc);
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Item>() is { } item)
            {
                Items.Add(item);
            }
        }

        if (Spawn == null || Spawn.Count < 4)
        {
            Spawn ??= new List<BaseCreature>();
            var toSpawn = 4 - Spawn.Count;

            for (var i = 0; i < toSpawn; i++)
            {
                SpawnRandom();
            }
        }
    }
}
