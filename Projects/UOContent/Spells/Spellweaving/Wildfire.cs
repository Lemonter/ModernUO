using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.Mobiles;
using Server.Multis;
using Server.Spells.SkillMasteries;

namespace Server.Spells.Spellweaving
{
    /// <summary>Spellweaving 609. Ported from ServUO (Scripts/Spells/Spellweaving/Wildfire.cs).
    /// The registration was already present in Spells/Initializer.cs, commented out, and a
    /// WildfireScroll has always existed in SpellweavingScrolls.cs — the school shipped with
    /// the scroll and the book slot but no spell behind them.
    ///
    /// One adaptation: ServUO's AcquireIndirectTargets doesn't exist here, so target
    /// gathering uses the idiom the rest of this folder uses (see EssenceOfWind.cs) —
    /// GetMobilesInRange plus SpellHelper.ValidIndirectTarget / CanBeHarmful, drained through
    /// a PooledRefQueue so the map enumerator isn't held while damage mutates it.
    ///
    /// Everything else is the original: tile spacing 5 + focus, the 3x3 ring of flame graphics
    /// with the centre skipped, the duration and damage formulas, the Spell Damage Increase
    /// bonus capped at 15% when the victim is a player, the split across up to three targets,
    /// the 1-second tick, the per-target cooldown that stops two overlapping casts from
    /// double-dipping, the house exclusion, DamageType.SpellAOE, and sound 0x5CF at both the
    /// epicentre and each victim.</summary>
    public class WildfireSpell : ArcanistSpell, ITargetingSpell<IPoint3D>
    {
        private static readonly SpellInfo _info = new("Wildfire", "Haelyn", -1);

        // Guards against two Wildfires over the same ground damaging a mobile twice in one
        // second. Entries are stamped with Core.TickCount and are only ever read/written on
        // the game loop, so a plain static dictionary is safe here.
        private static readonly Dictionary<Mobile, long> _cooldown = new();

        public WildfireSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.5);

        public override double RequiredSkill => 66.0;
        public override int RequiredMana => 50;

        public int TargetRange => 12;

        public void Target(IPoint3D p)
        {
            if (!Caster.CanSee(p) || !SpellHelper.CheckTown(p, Caster) || !CheckSequence())
            {
                FinishSequence();
                return;
            }

            SpellHelper.Turn(Caster, p);
            SpellHelper.GetSurfaceTop(ref p);

            var loc = new Point3D(p);
            var map = Caster.Map;

            var focus = FocusLevel;
            var tiles = 5 + focus;
            var skillTerm = Math.Max(1, (int)(Caster.Skills.Spellweaving.Value / 24));
            var duration = skillTerm + focus;
            var damage = 10 + skillTerm + focus;

            Effects.PlaySound(loc, map, 0x5CF);

            // Eight flame graphics on a ring `tiles` out from the epicentre, centre skipped.
            for (var x = loc.X - tiles; x <= loc.X + tiles; x += tiles)
            {
                for (var y = loc.Y - tiles; y <= loc.Y + tiles; y += tiles)
                {
                    if (x == loc.X && y == loc.Y)
                    {
                        continue;
                    }

                    new WildfireFlame(duration).MoveToWorld(new Point3D(x, y, loc.Z), map);
                }
            }

            new WildfireTimer(Caster, loc, map, tiles, damage, duration).Start();

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);
        }

        private class WildfireTimer : Timer
        {
            private readonly Mobile _caster;
            private readonly int _damage;
            private readonly Point3D _location;
            private readonly Map _map;
            private readonly int _range;
            private int _ticks;

            internal WildfireTimer(Mobile caster, Point3D location, Map map, int range, int damage, int ticks)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {

                _caster = caster;
                _location = location;
                _map = map;
                _range = range;
                _damage = damage;
                _ticks = ticks;
            }

            protected override void OnTick()
            {
                if (--_ticks < 0 || _map == null || _caster.Deleted)
                {
                    Stop();
                    return;
                }

                var now = Core.TickCount;

                using var queue = PooledRefQueue<Mobile>.Create();
                foreach (var m in _map.GetMobilesInRange(_location, _range))
                {
                    if (m == _caster || !SpellHelper.ValidIndirectTarget(_caster, m) ||
                        !_caster.CanBeHarmful(m, false) || BaseHouse.FindHouseAt(m) != null)
                    {
                        continue;
                    }

                    if (_cooldown.TryGetValue(m, out var next) && next > now)
                    {
                        continue;
                    }

                    queue.Enqueue(m);
                }

                var count = queue.Count;

                if (count == 0)
                {
                    return;
                }

                var sdiBonus = AosAttributes.GetValue(_caster, AosAttribute.SpellDamage) / 100.0;

                while (queue.Count > 0)
                {
                    var m = queue.Dequeue();

                    _cooldown[m] = now + 1000;

                    // The 15% cap only applies when the victim is a player.
                    var bonus = m is PlayerMobile && sdiBonus > 0.15 ? 0.15 : sdiBonus;

                    var damage = _damage + (int)(_damage * bonus);

                    if (count > 1)
                    {
                        damage /= Math.Min(3, count);
                    }

                    _caster.DoHarmful(m);
                    Effects.PlaySound(m.Location, _map, 0x5CF);
                    AOS.Damage(m, _caster, damage, 0, 100, 0, 0, 0, 0, DamageType.SpellAOE);
                }
            }
        }
    }

    /// <summary>The flame graphic Wildfire leaves on the ground. Carries no damage of its own
    /// (that's WildfireTimer's job, same split as ServUO) — it only needs to look right and
    /// clean itself up. Serializable because a world save can land inside its lifetime; on
    /// load it deletes rather than trying to resume, since the damage timer behind it is gone
    /// and a flame that burns without hurting anyone would be a lie.</summary>
    [SerializationGenerator(0, false)]
    public partial class WildfireFlame : Item
    {
        public WildfireFlame(int duration) : base(Utility.RandomBool() ? 0x398C : 0x3996)
        {
            Movable = false;
            Timer.DelayCall(TimeSpan.FromSeconds(duration), Delete);
        }

        [AfterDeserialization]
        private void AfterDeserialization()
        {
            Delete();
        }
    }
}
