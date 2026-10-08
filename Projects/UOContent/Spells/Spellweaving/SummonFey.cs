using System;
using Server.Mobiles;

namespace Server.Spells.Spellweaving
{
    public class SummonFeySpell : ArcaneSummon<ArcaneFey>
    {
        private static readonly SpellInfo _info = new(
            "Summon Fey",
            "Alalithra",
            -1
        );

        public SummonFeySpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 38.0;
        public override int RequiredMana => 10;

        public override int Sound => 0x217;

        // Mahaon: removed the "haven't forged a friendship with the fey"
        // (MLQuestSystem.GetContext(mobile).SummonFey) quest gate per the shard owner's
        // ask — CheckSequence override no longer needed at all, base handles it.
    }
}
