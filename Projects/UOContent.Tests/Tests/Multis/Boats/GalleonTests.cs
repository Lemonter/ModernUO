using System;
using System.Collections.Generic;
using Server;
using Server.Engines.HighSeas;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Xunit;

namespace UOContent.Tests.Multis.Boats;

// Verification for the High Seas galleon port: stage 1 (hull, crew, access, paint) and stage 2
// (hull damage, repairs, scuttling).
[Collection("Sequential UOContent Tests")]
public class GalleonTests : IDisposable
{
    private readonly List<Mobile> _mobiles = [];
    private readonly List<Item> _items = [];

    public void Dispose()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].Delete();
        }

        for (var i = 0; i < _mobiles.Count; i++)
        {
            _mobiles[i].Delete();
        }
    }

    private PlayerMobile NewPlayer()
    {
        var pm = new PlayerMobile(World.NewMobile);
        pm.DefaultMobileInit();
        World.AddEntity(pm);
        _mobiles.Add(pm);
        return pm;
    }

    private T Add<T>(T item) where T : Item
    {
        _items.Add(item);
        return item;
    }

    [Fact]
    public void FacingSetsMultiId()
    {
        Assert.Equal(0x40, Add(new BritannianShip()).ItemID);
        Assert.Equal(0x41, Add(new BritannianShip(Direction.East)).ItemID);
        Assert.Equal(0x42, Add(new BritannianShip(Direction.South)).ItemID);
        Assert.Equal(0x43, Add(new BritannianShip(Direction.West)).ItemID);
        Assert.Equal(0x27, Add(new GargishGalleon(Direction.West)).ItemID);
        Assert.Equal(0x1A, Add(new OrcishGalleon(Direction.South)).ItemID);
        Assert.Equal(0x31, Add(new TokunoGalleon(Direction.East)).ItemID);
    }

    [Fact]
    public void GalleonCreatesPilot()
    {
        var galleon = Add(new OrcishGalleon());

        Assert.NotNull(galleon.GalleonPilot);
        Assert.Same(galleon, galleon.GalleonPilot.Galleon);
        Assert.False(galleon.GalleonPilot.Deleted);
        Assert.Equal(galleon.GetPilotLocation(), galleon.GalleonPilot.Location);
    }

    [Fact]
    public void AccessListRanks()
    {
        var galleon = Add(new GargishGalleon());
        var owner = NewPlayer();
        var crew = NewPlayer();

        galleon.Owner = owner;

        var list = galleon.AccessList;

        Assert.NotNull(list);
        Assert.Equal(SecurityLevel.Captain, galleon.GetSecurityLevel(owner));
        Assert.Equal(SecurityLevel.Denied, galleon.GetSecurityLevel(crew));
        Assert.False(galleon.HasCommandRights(crew));
        Assert.False(galleon.HasAccess(crew));

        list.AddToManifest(crew, SecurityLevel.Crewman);

        Assert.Equal(SecurityLevel.Crewman, galleon.GetSecurityLevel(crew));
        Assert.True(galleon.HasCommandRights(crew));
        Assert.True(galleon.HasAccess(crew));

        list.AddToManifest(crew, SecurityLevel.Denied);
        Assert.Equal(SecurityLevel.Denied, galleon.GetSecurityLevel(crew));

        list.RemoveFromAccessList(crew);
        Assert.Equal(SecurityLevel.Denied, galleon.GetSecurityLevel(crew));

        list.DefaultPublicAccess = SecurityLevel.Passenger;
        Assert.True(list.IsPublic);
        Assert.Equal(SecurityLevel.Passenger, galleon.GetSecurityLevel(crew));
        Assert.False(galleon.HasCommandRights(crew));
    }

    [Fact]
    public void UnownedGalleonIsOpen()
    {
        var galleon = Add(new TokunoGalleon());
        var stranger = NewPlayer();

        Assert.Equal(SecurityLevel.Captain, galleon.GetSecurityLevel(stranger));
        Assert.True(galleon.HasCommandRights(stranger));
    }

    [Fact]
    public void PaintAndRemove()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        Assert.False(galleon.HasPaint);

        Assert.True(galleon.TryPaintBoat(owner, 1954));
        Assert.True(galleon.HasPaint);
        Assert.Equal(1953, galleon.Hue);
        Assert.Equal(1, galleon.PaintCoats);

        Assert.True(galleon.TryPaintBoat(owner, 1954));
        Assert.Equal(1952, galleon.Hue);

        Assert.True(galleon.RemovePaint());
        Assert.False(galleon.HasPaint);
        Assert.Equal(0, galleon.Hue);
        Assert.False(galleon.RemovePaint());

        Assert.True(galleon.TryPermanentPaintBoat(owner, 1965));
        Assert.Equal(1965, galleon.Hue);
        Assert.Equal(1965, galleon.BaseBoatHue);
        Assert.True(galleon.TryPaintBoat(owner, 1954));
        Assert.False(galleon.TryPermanentPaintBoat(owner, 1966));
    }

    [Fact]
    public void PaintDecays()
    {
        var galleon = Add(new OrcishGalleon());
        var owner = NewPlayer();

        galleon.Owner = owner;

        Assert.True(galleon.TryPaintBoat(owner, 1954));
        Assert.Equal(1, galleon.PaintCoats);

        galleon.NextPaintDecay = Core.Now - System.TimeSpan.FromSeconds(1);
        galleon.CheckPaintDecay();

        Assert.False(galleon.HasPaint);
        Assert.Equal(0, galleon.Hue);
    }

    [Fact]
    public void SerializesWithoutFailure_AndByteStable()
    {
        var galleon = Add(new BritannianShip(Direction.East));
        var owner = NewPlayer();

        galleon.Owner = owner;
        Assert.True(galleon.TryPaintBoat(owner, 1954));

        Assert.Equal(Snapshot(galleon), Snapshot(galleon));
        Assert.NotNull(typeof(BritannianShip).GetConstructor([typeof(Serial)]));
        Assert.NotNull(typeof(DockedBritannianShip).GetConstructor([typeof(Serial)]));
    }

    [Fact]
    public void RoundTripsFacingsAndPaintState()
    {
        var galleon = Add(new BritannianShip(Direction.West));
        var owner = NewPlayer();

        galleon.Owner = owner;
        galleon.ShipName = "Ласточка";
        Assert.True(galleon.TryPaintBoat(owner, 1954));

        RegisterWorld(galleon);

        var buffer = Snapshot(galleon);

        var copy = new BritannianShip(World.NewMobile);
        _items.Add(copy);

        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(Direction.West, copy.Facing);
        Assert.Equal(0x43, copy.ItemID);
        Assert.Equal("Ласточка", copy.ShipName);
        Assert.Equal(galleon.Hue, copy.Hue);
        Assert.Equal(1, copy.PaintCoats);
        Assert.Equal(1954, copy.BasePaintHue);
        Assert.Equal(owner, copy.Owner);
    }

    [Fact]
    public void ClassicLargeBoatRoundTripsFacing()
    {
        var boat = Add(new LargeBoat { Facing = Direction.East });

        var buffer = Snapshot(boat);

        var copy = new LargeBoat(World.NewMobile);
        _items.Add(copy);

        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(Direction.East, copy.Facing);
    }

    private void RegisterWorld(BaseBoat boat)
    {
        World.AddEntity(boat);

        if (boat.TillerMan != null)
        {
            World.AddEntity(boat.TillerMan);
        }

        if (boat.Hold != null)
        {
            World.AddEntity(boat.Hold);
        }

        if (boat.PPlank != null)
        {
            World.AddEntity(boat.PPlank);
        }

        if (boat.SPlank != null)
        {
            World.AddEntity(boat.SPlank);
        }

        if (boat is BaseGalleon { GalleonPilot: { } pilot })
        {
            World.AddEntity(pilot);
        }
    }

    private static byte[] Snapshot(Item item)
    {
        var writer = new BufferWriter(true);
        item.Serialize(writer);

        var buffer = new byte[writer.Position];
        writer.Buffer.AsSpan(0, (int)writer.Position).CopyTo(buffer);
        return buffer;
    }

    [Fact]
    public void AccessListIsKeyedBySerialAndRemovedOnDelete()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var list = galleon.AccessList;
        var other = Add(new BritannianShip());

        Assert.NotSame(list, other.AccessList);
        Assert.Same(list, galleon.AccessList);

        list.AddToManifest(owner, SecurityLevel.Crewman);

        galleon.Delete();
        _items.Remove(galleon);

        Assert.Null(HighSeasShips.GetAccessList(galleon));
    }

    private static Backpack NewPack(Mobile owner)
    {
        var pack = new Backpack();
        owner.AddItem(pack);
        return pack;
    }

    [Fact]
    public void DamageLowersHitsAndSwitchesHullArt()
    {
        var galleon = Add(new BritannianShip());
        var from = NewPlayer();

        Assert.Equal(galleon.MaxHits, galleon.Hits);
        Assert.Equal(DamageLevel.Pristine, galleon.DamageTaken);
        Assert.Equal(0x40, galleon.ItemID);

        // 55% повреждений → прочность 45% → Heavily → DamageValue 1 → корпус +4.
        galleon.OnTakenDamage(from, galleon.MaxHits * 55 / 100);

        Assert.Equal(galleon.MaxHits * 45 / 100, galleon.Hits);
        Assert.Equal(DamageLevel.Heavily, galleon.DamageTaken);
        Assert.Equal(1, galleon.DamageValue);
        Assert.Equal(0x44, galleon.ItemID);
        Assert.False(galleon.Scuttled);

        // У британской галеры всего две ступени арта, поэтому Severely остаётся 0x44.
        galleon.OnTakenDamage(from, galleon.MaxHits * 30 / 100);

        Assert.Equal(DamageLevel.Severely, galleon.DamageTaken);
        Assert.Equal(0x44, galleon.ItemID);
        Assert.True(galleon.Scuttled);
    }

    [Fact]
    public void DamagedHullUsesHullSpecificArt()
    {
        var galleon = Add(new OrcishGalleon(Direction.East));

        Assert.Equal(0x19, galleon.ItemID);

        // У орочьей галеры три ступени: Severely — это +8 к ID.
        galleon.OnTakenDamage(null, galleon.MaxHits * 80 / 100);

        Assert.Equal(DamageLevel.Severely, galleon.DamageTaken);
        Assert.Equal(2, galleon.DamageValue);
        Assert.Equal(0x21, galleon.ItemID);
    }

    [Fact]
    public void RepairsRestoreHitsAndPristineArt()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var pack = NewPack(owner);
        pack.AddItem(new Board(2000));
        pack.AddItem(new Cloth(2000));

        galleon.OnTakenDamage(owner, galleon.MaxHits * 55 / 100);

        Assert.Equal(0x44, galleon.ItemID);

        galleon.TryRepairs(owner);

        Assert.Equal(galleon.MaxHits, galleon.Hits);
        Assert.Equal(DamageLevel.Pristine, galleon.DamageTaken);
        Assert.Equal(0, galleon.DamageValue);
        Assert.Equal(0x40, galleon.ItemID);
        Assert.False(galleon.Scuttled);
    }

    [Fact]
    public void EmergencyRepairsKeepScuttledHullAfloat()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var pack = NewPack(owner);
        pack.AddItem(new Board(BaseGalleon.EmergencyRepairWoodCost));
        pack.AddItem(new Cloth(BaseGalleon.EmergencyRepairClothCost));

        galleon.OnTakenDamage(owner, galleon.MaxHits * 80 / 100);

        Assert.True(galleon.Scuttled);
        Assert.False(galleon.IsUnderEmergencyRepairs());

        Assert.True(galleon.TryEmergencyRepair(owner));

        Assert.True(galleon.IsUnderEmergencyRepairs());
        Assert.True(galleon.GetEndEmergencyRepairs() > TimeSpan.Zero);
        Assert.False(galleon.Scuttled);
        Assert.False(galleon.Deleted);
    }

    [Fact]
    public void DamagedHullSailsSlower()
    {
        var galleon = Add(new BritannianShip());

        var normal = galleon.GetMovementInterval(true, false, out var normalClientSpeed);

        Assert.Equal(0x4, normalClientSpeed);

        Assert.True(galleon.TrySail(Direction.North, true));
        Assert.True(galleon.IsMoving);

        galleon.OnTakenDamage(null, galleon.MaxHits * 60 / 100);

        Assert.Equal(DamageLevel.Heavily, galleon.DamageTaken);

        var damaged = galleon.GetMovementInterval(true, false, out var damagedClientSpeed);

        Assert.True(damaged > normal);
        Assert.Equal(0x3, damagedClientSpeed);

        // Пробитый корпус уже не слушается команд.
        galleon.OnTakenDamage(null, galleon.MaxHits * 20 / 100);

        Assert.True(galleon.Scuttled);
        Assert.False(galleon.TrySail(Direction.North, true));
    }

    [Fact]
    public void ScuttlingSinksShipAndClearsRegistry()
    {
        var galleon = Add(new BritannianShip());
        var serial = galleon.Serial;

        Assert.NotNull(galleon.AccessList);
        Assert.True(HighSeasShips.HasAccessList(serial));

        galleon.OnTakenDamage(null, galleon.MaxHits * 80 / 100);

        Assert.True(galleon.Scuttled);
        Assert.True(HighSeasShips.HasAccessList(serial));

        galleon.Sink();

        Assert.True(galleon.Deleted);
        Assert.False(HighSeasShips.HasAccessList(serial));
        Assert.Null(galleon.GalleonPilot);
    }

    [Fact]
    public void RoundTripsHullDamage()
    {
        var galleon = Add(new BritannianShip());

        galleon.OnTakenDamage(null, galleon.MaxHits * 55 / 100);

        RegisterWorld(galleon);

        var buffer = Snapshot(galleon);

        var copy = new BritannianShip(World.NewMobile);
        _items.Add(copy);

        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(galleon.Hits, copy.Hits);
        Assert.Equal(DamageLevel.Heavily, copy.DamageTaken);
        Assert.Equal(0x44, copy.ItemID);
    }
}
