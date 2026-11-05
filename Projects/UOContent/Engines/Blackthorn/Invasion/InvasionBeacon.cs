using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Blackthorn;

/// <summary>
///     Ported from ServUO's Blackthorn invasion spawner (InvasionSpawner/InvasionBeacon.cs) — Minax's
///     lighthouse, the invasion's win condition: it only accepts damage while no Captain of the current
///     wave is alive, calls the next wave out when it is beaten down to a quarter of its hits, and hands
///     the artifacts out when it falls.
///
///     ServUO's beacon was a <c>Beacon : DamageableItem</c> — a destructible-prop framework (item health
///     bar, per-damager looting rights, resistances) that does not exist in this codebase. Items here
///     cannot be attacked at all (the attack packet only resolves mobiles, and <c>Item</c> has no hits),
///     so this is a plain <see cref="Item" /> that carries the whole DamageableItem contract itself:
///       - <see cref="Hits" /> / <see cref="HitsMax" /> and the three-item-id damaged states come from
///         ServUO's <c>ItemLevel.Easy</c> (100 + level² × 500 hits).
///       - Damage arrives through <see cref="ApplyDamage" />. The player-facing entry point is a
///         weapon swing on double click (this codebase's idiom for striking a prop — see
///         Items/Addons/TrainingDummies.cs), priced with the same strength/tactics-scaled damage range
///         the status bar shows, so one click stands in for one combat round.
///       - The per-damager ledger ServUO's <c>DamageStore</c> kept is <see cref="_damagers" /> /
///         <see cref="_damageTotals" />, read back by <see cref="GetLootingRights" /> with the same
///         one-sixteenth threshold.
///       - The status-health bar packets (<c>MobileHitsN</c> / <c>MobileStatusCompact</c>) and the
///         item's <c>OnStatsQuery</c> have no counterpart for items; the hit pool is shown in the
///         tooltip instead.
///     ServUO's <c>Beacon</c> also owned the lighthouse rubble/area-blast behaviour and a
///     <c>BeaconItem</c> companion targeting proxy — none of that is part of the invasion, so it is not
///     carried over.
/// </summary>
[SerializationGenerator(0, false)]
public partial class InvasionBeacon : Item
{
    /// <summary>Intact lighthouse (ServUO's <c>Beacon</c> start id).</summary>
    public const int StartId = 18212;

    /// <summary>First damaged state, entered at half hits.</summary>
    public const int HalfHitsId = 39299;

    /// <summary>Second damaged state, entered when the next wave is called out at a quarter hits.</summary>
    public const int BatteredId = 39300;

    /// <summary>ServUO's destroyed id: the ruined lighthouse stays in the world, invisible.</summary>
    public const int RuinedId = 1;

    /// <summary>ServUO's <c>ItemLevel.Easy</c>: 100 + (2 × 100) × (2 × 5).</summary>
    public const int HitsMax = 2100;

    /// <summary>ServUO's <c>Beacon.ResistBasePhys</c>.</summary>
    public const int PhysicalResist = 50;

    private const double QuarterHits = 0.25;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _hits;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _destroyed;

    [SerializableField(2)]
    private bool _halfDamageWaveSpawned;

    [SerializableField(3)]
    private List<Mobile> _damagers;

    [SerializableField(4)]
    private List<int> _damageTotals;

    private TimerExecutionToken _waveToken;

    public InvasionBeacon(InvasionController controller) : base(StartId)
    {
        Controller = controller;

        Name = "маяк";
        Movable = false;

        _hits = HitsMax;
        _damagers = new List<Mobile>();
        _damageTotals = new List<int>();
    }

    /// <summary>Set by the controller when it deserializes; deliberately not serialized, so the
    /// controller → beacon reference stays one-way and cannot form a save cycle.</summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public InvasionController Controller { get; set; }

    public override bool ForceShowProperties => true;

    /// <summary>ServUO's <c>CanDamage</c>: the beacon is only vulnerable while it has no Captains left.</summary>
    public bool CanDamage => Controller == null || Controller.BeaconVulnerable;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (!_destroyed)
        {
            list.Add(1060639, $"{_hits}\t{HitsMax}"); // durability: ~1_val~ / ~2_val~
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.AccessLevel > AccessLevel.GameMaster)
        {
            from.SendGump(new PropertiesGump(from, this));
            return;
        }

        Strike(from);
    }

    /// <summary>One weapon swing against the beacon. ServUO got here through the normal combat loop
    /// (the client could attack the damageable item); this codebase's client cannot target items, so a
    /// double click plays the swing and applies the hit.</summary>
    private void Strike(Mobile from)
    {
        if (_destroyed || from.Deleted || !from.Alive)
        {
            return;
        }

        // The client only shows the beacon to nearby players, but a crafted double click carries no
        // range of its own, so the swing is gated here.
        if (!from.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        if (!CanDamage)
        {
            if (from.NetState != null)
            {
                // *Minax's Beacon surges with energy into an invulnerable state! Defeat her Captains to weaken the Beacon's defenses!*
                from.PrivateOverheadMessage(MessageType.Regular, 1154, 1154551, from.NetState);
            }

            return;
        }

        var weapon = from.Weapon;

        if (weapon is BaseWeapon baseWeapon)
        {
            baseWeapon.PlaySwingAnimation(from);
        }

        weapon.GetStatusDamage(from, out var min, out var max);

        var damage = Math.Max(1, Utility.RandomMinMax(min, max) * (100 - PhysicalResist) / 100);

        if (Map != null)
        {
            Effects.SendLocationEffect(Location, Map, 0x36CB, 14, 10, 2498, 2);
        }

        from.PlaySound(0x23B);

        ApplyDamage(from, damage);
    }

    /// <summary>Credit a hit against the beacon. Public so scripted/GM damage can reuse the same path
    /// as a weapon swing.</summary>
    public void ApplyDamage(Mobile from, int amount)
    {
        if (_destroyed || Deleted || amount <= 0)
        {
            return;
        }

        RegisterDamage(from, amount);

        _hits = Math.Max(0, _hits - amount);
        InvalidateProperties();

        if (!_halfDamageWaveSpawned && _hits <= HitsMax * QuarterHits)
        {
            _halfDamageWaveSpawned = true;
            OnHalfDamage();
        }

        UpdateItemId();

        if (_hits <= 0)
        {
            Destroy();
        }
    }

    private void RegisterDamage(Mobile from, int amount)
    {
        if (from == null)
        {
            return;
        }

        if (from is BaseCreature creature && creature.GetMaster() is PlayerMobile master)
        {
            from = master;
        }

        _damagers ??= new List<Mobile>();
        _damageTotals ??= new List<int>();

        var index = _damagers.IndexOf(from);

        if (index >= 0)
        {
            _damageTotals[index] += amount;
        }
        else
        {
            _damagers.Add(from);
            _damageTotals.Add(amount);
        }
    }

    /// <summary>ServUO's <c>DamageableItem.GetLootingRights</c>: everyone who did at least a sixteenth
    /// of the beacon's hits, in no particular order. Never null (the caller does not have to null-check
    /// the way it did for ServUO's damage store).</summary>
    public List<Mobile> GetLootingRights()
    {
        var rights = new List<Mobile>();

        if (_damagers == null || _damageTotals == null)
        {
            return rights;
        }

        for (var i = 0; i < _damagers.Count && i < _damageTotals.Count; i++)
        {
            if (_damagers[i] != null && _damageTotals[i] >= HitsMax / 16)
            {
                rights.Add(_damagers[i]);
            }
        }

        return rights;
    }

    /// <summary>ServUO's <c>Beacon.OnHalfDamage</c>: the next wave is called out one second later.</summary>
    private void OnHalfDamage()
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        foreach (var m in map.GetMobilesInRange(Location, 20))
        {
            if (m.NetState != null)
            {
                // *Minax's Beacon surges with energy into an invulnerable state! Defeat her Captains to weaken the Beacon's defenses!*
                m.PrivateOverheadMessage(MessageType.Regular, 1154, 1154551, m.NetState);
            }
        }

        _waveToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(1), SpawnNextWave, out _waveToken);
    }

    private void SpawnNextWave()
    {
        if (Deleted)
        {
            return;
        }

        Controller?.SpawnWave();
    }

    private void UpdateItemId()
    {
        if (_hits <= 0)
        {
            ItemID = RuinedId;
        }
        else if (_hits <= HitsMax * QuarterHits)
        {
            ItemID = BatteredId;
        }
        else if (_hits <= HitsMax / 2)
        {
            ItemID = HalfHitsId;
        }
        else
        {
            ItemID = StartId;
        }
    }

    /// <summary>ServUO's <c>DamageableItem.Destroy</c> for this beacon: the controller is told first (it
    /// hands out the artifacts while the damage ledger is still intact), then the lighthouse is left
    /// standing but invisible, which is what <c>IDDestroyed = 1</c> and <c>DeleteOnDestroy = false</c>
    /// did.</summary>
    public void Destroy()
    {
        if (_destroyed || Deleted)
        {
            return;
        }

        _destroyed = true;

        Effects.PlaySound(Location, Map, 0x3B3);

        Controller?.OnBeaconDestroyed();

        ItemID = RuinedId;
        Visible = false;

        InvalidateProperties();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _waveToken.Cancel();

        Controller = null;
        _damagers?.Clear();
        _damageTotals?.Clear();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _damagers ??= new List<Mobile>();
        _damageTotals ??= new List<int>();

        if (_hits <= 0)
        {
            _hits = 0;
            _destroyed = true;
            Visible = false;
        }

        UpdateItemId();
    }
}
