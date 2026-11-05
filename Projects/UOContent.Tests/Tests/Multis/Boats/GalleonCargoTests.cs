using System;
using System.Collections.Generic;
using Server;
using Server.Engines.HighSeas;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Xunit;

namespace UOContent.Tests.Multis.Boats;

// Verification for the High Seas galleon cargo port (stage 4): the real cargo hold (table markers,
// deck drops), the dry-dock offload that keeps the cargo alive, the shipping-crate round trip and the
// plunder beacon that hands out maritime cargo.
//
// The host has no client data (no multi.mul), so the multi-component tables resolve to nothing and the
// hold is created at its fallback position in the middle of the deck — exactly the branch a live shard
// takes when a client install is missing a hull. Everything else is the same code a player runs.
[Collection("Sequential UOContent Tests")]
public class GalleonCargoTests : IDisposable
{
    private readonly List<Mobile> _mobiles = [];
    private readonly List<Item> _items = [];

    public GalleonCargoTests()
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

    [Fact]
    public void GalleonCreatesCargoHold()
    {
        var galleon = Add(new BritannianShip());

        Assert.NotNull(galleon.GalleonHold);
        Assert.False(galleon.GalleonHold.Deleted);
        Assert.Same(galleon, galleon.GalleonHold.Galleon);
        Assert.Contains(galleon.GalleonHold, galleon.Fixtures);
        Assert.False(galleon.HasCargo);
    }

    [Fact]
    public void HoldCapacityFollowsHull()
    {
        Assert.Equal(28000, Add(new BritannianShip()).GalleonHold.DefaultMaxWeight);
        Assert.Equal(12000, Add(new GargishGalleon()).GalleonHold.DefaultMaxWeight);
        Assert.Equal(14000, Add(new OrcishGalleon()).GalleonHold.DefaultMaxWeight);
        Assert.Equal(16000, Add(new TokunoGalleon()).GalleonHold.DefaultMaxWeight);
    }

    [Fact]
    public void ItemDroppedOnDeckLandsInHold()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var board = Add(new Board(5));

        Assert.True(galleon.OnDragDrop(owner, board));
        Assert.Contains(board, galleon.GalleonHold.Items);
        Assert.True(galleon.HasCargo);

        // Посторонний без места в списке доступа груз на палубу не кладёт.
        var stranger = NewPlayer();
        var cloth = Add(new Cloth(3));

        Assert.False(galleon.OnDragDrop(stranger, cloth));
        Assert.DoesNotContain(cloth, galleon.GalleonHold.Items);
    }

    [Fact]
    public void HoldContentsSurviveDryDock()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        var board = Add(new Board(20));
        var cloth = Add(new Cloth(10));

        Assert.True(galleon.TryStoreCargo(owner, board));
        Assert.True(galleon.TryStoreCargo(owner, cloth));
        Assert.Equal(2, galleon.GalleonHold.Items.Count);

        // В этом движке EndDryDock удаляет корабль вместе с трюмом, поэтому груз выгружается владельцу.
        Assert.Equal(2, galleon.UnloadCargoForDryDock(owner));

        Assert.Empty(galleon.GalleonHold.Items);
        Assert.Contains(board, owner.BankBox.Items);
        Assert.Contains(cloth, owner.BankBox.Items);
    }

    [Fact]
    public void ShippingCrateRoundTrip()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();

        galleon.Owner = owner;

        NewPack(owner);

        var board = Add(new Board(7));
        Assert.True(galleon.TryStoreCargo(owner, board));

        var crate = galleon.RetrieveCargoCrate(owner, chargeFee: false);

        Assert.NotNull(crate);
        _items.Add(crate);
        Assert.Contains(board, crate.Items);
        Assert.Empty(galleon.GalleonHold.Items);

        Assert.True(crate.TryLoadIntoBoat(owner));

        Assert.Contains(board, galleon.GalleonHold.Items);
        Assert.True(crate.Deleted); // опустевший ящик исчезает сам (в ServUO — OnItemRemoved)
    }

    [Fact]
    public void HoldRejectsOutsiderCargo()
    {
        var galleon = Add(new BritannianShip());
        var owner = NewPlayer();
        var stranger = NewPlayer();

        galleon.Owner = owner;

        var board = Add(new Board(1));

        Assert.False(galleon.TryStoreCargo(stranger, board));
        Assert.Empty(galleon.GalleonHold.Items);
    }

    [Fact]
    public void ClassicHoldIsHiddenAndItsCargoMigrates()
    {
        var galleon = Add(new BritannianShip());

        // Штатный трюм BaseBoat у галеры больше не открывается: настоящий трюм — GalleonHold.
        Assert.NotNull(galleon.Hold);
        Assert.False(galleon.Hold.Visible);

        // Груз, оставшийся в старом трюме (сейв этапа 1–3), переезжает в настоящий.
        var legacyBoard = Add(new Board(2));
        galleon.Hold.DropItem(legacyBoard);

        galleon.EnsureHold();

        Assert.Contains(legacyBoard, galleon.GalleonHold.Items);
        Assert.Empty(galleon.Hold.Items);
    }

    [Fact]
    public void BeaconTakesDamageAndAwardsCargo()
    {
        var beacon = Add(new PlunderBeacon());
        var winner = NewPlayer();

        var pack = NewPack(winner);

        Assert.Equal(PlunderBeacon.DefaultHitsMax, beacon.Hits);
        Assert.True(beacon.CanDamage);

        // Половина прочности — первый уровень повреждений арта (в ServUO — IDHalfHits).
        beacon.ApplyDamage(winner, PlunderBeacon.DefaultHitsMax / 2);

        Assert.Equal(PlunderBeacon.HalfHitsId, beacon.ItemID);
        Assert.False(beacon.Deleted);

        beacon.ApplyDamage(winner, PlunderBeacon.DefaultHitsMax);

        Assert.True(beacon.Deleted);
        Assert.Equal(0, beacon.Hits);
        Assert.False(beacon.CanDamage);

        var cargo = pack.FindItemByType<MaritimeCargo>();

        Assert.NotNull(cargo);
        Assert.Equal(CargoQuality.Mythical, cargo.CargoQuality);
    }

    [Fact]
    public void BeaconIgnoresDamageAfterDestruction()
    {
        var beacon = Add(new PlunderBeacon());
        var winner = NewPlayer();

        NewPack(winner);

        beacon.ApplyDamage(winner, PlunderBeacon.DefaultHitsMax);

        Assert.True(beacon.Deleted);
        Assert.Equal(0, beacon.Hits);

        // Повторный урон по разрушенному маяку не падает и не выдаёт второй груз.
        beacon.ApplyDamage(winner, 100);
        Assert.Equal(0, beacon.Hits);
    }

    [Fact]
    public void BeaconPlatformCarriesBeaconAndCannons()
    {
        var addon = Add(new PlunderBeaconAddon());

        Assert.NotNull(addon.Beacon);
        Assert.Same(addon, addon.Beacon.Controller);
        Assert.Equal(4, addon.Cannons.Count);
        Assert.True(addon.BeaconVulnerable);

        foreach (var cannon in addon.Cannons)
        {
            Assert.IsType<MannedBlundercannon>(cannon);
            Assert.True(((MannedCannon)cannon).CanFireUnmanned);
        }

        // Разрушение маяка топит плот, но сам маяк к этому моменту уже удалён.
        addon.OnBeaconDestroyed();
        Assert.False(addon.Deleted);
    }

    [Fact]
    public void MannedCannonIsTakenByPlayer()
    {
        var cannon = Add(new MannedCulverin(null, Direction.North));
        var player = NewPlayer();

        Assert.Null(cannon.Operator);
        Assert.False(cannon.CanFireUnmanned); // флаг ставит только плот маяка

        Assert.True(cannon.TryTakeAim(player));
        Assert.Same(player, cannon.Operator);

        // На Map.Internal нет ни карты, ни кораблей — цели быть не может.
        Assert.Null(cannon.AcquireTarget());

        var stranger = NewPlayer();
        Assert.False(cannon.TryTakeAim(stranger));
    }

    [Fact]
    public void GalleonCargoSerializesWithoutFailure()
    {
        var galleon = Add(new BritannianShip(Direction.East));
        var owner = NewPlayer();

        galleon.Owner = owner;
        Assert.True(galleon.TryStoreCargo(owner, Add(new Board(4))));

        var writer = new BufferWriter(true);
        galleon.Serialize(writer);

        Assert.True(writer.Position > 0);
    }
}
