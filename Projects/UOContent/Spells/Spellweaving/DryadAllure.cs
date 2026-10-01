using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    /// <summary>Spellweaving 611. Ported from ServUO (Scripts/Spells/Spellweaving/DryadAllure.cs).
    /// Registration was already sitting commented out in Spells/Initializer.cs next to an
    /// existing DryadAllureScroll — scroll and book slot shipped, spell didn't.
    ///
    /// Charms a humanoid into a permanent pet costing 3 control slots. There is no expiry in
    /// the original and none here: that's deliberate, and it's what makes the spell safe
    /// across restarts. An allured creature is an ordinary Controlled pet from the moment it
    /// turns, so a timed charm would need its own persisted registry or a server restart mid
    /// charm would hand the player the creature forever — the exploit shape flagged as #11
    /// and #33 in code-audit-findings.md. Permanent-by-design sidesteps it entirely.
    ///
    /// BaseCreature had neither the `Allured` flag nor `AllureImmune`; both were added for
    /// this (serialization version 21), so the rejection rules match the original — an
    /// already-controlled creature is refused unless it is already allured, and a creature
    /// can opt out of the spell entirely by overriding AllureImmune.</summary>
    public class DryadAllureSpell : ArcanistSpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new("Dryad Allure", "Rathril", -1);

        public DryadAllureSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(3.0);

        public override double RequiredSkill => 52.0;
        public override int RequiredMana => 40;

        public int TargetRange => 12;

        public void Target(Mobile m)
        {
            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
                FinishSequence();
                return;
            }

            if (m is not BaseCreature creature || creature.IsParagon || creature.Summoned ||
                creature.AllureImmune || !IsHumanoid(creature))
            {
                Caster.SendLocalizedMessage(1074379); // You cannot charm that!
                FinishSequence();
                return;
            }

            if (creature.Controlled && !creature.Allured)
            {
                Caster.SendLocalizedMessage(1074380); // This humanoid is already controlled by someone else.
                FinishSequence();
                return;
            }

            if (!CheckSequence())
            {
                FinishSequence();
                return;
            }

            SpellHelper.Turn(Caster, creature);

            var chance = Caster.Skills.Spellweaving.Value / 150.0 + FocusLevel / 50.0;

            if (chance <= Utility.RandomDouble())
            {
                Caster.PlaySound(0x5C5);
                // The humanoid becomes enraged by your charming attempt and attacks you.
                Caster.SendLocalizedMessage(1074378);

                creature.Combatant = Caster;
                FinishSequence();
                return;
            }

            // Set before SetControlMaster so its follower-cap check counts the real cost.
            creature.ControlSlots = 3;

            if (!creature.SetControlMaster(Caster))
            {
                // SetControlMaster already sent 1049607 (too many followers) and changed
                // nothing, so put the slot cost back rather than leaving it inflated on a
                // creature that stayed wild.
                creature.ControlSlots = 1;
                FinishSequence();
                return;
            }

            creature.Combatant = null;
            creature.Warmode = false;
            creature.ControlTarget = Caster;
            creature.ControlOrder = OrderType.Follow;
            creature.Allured = true;

            // The original strips the creature's inventory so a charmed humanoid can't be
            // farmed for the gear it spawned with.
            var pack = creature.Backpack;

            if (pack != null)
            {
                for (var i = pack.Items.Count - 1; i >= 0; i--)
                {
                    pack.Items[i].Delete();
                }
            }

            Caster.PlaySound(0x5C4);
            Caster.SendLocalizedMessage(1074377); // You allure the humanoid to follow and protect you.

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this);
        }

        // ServUO tests the creature against the Repond slayer group — "humanoid" in the
        // spell's own wording. Same test here, through the existing slayer tables.
        private static bool IsHumanoid(Mobile m) =>
            SlayerGroup.GetEntryByName(SlayerName.Repond)?.Slays(m) == true;
    }
}
