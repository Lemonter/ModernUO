using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/CommandUndead.cs) —
// Necromancy mastery: temporarily commands an undead creature as a 2-slot follower.
// Simplifications: `BaseRenowned` and `BaseInstrument.GetBaseDifficulty` don't exist in
// this codebase — the renowned-creature exclusion is dropped, and difficulty is
// approximated from HitsMax instead (a real, if rougher, "harder to command = tougher
// creature" proxy). ServUO's Doom-specific `_NoCommandTypes` list (unique Doom bosses) and
// the SkeletalDragon/BellOfTheDead quest-item cleanup are dropped — none of that content
// exists here either.
public class CommandUndeadSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new(
        "Command Undead", "In Corp Xen Por", 204, 9061,
        typeof(DaemonBlood), typeof(PigIron), typeof(BatWing)
    );

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 40;
    public override bool PartyEffects => false;

    public override SkillName CastSkill => SkillName.Necromancy;
    public override SkillName DamageSkill => SkillName.SpiritSpeak;

    public override System.TimeSpan CastDelayBase => System.TimeSpan.FromSeconds(3.0);

    public CommandUndeadSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void OnCast() => Caster.Target = new MasteryTarget(this);

    protected override void OnTarget(object o)
    {
        if (o is not BaseCreature bc || !Caster.CanSee(bc.Location) || !Caster.InLOS(bc))
        {
            Caster.SendLocalizedMessage(500237); // Target can not be seen.
            return;
        }

        if (!ValidateTarget(bc))
        {
            Caster.SendLocalizedMessage(1156015); // You cannot command that!
            return;
        }

        if (Caster.Followers + 2 > Caster.FollowersMax)
        {
            Caster.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
            return;
        }

        if (bc.Controlled || bc.Summoned)
        {
            Caster.SendLocalizedMessage(1156015); // You cannot command that!
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        var difficulty = System.Math.Max(25, bc.HitsMax / 5.0);
        var skill = (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 2 + GetMasteryLevel() * 3 + 1;

        var chance = (skill - (difficulty - 25)) / ((difficulty + 25) - (difficulty - 25));

        if (chance < Utility.RandomDouble())
        {
            Caster.SendLocalizedMessage(1156014); // The undead becomes enraged by your command attempt and attacks you.
            return;
        }

        bc.ControlSlots = 2;
        bc.Combatant = null;

        if (Caster.Combatant == bc)
        {
            Caster.Combatant = null;
            Caster.Warmode = false;
        }

        if (!bc.SetControlMaster(Caster))
        {
            return;
        }

        _commanded.Add(bc);

        bc.PlaySound(0x5C4);

        var pack = bc.Backpack;

        if (pack != null)
        {
            for (var i = pack.Items.Count - 1; i >= 0; --i)
            {
                if (i < pack.Items.Count)
                {
                    pack.Items[i].Delete();
                }
            }
        }

        Caster.PlaySound(0x5C4);
        Caster.SendLocalizedMessage(1156013); // You command the undead to follow and protect you.
    }

    // Was: MasteryInfo.OnMasteryChanged used ValidateTarget (a pure "eligible TYPE to
    // command" check) to decide which of the caster's AllFollowers to release on a
    // mastery switch — that matches any undead follower gained through ANY means (a real
    // Necromancy Animate Dead pet, say), not just ones actually borrowed through this
    // spell. Tracked here instead, scoped to what this spell itself put under control.
    private static readonly HashSet<BaseCreature> _commanded = new();

    public static bool WasCommandedByThisSpell(BaseCreature bc) => _commanded.Contains(bc);

    public static void ClearCommanded(BaseCreature bc) => _commanded.Remove(bc);

    public static bool ValidateTarget(BaseCreature bc)
    {
        if (bc is BaseChampion or Engines.Shadowguard.ShadowguardBoss)
        {
            return false;
        }

        if (bc is SkeletalDragon)
        {
            return true;
        }

        var entry = SlayerGroup.GetEntryByName(SlayerName.Silver);
        return entry != null && entry.Slays(bc);
    }
}
