using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Necromancy;

public class VengefulSpiritSpell : NecromancerSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Vengeful Spirit",
        "Kal Xen Bal Beh",
        203,
        9031,
        Reagent.BatWing,
        Reagent.GraveDust,
        Reagent.PigIron
    );

    // Mahaon: vanilla allowed exactly one revenant (enforced only by its 3-slot follower
    // cost). Школа призыва now grants one more per 30 mastery, which needs a real
    // head-count of its own. Revenants are never serialized (SkipSerialization), so this
    // is safe to keep purely in memory — a restart legitimately clears it.
    private static readonly Dictionary<Mobile, List<Revenant>> _table = new();

    public VengefulSpiritSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public static int CountRevenants(Mobile caster)
    {
        if (caster == null || !_table.TryGetValue(caster, out var list))
        {
            return 0;
        }

        for (var i = list.Count - 1; i >= 0; --i)
        {
            if (list[i]?.Deleted != false)
            {
                list.RemoveAt(i);
            }
        }

        if (list.Count == 0)
        {
            _table.Remove(caster);
            return 0;
        }

        return list.Count;
    }

    private static void Register(Mobile caster, Revenant rev)
    {
        if (caster == null || rev == null)
        {
            return;
        }

        if (!_table.TryGetValue(caster, out var list))
        {
            _table[caster] = list = new List<Revenant>();
        }

        list.Add(rev);
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.0);

    public override double RequiredSkill => 80.0;
    public override int RequiredMana => 41;

    public void Target(Mobile m)
    {
        if (m == null)
        {
            return;
        }

        if (Caster == m)
        {
            Caster.SendLocalizedMessage(1061832); // You cannot exact vengeance on yourself.
        }
        else if (CheckHSequence(m))
        {
            SpellHelper.Turn(Caster, m);

            /* Summons a Revenant which haunts the target until either the target or the Revenant is dead.
             * Revenants have the ability to track down their targets wherever they may travel.
             * A Revenant's strength is determined by the Necromancy and Spirit Speak skills of the Caster.
             * The effect lasts for ((Spirit Speak skill level * 80) / 120) + 10 seconds.
             */

            var duration = TimeSpan.FromSeconds(GetDamageSkill(Caster) * 80 / 120 + 10);

            var rev = new Revenant(Caster, m, duration);

            // Mahaon: Школа призыва raises the revenant's stats and damage on top of the
            // SpiritSpeak scaling its constructor already does.
            Systems.MahaonCombat.NecromancySummonSystem.ApplyMasteryPower(Caster, rev);

            if (BaseCreature.Summon(
                    rev,
                    false,
                    Caster,
                    m.Location,
                    0x81,
                    TimeSpan.FromSeconds(duration.TotalSeconds + 2.0)
                ))
            {
                rev.FixedParticles(0x373A, 1, 15, 9909, EffectLayer.Waist);
                Register(Caster, rev);

                Systems.MahaonCombat.NecromancySummonSystem.AnnounceSummon(
                    Caster,
                    rev,
                    CountRevenants(Caster),
                    Systems.MahaonCombat.NecromancySummonSystem.GetVengefulLimit(Caster)
                );
            }
        }
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        var limit = Systems.MahaonCombat.NecromancySummonSystem.GetVengefulLimit(Caster);

        if (CountRevenants(Caster) >= limit)
        {
            Caster.SendMessage(0x3B2, $"Больше мстительных духов тебе не удержать ({limit}).");
            return false;
        }

        if (Caster.Followers + 1 > Caster.FollowersMax)
        {
            Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
            return false;
        }

        return true;
    }
}
