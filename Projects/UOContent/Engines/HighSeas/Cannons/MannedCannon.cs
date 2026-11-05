/// <summary>
///     Портировано из ServUO (Scripts/Services/Seasonal Events/RisingTide/MannedCannon.cs — классы
///     <c>MannedCannon</c>, <c>MannedCulverin</c>, <c>MannedCarronade</c>, <c>MannedBlundercannon</c>).
///
///     Пушки, которые обслуживает живой расчёт: в ServUO их ставил на плот «plunder beacon» и сажал
///     за них <c>PirateCrew</c>. Здесь пиратов нет, поэтому пушку может взять любой игрок: двойной
///     клик делает его наводчиком и стреляет по первому чужому кораблю в линии огня. Если наводчика
///     нет, пушка с <see cref="CanFireUnmanned" /> стреляет сама — так вооружён маяк.
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             В ServUO выстрел был растянут таймерами на каждую клетку полёта; здесь попадание
///             разрешается сразу по линии огня — ровно как у пушек этапа 3 (см. <c>Cannon.cs</c>).
///             Результат от этого только детерминированнее.
///         </item>
///         <item>
///             <c>BaseGalleon.CannonIDs</c> используется как таблица артов; своя копия не заводится.
///         </item>
///         <item>
///             Урон по кораблю делится на семь, как в ServUO у расчётных пушек (<c>damage /= 7</c>):
///             это не палубное орудие, а средство обороны плота.
///         </item>
///         <item>
///             <c>SpellHelper.AcquireIndirectTargets</c> для картечи не портирован: у расчётной пушки
///             всегда ядро, картечи у неё в ServUO тоже не было.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Items;
using Server.Multis;

namespace Server.Engines.HighSeas;

/// <summary>Пушка с расчётом (в ServUO — <c>MannedCannon</c>).</summary>
[SerializationGenerator(0, false)]
public abstract partial class MannedCannon : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _operator;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Direction _facing;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _canFireUnmanned;

    protected MannedCannon(Mobile oper, Direction facing) : base(0)
    {
        ItemID = GetItemID(facing, Power);
        _operator = oper;
        _facing = facing;
        Movable = false;
    }

    /// <summary>Класс пушки (в ServUO — <c>Power</c>).</summary>
    public abstract CannonPower Power { get; }

    /// <summary>Дальность в клетках (в ServUO — <c>Range</c>).</summary>
    public abstract int Range { get; }

    public virtual AmmunitionType AmmoType => AmmunitionType.Cannonball;

    public override bool ForceShowProperties => true;

    private static int GetItemID(Direction facing, CannonPower power) =>
        BaseGalleon.CannonIDs[BaseGalleon.GetValueForDirection(facing)][(int)power];

    /// <summary>Наводит пушку на игрока: он же становится расчётом.</summary>
    public bool TryTakeAim(Mobile from)
    {
        if (Deleted || from?.Deleted != false)
        {
            return false;
        }

        if (_operator != null && _operator != from && _operator.Alive && !_operator.Deleted)
        {
            from.SendMessage(0x22, "Пушка уже занята расчётом.");
            return false;
        }

        if (_operator != from)
        {
            _operator = from;
            from.SendMessage(0x22, "Вы встали к пушке — двойной клик стреляет.");
        }

        return true;
    }

    /// <summary>Ищет цель в линии огня и стреляет (в ServUO — <c>Scan(true)</c>).</summary>
    public bool Scan(bool shoot)
    {
        var target = AcquireTarget();

        if (target?.Deleted != false)
        {
            return false;
        }

        if (!shoot)
        {
            return true;
        }

        DoShootEffects();
        OnShipHit(target);

        return true;
    }

    /// <summary>Первый чужой корабль на линии ствола (в ServUO — <c>AcquireTarget</c>).</summary>
    public BaseGalleon AcquireTarget()
    {
        var map = Map;

        if (map == null || map == Map.Internal || Deleted)
        {
            return null;
        }

        var (stepX, stepY) = _facing switch
        {
            Direction.North => (0, -1),
            Direction.South => (0, 1),
            Direction.West  => (-1, 0),
            Direction.East  => (1, 0),
            _               => (0, 0)
        };

        if (stepX == 0 && stepY == 0)
        {
            return null;
        }

        var origin = Location;
        var lateral = 1;

        for (var range = 1; range <= Range; range++)
        {
            if (range % 2 == 0)
            {
                lateral++;
            }

            for (var offset = -lateral; offset <= lateral; offset++)
            {
                var point = stepX == 0
                    ? new Point3D(origin.X + offset, origin.Y + stepY * range, origin.Z)
                    : new Point3D(origin.X + stepX * range, origin.Y + offset, origin.Z);

                var target = BaseGalleon.FindGalleonAt(point, map);

                if (target != null && target.DamageTaken < DamageLevel.Severely)
                {
                    return target;
                }
            }
        }

        return null;
    }

    /// <summary>Попадание ядром по кораблю (в ServUO — <c>OnShipHit</c>).</summary>
    public virtual void OnShipHit(BaseGalleon target)
    {
        var ammo = AmmoInfo.GetAmmoInfo(AmmoType);

        if (ammo == null || target?.Deleted != false)
        {
            return;
        }

        var damage = ammo.GetDamage() / 7;
        var operatorMobile = _operator;

        target.OnTakenDamage(operatorMobile, damage);

        var z = target.ZSurface;
        var hit = new Point3D(target.X, target.Y, z);

        Effects.SendLocationEffect(hit, target.Map, Utility.RandomBool() ? 14000 : 14013, 15, 10);
        Effects.PlaySound(hit, target.Map, 0x207);

        if (operatorMobile?.Deleted == false)
        {
            HarmCrew(target, operatorMobile);
        }
    }

    /// <summary>Вспышка и звук у дула (в ServUO — <c>DoShootEffects</c>).</summary>
    public void DoShootEffects()
    {
        var p = Location;
        var map = Map;

        p.Z -= 3;

        switch (_facing)
        {
            case Direction.North:
                p.Y--;
                break;
            case Direction.East:
                p.X++;
                break;
            case Direction.South:
                p.Y++;
                break;
            case Direction.West:
                p.X--;
                break;
        }

        Effects.SendLocationEffect(p, map, 14120, 15, 10);
        Effects.PlaySound(p, map, 0x664);
    }

    /// <summary>Владелец корабля-цели становится врагом наводчика (в ServUO — <c>DoHarmful</c>).</summary>
    private static void HarmCrew(BaseGalleon target, Mobile from)
    {
        var victim = target.Owner;

        if (victim != null && target.Contains(victim) && from.CanBeHarmful(victim, false))
        {
            from.DoHarmful(victim);
        }
    }

    /// <summary>Выстрел по просьбе игрока: занять место наводчика и выстрелить.</summary>
    public bool TryFire(Mobile from)
    {
        if (!TryTakeAim(from))
        {
            return false;
        }

        if (!Scan(true))
        {
            from.SendMessage(0x22, "В линии огня нет чужого корабля.");
            return false;
        }

        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from == null)
        {
            return;
        }

        // Пушка занята живым расчётом — чужой не стреляет (в ServUO так же запирал Operator).
        if (_operator != null && _operator != from && _operator.Alive && !_operator.Deleted)
        {
            from.SendMessage(0x22, "Пушка уже занята расчётом.");
            return;
        }

        TryFire(from);
    }

    public override void OnAfterDelete()
    {
        _operator = null;

        base.OnAfterDelete();
    }
}

[SerializationGenerator(0, false)]
public partial class MannedCulverin : MannedCannon
{
    [Constructible]
    public MannedCulverin() : this(null, Direction.South)
    {
    }

    public MannedCulverin(Mobile oper, Direction facing) : base(oper, facing)
    {
    }

    public override int Range => 10;

    public override CannonPower Power => CannonPower.Light;
}

[SerializationGenerator(0, false)]
public partial class MannedCarronade : MannedCannon
{
    [Constructible]
    public MannedCarronade() : this(null, Direction.South)
    {
    }

    public MannedCarronade(Mobile oper, Direction facing) : base(oper, facing)
    {
    }

    public override int Range => 10;

    public override CannonPower Power => CannonPower.Heavy;
}

[SerializationGenerator(0, false)]
public partial class MannedBlundercannon : MannedCannon
{
    [Constructible]
    public MannedBlundercannon() : this(null, Direction.South)
    {
    }

    public MannedBlundercannon(Mobile oper, Direction facing) : base(oper, facing)
    {
    }

    public override int LabelNumber => 1158942; // Blundercannon

    public override int Range => 12;

    public override CannonPower Power => CannonPower.Massive;
}
