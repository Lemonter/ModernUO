/// <summary>
///     Портировано из ServUO (Scripts/Services/Seasonal Events/RisingTide/PlunderBeacon.cs — класс
///     <c>PlunderBeacon</c>, наследник <c>Beacon : DamageableItem</c>).
///
///     Пиратский маяк — плавучая цель Rising Tide: его расстреливают с кораблей или рубят вручную,
///     а когда он разлетается, победитель получает морской груз (<see cref="MaritimeCargo" />).
///
///     Чем отличается от ServUO (упрощение, разрешённое заданием):
///     <list type="bullet">
///         <item>
///             <c>DamageableItem</c> и <c>Beacon</c> в этом движке нет. Устройство повторяет уже
///             принятый здесь образец разрушаемого маяка — <c>Engines/Blackthorn/Invasion/InvasionBeacon.cs</c>:
///             запас прочности (<see cref="Hits" />/<see cref="HitsMax" />), три состояния арта,
///             список ударивших с суммами урона и <c>GetLootingRights</c> с порогом в 1/16 прочности.
///         </item>
///         <item>
///             Клиент не умеет бить предметы, поэтому урон приходит двумя путями: удар оружием по
///             двойному клику (<see cref="Strike" />, как у InvasionBeacon) и попадание корабельной
///             пушки этапа 3 (<c>CannonShot.TryHitBeacon</c> → <see cref="ApplyDamage" />).
///         </item>
///         <item>
///             <c>Controller.BeaconVulnerable</c> в ServUO запирал маяк, пока живы пушки-расчёты.
///             Пиратских расчётов (<c>PirateCrew</c>) в этом движке нет, поэтому маяк уязвим всегда:
///             его защищают только заспавненные вокруг существа (см. <c>PlunderBeaconAddon</c>).
///         </item>
///         <item>
///             Награда выдаётся сразу и гарантированно лучшему по урону (в ServUO — одному случайному
///             из подходящих с шансом 50%): иначе исход зависел бы от броска.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class PlunderBeacon : Item
{
    /// <summary>Целый маяк (в ServUO — стартовый ID <c>Beacon</c>).</summary>
    public const int StartId = 18212;

    /// <summary>Первое состояние повреждений — с половины прочности.</summary>
    public const int HalfHitsId = 39299;

    /// <summary>Второе состояние — с четверти прочности.</summary>
    public const int BatteredId = 39300;

    /// <summary>Разрушенный маяк (в ServUO — <c>IDDestroyed</c> у <c>DamageableItem</c>).</summary>
    public const int RuinedId = 1;

    /// <summary>Запас прочности маяка (в ServUO — <c>HitsMax = 70000</c>).</summary>
    public const int DefaultHitsMax = 70000;

    /// <summary>Сопротивление физическому урону (в ServUO — <c>ResistBasePhys = 0</c>).</summary>
    public const int PhysicalResist = 0;

    /// <summary>С какого расстояния победитель ещё считается участником (в ServUO — 20 клеток).</summary>
    public const int RewardRange = 20;

    private const double QuarterHits = 0.25;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _hits;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _destroyed;

    [SerializableField(2, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private PlunderBeaconAddon _controller;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _lastDamager;

    // Список ударивших и суммы урона лежат параллельно, поэтому [Tidy] здесь нельзя: вычистка
    // удалённых мобилов сдвинула бы индексы относительно сумм. Мёртвых отсеивают GetLootingRights
    // и FindWinner.
    [SerializableField(4)]
    private List<Mobile> _damagers = [];

    [SerializableField(5)]
    private List<int> _damageTotals = [];

    /// <summary>Пустой конструктор — только ради <c>[add</c>; настоящий маяк создаёт плот.</summary>
    [Constructible]
    public PlunderBeacon() : this(null)
    {
    }

    public PlunderBeacon(PlunderBeaconAddon controller) : base(StartId)
    {
        _controller = controller;
        _hits = HitsMax;

        Movable = false;
        Name = "пиратский маяк добычи";
    }

    /// <summary>Прочность маяка (в ServUO — <c>HitsMax</c> у <c>DamageableItem</c>).</summary>
    public virtual int HitsMax => DefaultHitsMax;

    /// <summary>Маяк ещё не разрушен.</summary>
    public bool CanDamage => !Deleted && !_destroyed && _hits > 0;

    public override bool ForceShowProperties => true;

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
        if (from == null)
        {
            return;
        }

        if (from.AccessLevel > AccessLevel.GameMaster)
        {
            from.SendGump(new PropertiesGump(from, this));
            return;
        }

        Strike(from);
    }

    /// <summary>
    ///     Один удар оружием по маяку. В ServUO попадание приходило из обычного боя (клиент умел
    ///     атаковать разрушаемый предмет); в этом движке клиент предметы не атакует, поэтому удар
    ///     наносит двойной клик — тот же приём, что у <c>InvasionBeacon</c> и тренировочных чучел.
    /// </summary>
    private void Strike(Mobile from)
    {
        if (!CanDamage || from.Deleted || !from.Alive)
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        var weapon = from.Weapon;

        if (weapon is BaseWeapon baseWeapon)
        {
            baseWeapon.PlaySwingAnimation(from);
        }

        weapon.GetStatusDamage(from, out var min, out var max);

        var damage = Math.Max(1, Utility.RandomMinMax(min, max) * (100 - PhysicalResist) / 100);

        if (Map != null && Map != Map.Internal)
        {
            Effects.SendLocationEffect(Location, Map, 0x36CB, 14, 10, 2498, 2);
        }

        from.PlaySound(0x23B);

        ApplyDamage(from, damage);
    }

    /// <summary>
    ///     Засчитывает урон по маяку. Публичный, потому что тем же путём идёт и пушечное попадание
    ///     (<see cref="CannonShot.TryHitBeacon" />), и урон ГМ-командой.
    /// </summary>
    public void ApplyDamage(Mobile from, int amount)
    {
        if (!CanDamage || amount <= 0)
        {
            return;
        }

        RegisterDamage(from, amount);

        if (from != null && from.AccessLevel <= AccessLevel.Player)
        {
            _lastDamager = from;
        }

        _hits = Math.Max(0, _hits - amount);
        InvalidateProperties();
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

        _damagers ??= [];
        _damageTotals ??= [];

        var index = _damagers.IndexOf(from);

        if (index >= 0)
        {
            _damageTotals[index] += amount;
            return;
        }

        _damagers.Add(from);
        _damageTotals.Add(amount);
    }

    /// <summary>
    ///     Участники, заслужившие добычу: все, кто нанёс хотя бы 1/16 прочности маяка
    ///     (в ServUO — <c>DamageableItem.GetLootingRights</c>).
    /// </summary>
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

    /// <summary>Лучший по урону участник: он и получает груз.</summary>
    public Mobile FindWinner()
    {
        if (_damagers == null || _damageTotals == null)
        {
            return _lastDamager;
        }

        Mobile best = null;
        var bestDamage = 0;

        for (var i = 0; i < _damagers.Count && i < _damageTotals.Count; i++)
        {
            var damager = _damagers[i];

            if (damager?.Deleted != false || _damageTotals[i] <= bestDamage)
            {
                continue;
            }

            best = damager;
            bestDamage = _damageTotals[i];
        }

        return best ?? _lastDamager;
    }

    private void UpdateItemId()
    {
        ItemID = _hits switch
        {
            <= 0                       => RuinedId,
            _ when _hits <= HitsMax * QuarterHits => BatteredId,
            _ when _hits <= HitsMax / 2           => HalfHitsId,
            _                                     => StartId
        };
    }

    /// <summary>
    ///     Маяк разбит: груз победителю и затопление плота (в ServUO — <c>OnBeforeDestroyed</c> +
    ///     <c>Delete</c>).
    /// </summary>
    public void Destroy()
    {
        if (_destroyed || Deleted)
        {
            return;
        }

        _destroyed = true;

        var map = Map;

        if (map != null && map != Map.Internal)
        {
            Effects.SendLocationEffect(Location, map, 0x3709, 30, 20);
            Effects.PlaySound(Location, map, 0x307);
        }

        AwardCargo();

        var controller = _controller;
        controller?.OnBeaconDestroyed();

        Delete();
    }

    /// <summary>
    ///     Выдаёт морской груз лучшему участнику (в ServUO — <c>winner.AddToBackpack(new
    ///     MaritimeCargo(CargoQuality.Mythical))</c>). Это и есть точка, в которой убийство маяка
    ///     становится источником груза Rising Tide.
    /// </summary>
    private void AwardCargo()
    {
        var winner = FindWinner();

        if (winner?.Deleted != false)
        {
            return;
        }

        var cargo = new MaritimeCargo(CargoQuality.Mythical);

        if (!GiveCargo(winner, cargo))
        {
            cargo.Delete();
            return;
        }

        winner.SendLocalizedMessage(1158907); // You recover maritime trade cargo!

        // Второй участник, если он тоже нанёс достаточно урона, получает груз пониже качеством.
        var rights = GetLootingRights();

        for (var i = 0; i < rights.Count; i++)
        {
            var other = rights[i];

            if (other?.Deleted != false || ReferenceEquals(other, winner))
            {
                continue;
            }

            var extra = new MaritimeCargo(CargoQuality.Grandmaster);

            if (GiveCargo(other, extra))
            {
                other.SendLocalizedMessage(1158907);
            }
            else
            {
                extra.Delete();
            }
        }
    }

    private static bool GiveCargo(Mobile winner, Item cargo)
    {
        if (winner.Backpack?.TryDropItem(winner, cargo, false) == true)
        {
            return true;
        }

        if (winner.BankBox?.TryDropItem(winner, cargo, false) == true)
        {
            return true;
        }

        if (winner.Map != null && winner.Map != Map.Internal)
        {
            cargo.MoveToWorld(winner.Location, winner.Map);
            return true;
        }

        return false;
    }

    public override void OnAfterDelete()
    {
        _controller = null;
        _lastDamager = null;
        _damagers?.Clear();
        _damageTotals?.Clear();

        base.OnAfterDelete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        _damagers ??= [];
        _damageTotals ??= [];

        if (_hits <= 0)
        {
            _hits = 0;
            _destroyed = true;
            Visible = false;
        }

        UpdateItemId();
    }
}
