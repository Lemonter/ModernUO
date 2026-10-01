using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

public class FountainEncounter : ShadowguardEncounter
{
    public const int MaxElementals = 30;

    public List<BaseCreature> Elementals { get; set; }
    public List<Item> ShadowguardCanals { get; set; }

    public List<FlowChecker> FlowCheckers { get; set; }

    public DateTime NextSpawn;

    public override Type AddonType => typeof(ShadowguardFountainAddon);

    public FountainEncounter() : base(EncounterType.Fountain)
    {
    }

    public FountainEncounter(ShadowguardInstance instance) : base(EncounterType.Fountain, instance)
    {
    }

    public void UseSpigot(ShadowguardSpigot spigot, Mobile m)
    {
        if (FlowCheckers == null)
        {
            return;
        }

        foreach (var checker in FlowCheckers)
        {
            if (checker.CheckUse(spigot, m))
            {
                return;
            }
        }
    }

    public override void CheckEncounter()
    {
        if (FlowCheckers != null && FlowCheckers.Count(c => c.Complete) == 4)
        {
            CompleteEncounter();
        }
    }

    public override void OnCreatureKilled(BaseCreature bc)
    {
        if (Elementals != null && Elementals.Contains(bc))
        {
            Elementals.Remove(bc);
            Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomMinMax(1, 5)), SpawnRandomElemental);
        }
    }

    public override void OnTick()
    {
        if (NextSpawn < Core.Now && (Elementals == null || Elementals.Count < MaxElementals))
        {
            SpawnRandomElemental();
            NextSpawn = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(30, 45));
        }
    }

    public override void Setup()
    {
        ShadowguardCanals = new List<Item>();
        Elementals = new List<BaseCreature>();
        FlowCheckers = new List<FlowChecker>();

        var toSpawn = 3 + PartySize() * 2;

        Timer.DelayCall(ShadowguardController.ReadyDuration + TimeSpan.FromSeconds(30), () =>
        {
            for (var i = 0; i < toSpawn; i++)
            {
                SpawnRandomElemental();
            }
        });

        for (var i = 0; i < 4; i++)
        {
            var spigot = new ShadowguardSpigot(i < 2 ? 0x9BF2 : 0x9BE5);
            var p = SpawnPoints[i];

            ConvertOffset(ref p);

            spigot.MoveToWorld(p, Map.TerMur);
            AddShadowguardCanal(spigot);
            FlowCheckers.Add(new FlowChecker(spigot, this));
        }

        var rec1 = SpawnRecs[4];
        var rec2 = SpawnRecs[5];

        ConvertOffset(ref rec1);
        ConvertOffset(ref rec2);

        SpawnDrain(rec1);
        SpawnDrain(rec2);
    }

    public override void CompleteEncounter()
    {
        base.CompleteEncounter();
        ClearSpawn();
    }

    private void ClearSpawn()
    {
        if (Elementals == null)
        {
            return;
        }

        foreach (var elemental in Elementals.Where(t => t is { Deleted: false }).ToList())
        {
            elemental.Delete();
        }

        Elementals = null;
    }

    public override void ClearItems()
    {
        if (ShadowguardCanals != null)
        {
            foreach (var canal in ShadowguardCanals.Where(i => i is { Deleted: false }).ToList())
            {
                canal.Delete();
            }

            ShadowguardCanals = null;
        }

        ClearSpawn();

        if (FlowCheckers != null)
        {
            foreach (var f in FlowCheckers.Where(f => f != null))
            {
                f.EndEncounter();
            }
        }
    }

    private void SpawnRandomElemental()
    {
        if (Elementals == null)
        {
            return;
        }

        var rec = SpawnRecs[Utility.RandomMinMax(0, 3)];
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

            BaseCreature elemental = new VileWaterElemental();

            elemental.MoveToWorld(new Point3D(x, y, z), Map.TerMur);
            Elementals.Add(elemental);
            break;
        }
    }

    private void SpawnDrain(Rectangle2D rec)
    {
        var x = Utility.RandomMinMax(rec.X, rec.X + rec.Width);
        var y = Utility.RandomMinMax(rec.Y, rec.Y + rec.Height);
        var z = Map.TerMur.GetAverageZ(x, y);

        var drain = new ShadowguardDrain();

        drain.MoveToWorld(new Point3D(x, y, z), Map.TerMur);
        AddShadowguardCanal(drain);
    }

    public void SpawnBaddie(Mobile m)
    {
        if (Elementals == null)
        {
            return;
        }

        var p = m.Location;

        for (var i = 0; i < 10; i++)
        {
            var x = Utility.RandomMinMax(p.X - 1, p.X + 1);
            var y = Utility.RandomMinMax(p.Y - 1, p.Y + 1);

            if (Map.TerMur.CanSpawnMobile(x, y, -20))
            {
                p = new Point3D(x, y, -20);
                break;
            }
        }

        BaseCreature creature = new HurricaneElemental();

        creature.MoveToWorld(p, Map.TerMur);
        Elementals.Add(creature);

        creature.Combatant = m;
    }

    public void AddShadowguardCanal(Item canal)
    {
        if (ShadowguardCanals != null && !ShadowguardCanals.Contains(canal))
        {
            ShadowguardCanals.Add(canal);
        }
    }

    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(Elementals?.Count ?? 0);
        if (Elementals != null)
        {
            foreach (var e in Elementals)
            {
                writer.Write(e);
            }
        }

        writer.WriteEncodedInt(ShadowguardCanals?.Count ?? 0);
        if (ShadowguardCanals != null)
        {
            foreach (var c in ShadowguardCanals)
            {
                writer.Write(c);
            }
        }

        writer.WriteEncodedInt(FlowCheckers?.Count ?? 0);
        if (FlowCheckers != null)
        {
            foreach (var f in FlowCheckers)
            {
                f.Serialize(writer);
            }
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);
        reader.ReadEncodedInt(); // version

        Elementals = new List<BaseCreature>();

        var count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            if (reader.ReadEntity<Mobile>() is BaseCreature bc)
            {
                Elementals.Add(bc);
            }
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            ShadowguardCanals ??= new List<Item>();

            if (reader.ReadEntity<Item>() is { } canal)
            {
                ShadowguardCanals.Add(canal);
            }
        }

        count = reader.ReadEncodedInt();
        for (var i = 0; i < count; i++)
        {
            FlowCheckers ??= new List<FlowChecker>();
            FlowCheckers.Add(new FlowChecker(reader, this));
        }

        if (Elementals.Count < 8)
        {
            var toSpawn = 8 - Elementals.Count;

            for (var i = 0; i < toSpawn; i++)
            {
                SpawnRandomElemental();
            }
        }
    }

    public class FlowChecker
    {
        private static readonly int[] _offsets = { 0, -1, 1, 0, 0, 1, -1, 0 };

        private FountainEncounter _encounter;
        private List<ShadowguardCanal> _checked;

        private readonly ShadowguardSpigot _spigot;
        private ShadowguardDrain _drain;

        public bool Complete => _spigot != null && _drain != null;

        public FlowChecker(ShadowguardSpigot start, FountainEncounter encounter)
        {
            _spigot = start;
            _encounter = encounter;
        }

        public FlowChecker(IGenericReader reader, FountainEncounter encounter)
        {
            reader.ReadEncodedInt(); // version
            _encounter = encounter;

            _spigot = reader.ReadEntity<Item>() as ShadowguardSpigot;
            _drain = reader.ReadEntity<Item>() as ShadowguardDrain;

            var count = reader.ReadEncodedInt();
            for (var i = 0; i < count; i++)
            {
                _checked ??= new List<ShadowguardCanal>();

                if (reader.ReadEntity<Item>() is ShadowguardCanal c)
                {
                    _checked.Add(c);
                }
            }
        }

        public void EndEncounter()
        {
            _spigot?.Delete();
            _checked = null;
            _encounter = null;
        }

        public bool CheckUse(ShadowguardSpigot spigot, Mobile m)
        {
            if (spigot != _spigot)
            {
                return false;
            }

            Check(m);
            return true;
        }

        public void Check(Mobile m)
        {
            var southFacing = _spigot.ItemID == 39922;

            _checked?.Clear();

            var p = southFacing
                ? new Point3D(_spigot.X, _spigot.Y + 1, -20)
                : new Point3D(_spigot.X + 1, _spigot.Y, -20);

            var item = FindItem(p);

            if (item is ShadowguardCanal canal &&
                (southFacing && canal.Flow is Flow.NorthSouth or Flow.SouthEastCorner or Flow.SouthWestCorner ||
                 !southFacing && canal.Flow is Flow.EastWest or Flow.SouthEastCorner or Flow.NorthEastCorner))
            {
                _checked ??= new List<ShadowguardCanal>();
                _checked.Add(canal);

                RecursiveCheck(item, null);
            }

            if (Complete)
            {
                Fill();
                Timer.DelayCall(TimeSpan.FromSeconds(2), () => _encounter.CheckEncounter());
            }
            else
            {
                _encounter.SpawnBaddie(m);
            }
        }

        public void RecursiveCheck(Item item, Item last)
        {
            for (var i = 0; i < _offsets.Length; i += 2)
            {
                var p = new Point3D(item.X + _offsets[i], item.Y + _offsets[i + 1], item.Z);
                var next = FindItem(p);

                if (next == null || next == last || !Connects(item, next))
                {
                    continue;
                }

                if (next is ShadowguardDrain drain)
                {
                    _drain = drain;
                    return;
                }

                if (next is ShadowguardCanal nextCanal)
                {
                    _checked ??= new List<ShadowguardCanal>();
                    _checked.Add(nextCanal);

                    RecursiveCheck(next, item);
                }
            }
        }

        public static bool Connects(Item one, Item two)
        {
            if (one is ShadowguardCanal oneCanal && two is ShadowguardDrain)
            {
                var flow = oneCanal.Flow;
                var d = Utility.GetDirection(one.Location, two.Location);

                return d switch
                {
                    Direction.North => flow is Flow.NorthSouth or Flow.SouthEastCorner or Flow.SouthWestCorner,
                    Direction.East  => flow is Flow.EastWest or Flow.NorthWestCorner or Flow.SouthWestCorner,
                    Direction.South => flow is Flow.NorthSouth or Flow.NorthEastCorner or Flow.NorthWestCorner,
                    Direction.West  => flow is Flow.EastWest or Flow.NorthEastCorner or Flow.SouthEastCorner,
                    _               => false
                };
            }

            if (two is ShadowguardCanal twoCanal)
            {
                return ((ShadowguardCanal)one).Connects(twoCanal);
            }

            return false;
        }

        public static Item FindItem(Point3D p)
        {
            foreach (var i in Map.TerMur.GetItemsInRange<Item>(p, 0))
            {
                if (i.Z == p.Z && i is ShadowguardCanal or ShadowguardDrain)
                {
                    return i;
                }
            }

            return null;
        }

        private void Fill()
        {
            var time = 200;

            if (_spigot.ItemID == 39922)
            {
                _spigot.ItemID = 17294;
            }
            else if (_spigot.ItemID == 39909)
            {
                _spigot.ItemID = 17278;
            }

            foreach (var i in _checked)
            {
                i.Movable = false;
            }

            for (var index = 0; index < _checked.Count; index++)
            {
                var item = _checked[index];
                Timer.DelayCall(TimeSpan.FromMilliseconds(time), () => Fill(item));
                time += 200;
            }
        }

        private void Fill(ShadowguardCanal canal)
        {
            canal.Fill();

            if (_checked.IndexOf(canal) == _checked.Count - 1)
            {
                _drain.Hue = 0;
            }
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.Write(_spigot);
            writer.Write(_drain);

            writer.WriteEncodedInt(_checked?.Count ?? 0);
            if (_checked != null)
            {
                foreach (var c in _checked)
                {
                    writer.Write(c);
                }
            }
        }
    }
}
