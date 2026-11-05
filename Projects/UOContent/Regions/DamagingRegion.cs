using System;
using System.Text.Json.Serialization;

namespace Server.Regions;

/// <summary>
///     Портировано из ServUO (Scripts/Regions/DamagingRegion.cs) — регион, который периодически наносит
///     урон или накладывает эффект на всех находящихся в нём. На этом базируются кислотная река, ледяная
///     река, поле кристаллов, отравленное дерево, кладбище и вход в Затерянный город.
///
///     Отличие от ServUO: там на каждого мобиля заводился отдельный таймер (с утечками при выходе из
///     региона и загрузке), здесь один таймер региона тикает с заданным интервалом и обходит всех, кто
///     внутри. Поведение то же, но таймер живёт ровно столько, сколько живёт регион.
/// </summary>
public abstract class DamagingRegion : MondainRegion
{
    private TimerExecutionToken _damageToken;

    protected virtual TimeSpan DamageInterval => TimeSpan.FromSeconds(1);
    protected virtual int EnterMessage => 0;
    protected virtual int EnterSound => 0;

    protected DamagingRegion(string name, Map map, int priority, params Rectangle3D[] area)
        : base(name, map, priority, area)
    {
    }

    protected DamagingRegion(string name, Map map, Region parent, params Rectangle3D[] area)
        : base(name, map, parent, area)
    {
    }

    protected DamagingRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area)
        : base(name, map, parent, priority, area)
    {
    }

    public override void OnRegister()
    {
        base.OnRegister();

        _damageToken.Cancel();
        Timer.StartTimer(DamageInterval, DamageInterval, Tick, out _damageToken);
    }

    public override void OnUnregister()
    {
        base.OnUnregister();
        _damageToken.Cancel();
    }

    private void Tick()
    {
        foreach (var m in GetPlayers())
        {
            if (CanDamage(m))
            {
                OnDamage(m);
            }
        }
    }

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);

        if (!CanDamage(m))
        {
            return;
        }

        if (EnterSound > 0)
        {
            m.PlaySound(EnterSound);
        }

        if (EnterMessage > 0)
        {
            m.SendLocalizedMessage(EnterMessage);
        }
    }

    public virtual bool CanDamage(Mobile m) =>
        m.Alive && !m.Blessed && !m.IsDeadBondedPet && Map != null && m.Map == Map && Contains(m.Location);

    protected virtual void OnDamage(Mobile m) => m.RevealingAction();
}
