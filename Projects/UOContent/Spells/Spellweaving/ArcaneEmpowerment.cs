using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    /// <summary>Spellweaving 615. Ported from ServUO (Scripts/Spells/Spellweaving/ArcaneEmpowerment.cs).
    /// Registration was already in Spells/Initializer.cs, commented out, with an
    /// ArcaneEmpowermentScroll shipping in SpellweavingScrolls.cs and BuffIcon.ArcaneEmpowerment
    /// already in the enum — everything around the spell existed except the spell.
    ///
    /// This one is only worth having if the rest of the game reads it: on its own it's a buff
    /// icon and nothing else, which is exactly the failure mode recorded as #39 in
    /// code-audit-findings.md ("half the mastery roster is mechanically dead, only the buff
    /// icon works"). So the three call sites ServUO wires are wired here too:
    ///   - Spell.GetNewAosDamage  -> GetSpellBonus, next to the ReaperFormSpell bonus that
    ///     already sits there for the same reason
    ///   - HealSpell / GreaterHealSpell -> AddHealBonus
    ///   - DispelSpell -> GetDispellBonus
    ///
    /// Table-and-timer shape follows EssenceOfWind.cs, the established pattern in this
    /// folder. In-memory only, like every other timed buff here: the effect lasts well under
    /// a minute, so losing it to a restart costs the player nothing they can exploit.</summary>
    public class ArcaneEmpowermentSpell : ArcanistSpell
    {
        private static readonly SpellInfo _info = new("Arcane Empowerment", "Aslavdra", -1);

        private static readonly Dictionary<Mobile, ArcaneEmpowermentTimer> _table = new();

        public ArcaneEmpowermentSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(3.0);

        public override double RequiredSkill => 24.0;
        public override int RequiredMana => 50;

        public override void OnCast()
        {
            if (CheckSequence())
            {
                var caster = Caster;
                var skill = caster.Skills.Spellweaving.Value;
                var focus = FocusLevel;

                var bonus = (int)(skill / 12) + focus * 5;
                var duration = TimeSpan.FromSeconds(15 + (int)(skill / 24) + focus * 2);

                RemoveBonus(caster);

                caster.PlaySound(0x5C1);
                caster.FixedParticles(0x375A, 1, 30, 9966, 33, 2, EffectLayer.Head);

                var timer = new ArcaneEmpowermentTimer(caster, bonus, focus, duration);
                timer.Start();

                _table[caster] = timer;

                (caster as PlayerMobile)?.AddBuff(
                    new BuffInfo(BuffIcon.ArcaneEmpowerment, 1031616, 1075808, duration, $"{bonus}")
                );
            }

            FinishSequence();
        }

        /// <summary>Spell damage bonus, in percent. The focus level is added on top in PvP —
        /// the empowerment is the one spell-damage source deliberately not capped there.</summary>
        public static int GetSpellBonus(Mobile m, bool playerVsPlayer)
        {
            if (!_table.TryGetValue(m, out var timer))
            {
                return 0;
            }

            return playerVsPlayer ? timer._bonus + timer._focus : timer._bonus;
        }

        public static double GetDispellBonus(Mobile m) =>
            _table.TryGetValue(m, out var timer) ? 10.0 * timer._focus : 0.0;

        public static void AddHealBonus(Mobile m, ref int toHeal)
        {
            if (_table.TryGetValue(m, out var timer))
            {
                toHeal += (int)(toHeal * ((10 + timer._bonus) / 100.0));
            }
        }

        public static bool IsUnderEffects(Mobile m) => _table.ContainsKey(m);

        public static void RemoveBonus(Mobile m)
        {
            if (_table.TryGetValue(m, out var timer))
            {
                timer.DoExpire();
            }
        }

        private class ArcaneEmpowermentTimer : Timer
        {
            internal readonly int _bonus;
            internal readonly int _focus;
            private readonly Mobile _owner;

            internal ArcaneEmpowermentTimer(Mobile owner, int bonus, int focus, TimeSpan duration) : base(duration)
            {
                _owner = owner;
                _bonus = bonus;
                _focus = focus;
            }

            protected override void OnTick()
            {
                DoExpire();
            }

            internal void DoExpire()
            {
                Stop();
                _table.Remove(_owner);

                _owner.PlaySound(0x5C2);

                (_owner as PlayerMobile)?.RemoveBuff(BuffIcon.ArcaneEmpowerment);
            }
        }
    }
}
