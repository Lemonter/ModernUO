using System;
using System.Collections.Generic;
using Server;
using Server.Engines.HighSeas;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Xunit;

namespace UOContent.Tests.Multis.Boats;

// Verification for the High Seas galleon cannon port (stage 3): the weapon pads and the Cannons list,
// the reload cycle of both cannon families, ammunition, and the shot itself.
//
// The host has no client data (no maps, no multi.mul), so every test stays on Map.Internal: cannons are
// placed with force: true, and the shot is aimed at a ship passed straight to the firing API — that is
// exactly the Target path a player takes, minus the spatial lookup of pad and target.
[Collection("Sequential UOContent Tests")]
public class CannonTests : IDisposable
{
    private readonly List<Mobile> _mobiles = [];
    private readonly List<Item> _items = [];

    public CannonTests()
    {
        Core._tickCount = 0;
        Timer.Init(0);
    }

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

    private static Backpack NewPack(Mobile owner)
    {
        var pack = new Backpack();
        owner.AddItem(pack);
        return pack;
    }

    // 64 ms lockstep keeps the wheel and Core.TickCount in sync.
    private static void Advance(int ms)
    {
        for (var elapsed = 0; elapsed < ms; elapsed += 64)
        {
            Core._tickCount += 64;
            Timer.Slice(Core.TickCount);
        }
    }

    private static void LoadMagazine(BaseShipCannon cannon, AmmunitionType ammoType)
    {
        cannon.DropItem(new PowderCharge());
        cannon.DropItem(AmmoInfo.GetAmmoInfo(ammoType).CreateAmmo());
        cannon.DropItem(new FuseCord());
    }

    [Fact]
    public void CannonPlacedOnDeckIsTracked()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new Culverin(galleon);

        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));
        Assert.Contains(cannon, galleon.Cannons);
        Assert.Same(galleon, cannon.Galleon);
        Assert.Equal(1, galleon.CannonCount);
        Assert.Equal(galleon.GetPadLocation(cannon.PadOffset), cannon.Location);
        Assert.Equal(AmmunitionType.Empty, cannon.AmmoType);
        Assert.True(cannon.Empty);
        Assert.False(cannon.CanLight);
        Assert.InRange((int)cannon.Position, 0, 7);

        // Арт ствола — из таблицы пушек (лёгкая, север).
        Assert.Contains(cannon.ItemID, BaseGalleon.CannonIDs[BaseGalleon.GetValueForDirection(cannon.Facing)]);

        // Столько же пушек, сколько мест, — это предел палубы.
        for (var i = 1; i < galleon.MaxCannons; i++)
        {
            var spot = new Point3D(galleon.X + i, galleon.Y, galleon.Z);
            Assert.True(galleon.TryAddCannon(owner, spot, new Culverin(galleon), null, force: true));
        }

        Assert.Equal(galleon.MaxCannons, galleon.CannonCount);

        var extra = new Culverin(galleon);
        var extraSpot = new Point3D(galleon.X + galleon.MaxCannons, galleon.Y, galleon.Z);

        Assert.False(galleon.TryAddCannon(owner, extraSpot, extra, null, force: true));
        Assert.True(extra.Deleted);
        Assert.Equal(galleon.MaxCannons, galleon.CannonCount);
    }

    [Fact]
    public void DeedDeploysCannonAndIsDeleted()
    {
        var galleon = Add(new OrcishGalleon());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var deed = Add(new CarronadeDeed());

        Assert.True(galleon.TryAddCannon(owner, galleon.Location, deed, force: true));
        Assert.Equal(1, galleon.CannonCount);
        Assert.IsType<Carronade>(galleon.Cannons[0]);
        Assert.True(deed.Deleted);
        Assert.Same(owner, galleon.Owner);

        // 1.3 у орочьего корпуса — из ServUO.
        Assert.Equal(1.3, galleon.CannonDamageMod);
    }

    [Fact]
    public void MagazineReloadCycleConsumesPowderAmmoAndFuse()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new Culverin(galleon);
        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));

        var pack = NewPack(owner);
        pack.AddItem(new Ramrod());

        LoadMagazine(cannon, AmmunitionType.Cannonball);

        Assert.Equal(3, cannon.Items.Count);
        Assert.False(cannon.CanLight);

        cannon.TryPrep(owner);

        // Подготовка (4 с) → порох (1.5 с) → снаряд (1.5 с) → фитиль (1.5 с).
        Advance(12000);

        Assert.True(cannon.CanLight);
        Assert.Equal(AmmunitionType.Cannonball, cannon.AmmoType);
        Assert.Empty(cannon.Items);
        Assert.Equal(CannonAction.Finish, cannon.Prepered);
        Assert.Equal(CannonAction.Finish, cannon.Charged);
        Assert.Equal(CannonAction.Finish, cannon.Loaded);
        Assert.Equal(CannonAction.Finish, cannon.Primed);
    }

    [Fact]
    public void FiringLoadedCannonDamagesTargetShipHull()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new Culverin(galleon);
        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));

        var pack = NewPack(owner);
        pack.AddItem(new Ramrod());

        var match = new Matches();
        pack.AddItem(match);
        match.Light(owner);

        LoadMagazine(cannon, AmmunitionType.Cannonball);
        cannon.TryPrep(owner);
        Advance(12000);

        Assert.True(cannon.CanLight);

        // Чужой корабль прямо по стволу (пушка стоит в центре палубы и смотрит на север).
        var target = Add(new BritannianShip());
        target.Location = new Point3D(galleon.X, galleon.Y - 5, galleon.Z);

        var hitsBefore = target.Hits;

        Assert.True(cannon.TryFireAt(owner, target));

        // Фитиль горит 1.5 с.
        Advance(3000);

        Assert.True(target.Hits < hitsBefore, "выстрел должен пробить чужой корпус через OnTakenDamage");
        Assert.Equal(hitsBefore - 5000, target.Hits); // урон ядра из ServUO

        // После выстрела ствол пуст.
        Assert.False(cannon.CanLight);
        Assert.Equal(AmmunitionType.Empty, cannon.AmmoType);
        Assert.Empty(cannon.Items);
    }

    [Fact]
    public void GrapeshotHitsHullWeakerThanCannonball()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new Culverin(galleon);
        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));

        var pack = NewPack(owner);
        pack.AddItem(new Ramrod());

        var match = new Matches();
        pack.AddItem(match);
        match.Light(owner);

        LoadMagazine(cannon, AmmunitionType.Grapeshot);
        cannon.TryPrep(owner);
        Advance(12000);

        Assert.Equal(AmmunitionType.Grapeshot, cannon.AmmoType);
        Assert.True(cannon.CanLight);

        var target = Add(new BritannianShip());
        target.Location = new Point3D(galleon.X, galleon.Y - 5, galleon.Z);

        var hitsBefore = target.Hits;

        Assert.True(cannon.TryFireAt(owner, target));
        Advance(3000);

        // Картечь бьёт по корпусу пятнадцатью дробями по 100–150.
        var damage = hitsBefore - target.Hits;

        Assert.InRange(damage, 15 * 100, 15 * 150);
    }

    [Fact]
    public void AmmoCatalogueMatchesServUo()
    {
        var cannonball = AmmoInfo.GetAmmoInfo(AmmunitionType.Cannonball);
        var grapeshot = AmmoInfo.GetAmmoInfo(AmmunitionType.Grapeshot);
        var frost = AmmoInfo.GetAmmoInfo(AmmunitionType.FrostCannonball);
        var flame = AmmoInfo.GetAmmoInfo(AmmunitionType.FlameCannonball);

        Assert.NotNull(cannonball);
        Assert.Equal(5000, cannonball.MinDamage);
        Assert.Equal(5000, cannonball.MaxDamage);
        Assert.Equal(100, cannonball.PhysicalDamage);

        Assert.Equal(100, grapeshot.MinDamage);
        Assert.Equal(150, grapeshot.MaxDamage);

        Assert.Equal(30, frost.MinDamage);
        Assert.Equal(50, frost.ColdDamage);

        Assert.Equal(50, flame.FireDamage);
        Assert.Equal(5000, flame.MaxDamage);

        Assert.Null(AmmoInfo.GetAmmoInfo(AmmunitionType.Empty));
        Assert.Equal("ядро", AmmoInfo.GetAmmoName(AmmunitionType.Cannonball));
        Assert.Equal("нет", AmmoInfo.GetAmmoName(AmmunitionType.Empty));

        // Снаряд каждого типа создаётся и несёт свой тип заряда.
        foreach (var ammoType in new[]
                 {
                     AmmunitionType.Cannonball, AmmunitionType.Grapeshot,
                     AmmunitionType.FrostCannonball, AmmunitionType.FlameCannonball
                 })
        {
            var item = AmmoInfo.GetAmmoInfo(ammoType).CreateAmmo();

            Assert.IsAssignableFrom<ICannonAmmo>(item);
            Assert.Equal(ammoType, ((ICannonAmmo)item).AmmoType);

            item.Delete();
        }
    }

    [Fact]
    public void LegacyCannonReloadCycleUsesBackpackSupplies()
    {
        var galleon = Add(new TokunoGalleon());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new LightShipCannon(galleon);

        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));

        var pack = NewPack(owner);

        pack.AddItem(new PowderCharge());
        pack.AddItem(new Ramrod());
        pack.AddItem(new Grapeshot());
        pack.AddItem(new FuseCord());

        var match = new Matches();
        pack.AddItem(match);
        match.Light(owner);

        Assert.True(cannon.TryCharge(owner));
        Advance(3000);

        Assert.True(cannon.Charged);
        Assert.Equal(0, pack.GetAmount(typeof(PowderCharge)));

        Assert.True(cannon.TryLoad(owner));

        var ammo = pack.FindItemByType<Grapeshot>();
        Assert.NotNull(ammo);

        cannon.DoLoad(owner, ammo);
        Advance(3000);

        Assert.Equal(AmmunitionType.Grapeshot, cannon.AmmoType);
        Assert.Null(pack.FindItemByType<Grapeshot>());

        Assert.True(cannon.TryPrime(owner));
        Advance(3000);

        Assert.True(cannon.Primed);
        Assert.True(cannon.CanLight);
        Assert.Null(pack.FindItemByType<FuseCord>());

        Assert.True(cannon.TryLightFuse(owner));
        Advance(3000);

        // Выстрел по пустой линии: ствол разряжен и стал грязнее на один выстрел.
        Assert.False(cannon.CanLight);
        Assert.Equal(AmmunitionType.Empty, cannon.AmmoType);
        Assert.True(cannon.Cleaned);
        Assert.Equal(1, cannon.Cleanliness);
    }

    [Fact]
    public void CannonDeletedWithShip()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new Culverin(galleon);
        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));

        var deeds = galleon.TakeCannonDeeds();
        Assert.Single(deeds);
        Assert.IsType<CulverinDeed>(deeds[0]);
        Add(deeds[0]);

        galleon.Delete();
        _items.Remove(galleon);

        Assert.True(cannon.Deleted);
        Assert.Empty(galleon.Cannons);
    }

    [Fact]
    public void CannonRoundTripsReloadState()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new Culverin(galleon);
        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));

        var buffer = Snapshot(cannon);

        var copy = new Culverin(null);
        _items.Add(copy);

        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Equal(cannon.PadOffset, copy.PadOffset);
        Assert.Equal(cannon.Position, copy.Position);
        Assert.Equal(AmmunitionType.Empty, copy.AmmoType);
        Assert.Equal(cannon.Hits, copy.Hits);
    }

    [Fact]
    public void ShipKeepsItsCannonsAcrossSaveAndLoad()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var cannon = new Culverin(galleon);
        Assert.True(galleon.TryAddCannon(owner, galleon.Location, cannon, null, force: true));

        RegisterWorld(galleon);
        World.AddEntity(cannon);

        foreach (var pad in galleon.WeaponPads)
        {
            World.AddEntity(pad);
        }

        var buffer = Snapshot(galleon);

        var copy = new BritannianShip(World.NewMobile);
        _items.Add(copy);

        var reader = new BufferReader(buffer);
        copy.Deserialize(reader);

        Assert.Equal(buffer.Length, reader.Position);
        Assert.Single(copy.Cannons);
        Assert.Same(cannon, copy.Cannons[0]);
        Assert.Equal(galleon.WeaponPads.Count, copy.WeaponPads.Count);
    }

    private static void RegisterWorld(BaseBoat boat)
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
}
