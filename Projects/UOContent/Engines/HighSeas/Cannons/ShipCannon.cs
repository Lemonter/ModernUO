/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/ShipCannon.cs).
///
///     Второе («журнальное») поколение пушек High Seas: сама пушка — контейнер на три предмета, в
///     который экипаж складывает порох, снаряд и фитиль. Дальше одна кнопка «ЗАРЯДИТЬ» прогоняет весь
///     цикл: подготовка (шомпол) → заряд пороха → снаряд → фитиль. Ещё одна кнопка — «ВЫСТРЕЛ».
///     Именно эти пушки выдают деи <see cref="CulverinDeed" />/<see cref="CarronadeDeed" />/
///     <see cref="BlundercannonDeed" />.
///
///     Адаптация под этот движок:
///     <list type="bullet">
///         <item>
///             <c>BaseGump</c> заменён на обычный <see cref="Gump" /> со статическим <c>DisplayTo</c>
///             (в этом движке <c>BaseGump</c> — другой класс, а окно пушки ServUO было на нём).
///         </item>
///         <item>
///             Проверка «всё ещё на палубе» (<c>Galleon.Contains</c>) заменена на дистанцию до пушки:
///             <c>Contains</c> требует, чтобы у корабля была карта, а цикл перезарядки должен
///             работать и в тестовом хосте без данных клиента.
///         </item>
///         <item>
///             Анимация <c>m.Animate(AnimationType.Attack, 3)</c> не портирована: в этом движке
///             <c>Animate</c> принимает только числовой код действия, а <c>AnimationType</c> нет.
///         </item>
///         <item>
///             Список зрителей окна (<c>Viewing</c>) не сохраняется — после рестарта окно просто
///             откроется заново по двойному клику.
///         </item>
///         <item>
///             Положение на палубе — смещением в «северной» системе координат (<see cref="PadOffset" />),
///             как и у <see cref="BaseCannon" />.
///         </item>
///         <item>
///             Урон считается общей для обеих пушек функцией <see cref="CannonShot" />; выстрел
///             разрешается сразу, без отложенных «полётов» по клеткам.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.HighSeas;

/// <summary>Шаг цикла перезарядки (в ServUO — <c>CannonAction</c>).</summary>
public enum CannonAction
{
    None,
    Stop,
    Fail,
    Finish
}

[SerializationGenerator(0, false)]
public abstract partial class BaseShipCannon : Container, IShipCannon
{
    public const int MagazineSlots = 3;

    /// <summary>Задержка подготовки (в ServUO — 4 с: дольше остальных шагов).</summary>
    public static readonly TimeSpan PreparationTime = TimeSpan.FromSeconds(4);

    /// <summary>Задержка между поджогом фитиля и выстрелом (в ServUO — 1.5 с).</summary>
    public static readonly TimeSpan FuseDelay = TimeSpan.FromSeconds(1.5);

    // Индексы полей закреплены: 0–7 объявлены здесь, дальше только добавлять.
    [SerializableField(0, allowFieldChange: nameof(AllowHitsChange), fieldChanged: nameof(OnHitsChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _hits;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CannonAction _prepered;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CannonAction _charged;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CannonAction _loaded;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CannonAction _primed;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private AmmunitionType _ammoType;

    [SerializableField(6, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseGalleon _galleon;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ShipPosition _position;

    [SerializableField(8, fieldChanged: nameof(OnPadOffsetChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _padOffset;

    private readonly List<TimerExecutionToken> _tokens = [];
    private bool _processing;

    protected BaseShipCannon(BaseGalleon galleon) : base(0x2198)
    {
        Movable = false;
        _galleon = galleon;
        _ammoType = AmmunitionType.Empty;
        _hits = MaxHits;

        LiftOverride = true;
        MaxItems = MagazineSlots;
    }

    public virtual int MaxHits => 100;

    public virtual int Range => 10;

    public virtual TimeSpan ActionTime => TimeSpan.FromSeconds(1.5);

    /// <summary>Класс пушки: от него зависит арт на палубе и дей при разборке.</summary>
    public virtual CannonPower Power => CannonPower.Light;

    public abstract ShipCannonDeed GetDeed { get; }

    /// <summary>Идёт ли сейчас шаг цикла (в ServUO — <c>Processing</c>).</summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Processing => _processing;

    [CommandProperty(AccessLevel.GameMaster)]
    public Direction Facing => GetFacing();

    [CommandProperty(AccessLevel.GameMaster)]
    public DamageLevel DamageState => GetDamageLevel();

    /// <summary>Ствол готов к выстрелу: подготовлен, заряжен, снаряжён и взведён.</summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public bool CanLight =>
        _prepered == CannonAction.Finish && _loaded == CannonAction.Finish &&
        _charged == CannonAction.Finish && _primed == CannonAction.Finish;

    /// <summary>Пушка пуста и не готова к выстрелу (можно разбирать).</summary>
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Empty => !CanLight && Items.Count == 0;

    [CommandProperty(AccessLevel.GameMaster)]
    public double Durability => (double)_hits / MaxHits * 100.0;

    public override bool ForceShowProperties => true;

    public override int DefaultGumpID => 0x9CE7;

    public override bool DisplaysContent => false;

    public override int DefaultMaxWeight => 300;

    private bool AllowHitsChange(ref int value)
    {
        value = Math.Clamp(value, 0, MaxHits);
        return true;
    }

    private void OnHitsChanged(int oldValue, int newValue)
    {
        InvalidateProperties();
        CheckDestroyed(null);
    }

    private void OnPadOffsetChanged(Point3D oldValue, Point3D newValue) => _galleon?.UpdateCannonID(this);

    /// <summary>Ствол смотрит в сторону борта/носа, на котором стоит.</summary>
    public Direction GetFacing()
    {
        var galleon = _galleon;

        if (galleon == null)
        {
            return Direction.North;
        }

        if (_padOffset.X == 0)
        {
            return galleon.Facing;
        }

        var local = _padOffset.X < 0 ? Direction.West : Direction.East;

        return (Direction)(((int)local + (int)galleon.Facing) & 0x7);
    }

    public DamageLevel GetDamageLevel() => Durability switch
    {
        >= 100.0 => DamageLevel.Pristine,
        >= 75.0  => DamageLevel.Slightly,
        >= 50.0  => DamageLevel.Moderately,
        >= 25.0  => DamageLevel.Heavily,
        _        => DamageLevel.Severely
    };

    public virtual void OnDamage(int damage, Mobile shooter)
    {
        if (Deleted || damage <= 0)
        {
            return;
        }

        Hits -= damage;
    }

    protected void CheckDestroyed(Mobile from)
    {
        if (_hits > 0 || Deleted)
        {
            return;
        }

        DoAreaMessage(1116297, 5, null); // The ship cannon has been destroyed!
        from?.SendLocalizedMessage(1116297);

        Delete();
    }

    #region Журнал (контейнер)

    /// <summary>В пушку можно класть только порох, снаряд и фитиль, и не больше трёх предметов.</summary>
    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (!IsMagazineItem(item))
        {
            if (message)
            {
                m?.SendLocalizedMessage(1074836); // The container can not hold that type of object.
            }

            return false;
        }

        if (Items.Count >= MagazineSlots)
        {
            if (message)
            {
                m?.SendLocalizedMessage(1080017); // That container cannot hold more items.
            }

            return false;
        }

        return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
    }

    private static bool IsMagazineItem(Item item) =>
        item is PowderCharge or FuseCord or ICannonAmmo;

    #endregion

    #region Цикл перезарядки

    /// <summary>Первый шаг: подготовка ствола (нужен шомпол в рюкзаке).</summary>
    public void TryPrep(Mobile from)
    {
        if (Deleted || from == null || _processing)
        {
            return;
        }

        if (!HasInPack(from, typeof(Ramrod)))
        {
            from.SendLocalizedMessage(1149660); // You need a ramrod.
            return;
        }

        _processing = true;

        Schedule(PreparationTime, () => CompletePrep(from));
    }

    private void CompletePrep(Mobile from)
    {
        _processing = false;

        if (Deleted || from?.Deleted != false)
        {
            return;
        }

        if (!IsAtCannon(from))
        {
            Prepered = CannonAction.Stop;
            ResendGump(from);
            return;
        }

        Prepered = CannonAction.Finish;
        ResendGump(from);

        TryCharge(from);
    }

    /// <summary>Второй шаг: пороховой заряд из журнала.</summary>
    public void TryCharge(Mobile from)
    {
        if (Deleted || from == null || _processing)
        {
            return;
        }

        if (_charged == CannonAction.Finish)
        {
            from.SendLocalizedMessage(1116012); // The cannon is already charged.
            return;
        }

        _processing = true;

        Schedule(ActionTime, () => CompleteCharge(from));
    }

    private void CompleteCharge(Mobile from)
    {
        _processing = false;

        if (Deleted || from?.Deleted != false)
        {
            return;
        }

        var charge = FindItemByType<PowderCharge>(false);

        if (charge == null)
        {
            _charged = CannonAction.Fail;
            from.SendLocalizedMessage(1116014); // The magazine does not have a powder charge...
            ResendGump(from);
            return;
        }

        if (!IsAtCannon(from))
        {
            _charged = CannonAction.Stop;
            ResendGump(from);
            return;
        }

        charge.Consume();

        _charged = CannonAction.Finish;
        InvalidateProperties();
        ResendGump(from);

        TryLoad(from);
    }

    /// <summary>Третий шаг: снаряд — из журнала или из рюкзака.</summary>
    public void TryLoad(Mobile from)
    {
        if (Deleted || from == null || _processing)
        {
            return;
        }

        if (_loaded == CannonAction.Finish)
        {
            from.SendLocalizedMessage(1116018); // ...needs to be charged with a rammer and powder charge...
            return;
        }

        _processing = true;

        Schedule(ActionTime, () => CompleteLoad(from));
    }

    private void CompleteLoad(Mobile from)
    {
        _processing = false;

        if (Deleted || from?.Deleted != false)
        {
            return;
        }

        var ammo = FindMagazineAmmo() ??
                   from.Backpack?.FindItemByType<Item>(true, item => item is ICannonAmmo);

        if (ammo is not ICannonAmmo cannonAmmo)
        {
            _loaded = CannonAction.Fail;
            from.SendLocalizedMessage(1158933); // The magazine does not have ammo to load this cannon with.
            ResendGump(from);
            return;
        }

        if (!IsAtCannon(from))
        {
            _loaded = CannonAction.Stop;
            ResendGump(from);
            return;
        }

        _ammoType = cannonAmmo.AmmoType;
        ammo.Consume();

        _loaded = CannonAction.Finish;
        InvalidateProperties();
        ResendGump(from);

        TryPrime(from);
    }

    /// <summary>Четвёртый шаг: фитиль из журнала.</summary>
    public void TryPrime(Mobile from)
    {
        if (Deleted || from == null || _processing)
        {
            return;
        }

        if (_primed == CannonAction.Finish)
        {
            from.SendLocalizedMessage(1116019); // The cannon is already primed and ready to be fired.
            return;
        }

        _processing = true;

        Schedule(ActionTime, () => CompletePrime(from));
    }

    private void CompletePrime(Mobile from)
    {
        _processing = false;

        if (Deleted || from?.Deleted != false)
        {
            return;
        }

        var fuse = FindItemByType<FuseCord>(false);

        if (fuse == null)
        {
            _primed = CannonAction.Fail;
            from.SendLocalizedMessage(1158939); // The magazine does not have a fuse to prime the cannon with.
            ResendGump(from);
            return;
        }

        if (!IsAtCannon(from))
        {
            _primed = CannonAction.Stop;
            ResendGump(from);
            return;
        }

        fuse.Consume();

        _primed = CannonAction.Finish;
        InvalidateProperties();
        from.SendLocalizedMessage(1149652); // Ready to fire.
        ResendGump(from);
    }

    /// <summary>Разрядить пушку: вернуть фитиль, снаряд и заряд хозяину.</summary>
    public void Unload(Mobile from)
    {
        if (from == null || Deleted)
        {
            return;
        }

        if (_primed == CannonAction.Finish)
        {
            GiveBack(from, new FuseCord());
            _primed = CannonAction.None;
        }

        if (_loaded == CannonAction.Finish)
        {
            var ammo = AmmoInfo.GetAmmoInfo(_ammoType)?.CreateAmmo();

            if (ammo != null)
            {
                GiveBack(from, ammo);
            }

            _ammoType = AmmunitionType.Empty;
            _loaded = CannonAction.None;
        }

        if (_charged == CannonAction.Finish)
        {
            GiveBack(from, new PowderCharge());
            _charged = CannonAction.None;
        }

        _prepered = CannonAction.None;

        InvalidateProperties();
        ResendGump(from);
    }

    private static void GiveBack(Mobile from, Item item)
    {
        if (item == null)
        {
            return;
        }

        if (from.Backpack?.TryDropItem(from, item, false) != true)
        {
            item.MoveToWorld(from.Location, from.Map);
        }
    }

    #endregion

    #region Выстрел

    /// <summary>Поджог фитиля горящей спичкой или факелом.</summary>
    public bool TryLightFuse(Mobile from)
    {
        if (!HasFireSource(from))
        {
            from.SendLocalizedMessage(1149669); // Need a lighted fire source.
            return false;
        }

        LightFuse(from);

        return true;
    }

    public void LightFuse(Mobile from) => LightFuse(from, null, Point3D.Zero);

    /// <summary>Прицеливание по конкретной цели с последующим выстрелом.</summary>
    public bool TryFireAt(Mobile from, object targeted)
    {
        if (Deleted)
        {
            return false;
        }

        if (!CanLight)
        {
            from.SendLocalizedMessage(1116021); // ...needs to be charged and loaded before it can be primed.
            return false;
        }

        if (!HasFireSource(from))
        {
            from.SendLocalizedMessage(1149669); // Need a lighted fire source.
            return false;
        }

        var aimed = ResolveTargetShip(targeted);

        if (aimed != null && ReferenceEquals(aimed, _galleon))
        {
            from.SendMessage(0x22, "Свой корабль обстреливать нельзя.");
            return false;
        }

        var point = ResolvePoint(targeted);

        if (!CannonShot.TryGetFireDirection(this, point, out _))
        {
            from.SendMessage(0x22, "Ствол не смотрит в ту сторону — разверните корабль.");
            return false;
        }

        if (!CannonShot.IsInRange(this, point))
        {
            from.SendMessage(0x22, "Цель вне дальности выстрела.");
            return false;
        }

        LightFuse(from, aimed, point);

        return true;
    }

    public void LightFuse(Mobile from, BaseGalleon aimed, Point3D point)
    {
        if (Deleted || from == null)
        {
            return;
        }

        if (!BaseCannon.CheckRegion(from))
        {
            return;
        }

        switch (GetDamageLevel())
        {
            case DamageLevel.Severely:
                from.SendLocalizedMessage(1116608); // The cannon is severely damaged...
                return;
            case DamageLevel.Heavily:
            case DamageLevel.Moderately:
                from.SendLocalizedMessage(1116607); // ...safe to fire.
                break;
            case DamageLevel.Slightly:
                from.SendLocalizedMessage(1116606); // ...needs some minor repair.
                break;
        }

        from.SendLocalizedMessage(1149683); // The fuse is lit!

        Effects.PlaySound(Location, Map, 0x666);

        Schedule(FuseDelay, () => Shoot(from, aimed, point));
    }

    public void Shoot(Mobile shooter) => Shoot(shooter, null, Point3D.Zero);

    public void Shoot(Mobile shooter, BaseGalleon aimed, Point3D point)
    {
        if (Deleted)
        {
            return;
        }

        var ammo = AmmoInfo.GetAmmoInfo(_ammoType);

        if (ammo == null)
        {
            ClearCannon();
            return;
        }

        if (shooter?.Player == true)
        {
            Hits -= Utility.RandomMinMax(0, 4);
        }

        CannonShot.MuzzleEffects(this);
        shooter?.SendLocalizedMessage(1149691); // Fired successfully.

        if (aimed?.Deleted == false)
        {
            CannonShot.HitShip(this, aimed, point, shooter);
        }
        else
        {
            ShootAlongFacing(shooter, ammo);
        }

        ClearCannon();

        ResendGump(shooter);
    }

    private void ShootAlongFacing(Mobile shooter, AmmoInfo ammo)
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        var stepX = 0;
        var stepY = 0;

        switch (Facing)
        {
            case Direction.North:
                stepY = -1;
                break;
            case Direction.South:
                stepY = 1;
                break;
            case Direction.West:
                stepX = -1;
                break;
            case Direction.East:
                stepX = 1;
                break;
        }

        var lateral = 1;
        var origin = Location;

        for (var range = 1; range <= Range; range++)
        {
            if (range % ammo.LateralOffset == 0)
            {
                lateral++;
            }

            for (var offset = -lateral; offset <= lateral; offset++)
            {
                var point = stepX == 0
                    ? new Point3D(origin.X + offset, origin.Y + stepY * range, origin.Z)
                    : new Point3D(origin.X + stepX * range, origin.Y + offset, origin.Z);

                if (CannonShot.TryHitBeacon(this, shooter, point, map))
                {
                    return;
                }

                var target = CannonShot.FindShip(this, point, map);

                if (target != null)
                {
                    // Залп останавливается на первой цели (в ServUO все снаряды SingleTarget).
                    CannonShot.HitShip(this, target, point, shooter);
                    return;
                }

                var mobs = CannonShot.CollectMobiles(this, shooter, point, map);

                if (mobs.Count == 0)
                {
                    continue;
                }

                CannonShot.HitMobiles(this, mobs, point, shooter);

                return;
            }
        }
    }

    public void ClearCannon()
    {
        _prepered = CannonAction.None;
        _charged = CannonAction.None;
        _loaded = CannonAction.None;
        _primed = CannonAction.None;
        _ammoType = AmmunitionType.Empty;

        InvalidateProperties();
    }

    #endregion

    #region Ремонт и разборка

    public bool TryRepairCannon(Mobile from)
    {
        if (from?.Backpack == null)
        {
            return false;
        }

        if (GetDamageLevel() == DamageLevel.Pristine)
        {
            from.SendLocalizedMessage(1116604); // The cannon is in pristine condition and does not need repairs.
            return false;
        }

        var pack = from.Backpack;
        double ingotsNeeded = 36.0 * (100 - (int)Durability);
        ingotsNeeded -= from.Skills[SkillName.Blacksmith].Value / 200.0 * ingotsNeeded;

        var min = ingotsNeeded / 10.0;
        var available = pack.GetAmount(typeof(IronIngot));

        if (available < min)
        {
            from.SendLocalizedMessage(1116603, $"{(int)min}"); // You need a minimum of ~1_METAL~ iron ingots...
            return false;
        }

        var used = Math.Min(available, ingotsNeeded);
        var repaired = available >= ingotsNeeded ? 100.0 : available / ingotsNeeded * 100.0;

        pack.ConsumeTotal(typeof(IronIngot), (int)used);

        Hits += (int)((MaxHits - _hits) * (repaired / 100.0));

        from.SendLocalizedMessage(1116605, $"{(int)used}\t{(int)Durability}"); // You make repairs...

        return true;
    }

    public bool TryDismantle(Mobile from)
    {
        if (!Empty)
        {
            from.SendLocalizedMessage(1116321); // ...must be fully unloaded before it can be dismantled.
            return false;
        }

        if (GetDamageLevel() != DamageLevel.Pristine)
        {
            from.SendLocalizedMessage(1116322); // ...must be fully repaired before it can be dismantled.
            return false;
        }

        var deed = GetDeed;

        if (deed == null)
        {
            return false;
        }

        DoAreaMessage(1116073, 10, from); // ~1_NAME~ dismantles the ship cannon.

        Delete();

        if (from.Backpack?.TryDropItem(from, deed, false) != true)
        {
            deed.MoveToWorld(from.Location, from.Map);
        }

        return true;
    }

    #endregion

    #region Вспомогательное

    private Item FindMagazineAmmo()
    {
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i] is ICannonAmmo)
            {
                return Items[i];
            }
        }

        return null;
    }

    protected bool IsAtCannon(Mobile from) => from?.Map == Map && from.InRange(Location, 3);

    protected static bool HasInPack(Mobile from, Type type) => from?.Backpack?.GetAmount(type) >= 1;

    protected static bool HasFireSource(Mobile from) => BaseCannon.HasFireSource(from);

    protected static BaseGalleon ResolveTargetShip(object targeted) => BaseCannon.ResolveTargetShip(targeted);

    protected static Point3D ResolvePoint(object targeted) => BaseCannon.ResolvePoint(targeted);

    public void DoAreaMessage(int cliloc, int range, Mobile from)
    {
        var map = Map;

        if (map == null || map == Map.Internal)
        {
            return;
        }

        foreach (var mob in map.GetMobilesInRange<PlayerMobile>(Location, range))
        {
            if (mob.Deleted || mob == from || !mob.InLOS(this))
            {
                continue;
            }

            if (from != null)
            {
                mob.SendLocalizedMessage(cliloc, from.Name);
            }
            else
            {
                mob.SendLocalizedMessage(cliloc);
            }
        }
    }

    private void Schedule(TimeSpan delay, Action action)
    {
        for (var i = _tokens.Count - 1; i >= 0; i--)
        {
            if (!_tokens[i].Running)
            {
                _tokens.RemoveAt(i);
            }
        }

        Timer.StartTimer(delay, action, out var token);
        _tokens.Add(token);
    }

    private void CancelTimers()
    {
        for (var i = 0; i < _tokens.Count; i++)
        {
            _tokens[i].Cancel();
        }

        _tokens.Clear();
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        _galleon?.UpdateCannonID(this);
    }

    public override void OnDelete()
    {
        CancelTimers();

        var galleon = _galleon;
        _galleon = null;
        galleon?.RemoveCannon(this);

        base.OnDelete();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1116026, _charged == CannonAction.Finish ? "#1116031" : "#1116032"); // Charged: ~1_VALUE~
        list.Add(1116027, AmmoInfo.GetAmmoName(_ammoType)); // Ammo: ~1_VALUE~
        list.Add(1116028, _primed == CannonAction.Finish ? "#1116031" : "#1116032"); // Primed: ~1_VALUE~
        list.Add(1116580 + (int)GetDamageLevel()); // состояние ствола
        list.Add(1072241, $"{TotalItems}\t{MaxItems}\t{TotalWeight}\t{MaxWeight}"); // содержимое журнала
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (_galleon == null || _galleon.GetSecurityLevel(from) < SecurityLevel.Officer)
        {
            return;
        }

        list.Add(new MagazineEntry(this, from, MagazineAction.Unload));

        if (Empty && GetDamageLevel() == DamageLevel.Pristine)
        {
            list.Add(new MagazineEntry(this, from, MagazineAction.Dismantle));
        }

        if (GetDamageLevel() != DamageLevel.Pristine)
        {
            list.Add(new MagazineEntry(this, from, MagazineAction.Repair));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from == null || Deleted)
        {
            return;
        }

        if (_galleon == null || _galleon.GetSecurityLevel(from) < SecurityLevel.Crewman)
        {
            from.Say(1010436); // You do not have permission to do this.
            return;
        }

        if (!from.InRange(Location, 2))
        {
            from.SendLocalizedMessage(1149654); // You are too far away.
            return;
        }

        ShipCannonGump.DisplayTo(from, this);
    }

    private enum MagazineAction
    {
        Unload,
        Dismantle,
        Repair
    }

    private class MagazineEntry : ContextMenuEntry
    {
        private readonly BaseShipCannon _cannon;
        private readonly Mobile _from;
        private readonly MagazineAction _action;

        public MagazineEntry(BaseShipCannon cannon, Mobile from, MagazineAction action) : base(GetLabel(action), 2)
        {
            _cannon = cannon;
            _from = from;
            _action = action;
        }

        private static int GetLabel(MagazineAction action) => action switch
        {
            MagazineAction.Unload    => 1116072,
            MagazineAction.Dismantle => 1116069,
            _                        => 1116602
        };

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_cannon.Deleted)
            {
                return;
            }

            switch (_action)
            {
                case MagazineAction.Unload:
                    _cannon.Unload(_from);
                    break;
                case MagazineAction.Dismantle:
                    _cannon.TryDismantle(_from);
                    break;
                case MagazineAction.Repair:
                    _cannon.TryRepairCannon(_from);
                    break;
            }
        }
    }

    public void ResendGump(Mobile from) => ShipCannonGump.DisplayTo(from, this);

    #endregion
}

/// <summary>Окно пушки с журналом (в ServUO — <c>BaseShipCannon.ShipCannonGump</c>).</summary>
public class ShipCannonGump : Gump
{
    private const int LabelColor = 0x7FFF;
    private const int ReadyColor = 0x1FE7;
    private const int EmptyColor = 0x7CE7;

    private readonly BaseShipCannon _cannon;

    private ShipCannonGump(BaseShipCannon cannon) : base(100, 100)
    {
        _cannon = cannon;

        AddPage(0);
        AddBackground(0, 0, 250, 175, 0x6DB);

        AddLabel(10, 10, LabelColor, GetPositionName(cannon.Position));
        AddLabel(115, 35, LabelColor, "СОСТОЯНИЕ");

        if (cannon.CanLight)
        {
            AddButton(10, 35, 0xFA5, 0xFA7, 8);
            AddLabel(45, 35, LabelColor, "РАЗРЯДИТЬ");

            AddButton(10, 89, 0xFA5, 0xFA7, 6);
            AddLabel(45, 89, LabelColor, "ВЫСТРЕЛ");
        }
        else
        {
            AddButton(10, 35, 0xFA5, 0xFA7, 1);
            AddLabel(45, 35, LabelColor, "ЗАРЯДИТЬ");
        }

        AddLabel(115, 53, cannon.Charged == CannonAction.Finish ? ReadyColor : EmptyColor,
            cannon.Charged == CannonAction.Finish ? "заряжено" : "не заряжено");
        AddLabel(115, 71, cannon.Loaded == CannonAction.Finish ? ReadyColor : EmptyColor,
            cannon.Loaded == CannonAction.Finish ? AmmoInfo.GetAmmoName(cannon.AmmoType) : "без снаряда");
        AddLabel(115, 89, cannon.Primed == CannonAction.Finish ? ReadyColor : EmptyColor,
            cannon.Primed == CannonAction.Finish ? "фитиль взведён" : "нет фитиля");

        AddLabel(10, 130, LabelColor, $"Журнал: {cannon.Items.Count}/{BaseShipCannon.MagazineSlots}");
    }

    public static void DisplayTo(Mobile from, BaseShipCannon cannon)
    {
        if (from?.NetState == null || cannon?.Deleted != false)
        {
            return;
        }

        from.CloseGump<ShipCannonGump>();
        from.SendGump(new ShipCannonGump(cannon));
    }

    private static string GetPositionName(ShipPosition position) => position switch
    {
        ShipPosition.Bow              => "Пушка: нос",
        ShipPosition.BowPort          => "Пушка: нос, левый борт",
        ShipPosition.BowStarboard     => "Пушка: нос, правый борт",
        ShipPosition.AmidShipPort     => "Пушка: середина, левый борт",
        ShipPosition.AmidShipStarboard => "Пушка: середина, правый борт",
        ShipPosition.AftPort          => "Пушка: корма, левый борт",
        ShipPosition.AftStarboard     => "Пушка: корма, правый борт",
        _                             => "Пушка: корма"
    };

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null || _cannon?.Deleted != false)
        {
            return;
        }

        if (info.ButtonID == 0 || _cannon.Galleon?.Contains(from) != true)
        {
            return;
        }

        if (!from.InRange(_cannon.Location, 2) || !from.Alive)
        {
            from.SendLocalizedMessage(1149654); // You are too far away.
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                if (_cannon.Processing)
                {
                    break;
                }

                if (_cannon.Prepered != CannonAction.Finish)
                {
                    _cannon.TryPrep(from);
                }
                else if (_cannon.Charged != CannonAction.Finish)
                {
                    _cannon.TryCharge(from);
                }
                else if (_cannon.Loaded != CannonAction.Finish)
                {
                    _cannon.TryLoad(from);
                }
                else if (_cannon.Primed != CannonAction.Finish)
                {
                    _cannon.TryPrime(from);
                }

                break;
            case 6:
                if (_cannon.CanLight)
                {
                    from.Target = new FireTarget(_cannon);
                }

                break;
            case 8:
                _cannon.Unload(from);
                break;
        }

        DisplayTo(from, _cannon);
    }

    /// <summary>Прицел на цель выстрела (в ServUO выстрел шёл строго по стволу).</summary>
    private class FireTarget : Target
    {
        private readonly BaseShipCannon _cannon;

        public FireTarget(BaseShipCannon cannon) : base(cannon.Range, false, TargetFlags.None) => _cannon = cannon;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_cannon.Deleted)
            {
                return;
            }

            if (!_cannon.TryFireAt(from, targeted))
            {
                _cannon.TryLightFuse(from);
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class Culverin : BaseShipCannon
{
    [Constructible]
    public Culverin(BaseGalleon galleon) : base(galleon)
    {
    }

    public override int Range => 10;

    public override CannonPower Power => CannonPower.Light;

    public override ShipCannonDeed GetDeed => new CulverinDeed();
}

[SerializationGenerator(0, false)]
public partial class Carronade : BaseShipCannon
{
    [Constructible]
    public Carronade(BaseGalleon galleon) : base(galleon)
    {
    }

    public override int Range => 10;

    public override TimeSpan ActionTime => TimeSpan.FromSeconds(2.0);

    public override CannonPower Power => CannonPower.Heavy;

    public override ShipCannonDeed GetDeed => new CarronadeDeed();
}

[SerializationGenerator(0, false)]
public partial class Blundercannon : BaseShipCannon
{
    [Constructible]
    public Blundercannon(BaseGalleon galleon) : base(galleon)
    {
    }

    public override int Range => 12;

    public override TimeSpan ActionTime => TimeSpan.FromSeconds(2.0);

    public override CannonPower Power => CannonPower.Massive;

    public override int LabelNumber => 1158942; // Blundercannon

    public override ShipCannonDeed GetDeed => new BlundercannonDeed();
}
