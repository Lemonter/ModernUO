using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Pierce.cs) —
// Fencing mastery on-hit move: drains the defender's stamina over 10 seconds.
public class PierceMove : SkillMasteryMove
{
    public override int BaseMana => 20;
    public override double RequiredSkill => 90.0;

    public override SkillName MoveSkill => SkillName.Fencing;
    public override TextDefinition AbilityMessage => 1155991; // You ready yourself to pierce your opponent!

    private Dictionary<Mobile, Timer> _table;

    public override bool Validate(Mobile from)
    {
        if (!CheckWeapon(from))
        {
            from.SendLocalizedMessage(1156005); // You must have a fencing weapon equipped to use this ability.
            return false;
        }

        return base.Validate(from);
    }

    public override void OnUse(Mobile from)
    {
        if (from.Player)
        {
            from.PlaySound(from.Female ? 0x338 : 0x44A);
        }
        else if (from is BaseCreature bc)
        {
            from.PlaySound(bc.GetAngerSound());
        }

        from.FixedParticles(0x376A, 1, 31, 9961, 1160, 0, EffectLayer.Waist);
    }

    public override void OnHit(Mobile attacker, Mobile defender, int damage)
    {
        if (!Validate(attacker) || !CheckMana(attacker, true))
        {
            return;
        }

        ClearCurrentMove(attacker);

        if (attacker.Weapon is not BaseWeapon || (_table != null && _table.ContainsKey(attacker)))
        {
            attacker.SendLocalizedMessage(1095215); // Your target is already under the effect of this attack.
            return;
        }

        var toDrain = (int)(attacker.Skills[MoveSkill].Value + attacker.Skills[SkillName.Tactics].Value + MasteryInfo.GetMasteryLevel(attacker, SkillName.Fencing) * 40 / 3);
        toDrain /= 3;

        _table ??= new Dictionary<Mobile, Timer>();
        var t = new InternalTimer(this, attacker, defender, toDrain);
        _table[attacker] = t;
        t.Start();

        attacker.PrivateOverheadMessage(MessageType.Regular, 1150, 1155993, attacker.NetState); // You deliver a piercing blow!
        defender.FixedEffect(0x36BD, 20, 10, 2725, 5);

        var drain = (int)(defender.StamMax * (toDrain / 100.0));

        if (defender is PlayerMobile pm)
        {
            // -~1_VAL~ Stamina Regeneration.
            pm.AddBuff(new BuffInfo(BuffIcon.Pierce, 1155994, 1155995, TimeSpan.FromSeconds(10), (drain / 7).ToString()));
        }
    }

    public void RemoveEffects(Mobile attacker)
    {
        if (_table != null && _table.TryGetValue(attacker, out var timer))
        {
            timer.Stop();
            _table.Remove(attacker);
        }
    }

    private class InternalTimer : Timer
    {
        private readonly PierceMove _move;
        private readonly Mobile _attacker;
        private readonly Mobile _defender;
        private readonly int _toDrain;
        private int _ticks;

        public InternalTimer(PierceMove move, Mobile attacker, Mobile defender, int toDrain) : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
        {
            _move = move;
            _attacker = attacker;
            _defender = defender;
            _toDrain = toDrain;
        }

        protected override void OnTick()
        {
            if (!_defender.Alive || _ticks >= 10)
            {
                _move.RemoveEffects(_attacker);
                return;
            }

            _ticks++;
            _defender.Stam = Math.Max(0, _defender.Stam - (_toDrain / 10 + Utility.RandomMinMax(-2, 2)));
        }
    }
}
