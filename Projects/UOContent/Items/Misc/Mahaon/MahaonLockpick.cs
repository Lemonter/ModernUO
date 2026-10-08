using System;
using ModernUO.Serialization;
using Server.Systems.MahaonMetals;
using Server.Targeting;

namespace Server.Items;

/// <summary>Отмычка из нового металла — та же логика доступа/скорости, что и обычная
/// Lockpick.cs, но с проверкой тира металла против замка (см. MahaonLockSystem) и
/// переменной скоростью взлома вместо фиксированных 3 секунд. Замок без металла
/// (MahaonMetalTracker.GetMetal возвращает null — обычный не-перекованный ванильный
/// сундук) обрабатывается как раньше, эта отмычка тогда просто ведёт себя как обычная.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonLockpick : Item
{
    [SerializableField(0)]
    private MahaonMetal _metal;

    [Constructible]
    public MahaonLockpick(MahaonMetal metal = MahaonMetal.Iron, int amount = 1) : base(0x14FC)
    {
        _metal = metal;
        Stackable = true;
        Amount = amount;

        var info = MahaonMetalTable.Get(metal);
        Name = $"отмычка ({info.RuName})";
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(502068); // What do you want to pick?
        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private readonly MahaonLockpick _pick;

        public InternalTarget(MahaonLockpick pick) : base(1, false, TargetFlags.None) => _pick = pick;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_pick.Deleted)
            {
                return;
            }

            if (targeted is not ILockpickable lockpickable)
            {
                from.SendLocalizedMessage(501666); // You can't unlock that!
                return;
            }

            if (targeted is not Item item || !lockpickable.Locked)
            {
                from.SendLocalizedMessage(502069); // This does not appear to be locked
                return;
            }

            var lockMetal = MahaonMetalTracker.GetMetal(item);

            if (lockMetal != null && !MahaonLockSystem.CanAttempt(lockMetal.Value, _pick.Metal))
            {
                from.SendMessage(0x22, "Эта отмычка не подходит для замка такого класса.");
                return;
            }

            if (item.RootParent != from)
            {
                from.Direction = from.GetDirectionTo(item);
            }

            from.PlaySound(0x241);

            var fastMatch = lockMetal != null && MahaonLockSystem.IsExactMatch(lockMetal.Value, _pick.Metal);
            var delay = fastMatch ? TimeSpan.FromSeconds(1.0) : TimeSpan.FromSeconds(3.0);

            new InternalTimer(from, lockpickable, _pick, delay).Start();
        }

        // Mahaon: self-repeating now — see LockPick.cs's own copy of this same change for
        // the full reasoning (mirrors Engines/Harvest/Core/HarvestTimer's auto-retry
        // pattern). Kept the variable delay (fast-match vs normal) as both the initial AND
        // repeat interval.
        private class InternalTimer : Timer
        {
            private readonly Mobile _from;
            private readonly ILockpickable _item;
            private readonly MahaonLockpick _pick;

            public InternalTimer(Mobile from, ILockpickable item, MahaonLockpick pick, TimeSpan delay)
                : base(delay, delay)
            {
                _from = from;
                _item = item;
                _pick = pick;
            }

            private bool BrokeLockPickTest()
            {
                if (Utility.Random(4) != 0)
                {
                    return false;
                }

                var item = (Item)_item;
                item.SendLocalizedMessageTo(_from, 502074); // You broke the lockpick.
                _from.PlaySound(0x3A4);
                _pick.Consume();

                return true;
            }

            protected override void OnTick()
            {
                if (_pick.Deleted || !_pick.IsChildOf(_from.Backpack))
                {
                    Stop();
                    return;
                }

                var item = (Item)_item;

                if (item.Deleted || !_item.Locked)
                {
                    Stop();
                    return;
                }

                if (!_from.InRange(item.GetWorldLocation(), 1))
                {
                    Stop();
                    return;
                }

                if (_item.LockLevel is ILockpickable.CannotPick or ILockpickable.MagicLock)
                {
                    item.SendLocalizedMessageTo(_from, 502073); // This lock cannot be picked by normal means
                    Stop();
                    return;
                }

                // Was a hard gate — see LockPick.cs's own copy of this same change for why
                // it's gone (attemptable from 0 skill now, minSkill 0 lets the chance
                // formula itself express "low skill = low odds" instead).
                if (_from.CheckTargetSkill(SkillName.Lockpicking, _item, 0, _item.MaxLockLevel))
                {
                    item.SendLocalizedMessageTo(_from, 502076); // The lock quickly yields to your skill.
                    _from.PlaySound(0x4A);
                    _item.LockPick(_from);
                    Stop();
                }
                else
                {
                    item.SendLocalizedMessageTo(_from, 502075); // You are unable to pick the lock.

                    if (BrokeLockPickTest())
                    {
                        Stop();
                    }
                }
            }
        }
    }
}
