using Server.Targeting;

namespace Server.Spells.SkillMasteries;

/// <summary>Shared single-target helper for the mastery spells that resolve via
/// Caster.Target = new MasteryTarget(this, ...) — real ServUO shape, same idea as this
/// codebase's own per-item Target subclasses used throughout this session, just generic
/// over any SkillMasterySpell via the protected OnTarget hook.</summary>
public class MasteryTarget : Target
{
    private readonly SkillMasterySpell _spell;

    public MasteryTarget(SkillMasterySpell spell, int range = 10, bool allowGround = false, TargetFlags flags = TargetFlags.Harmful)
        : base(range, allowGround, flags) =>
        _spell = spell;

    protected override void OnTarget(Mobile from, object targeted) => _spell.InvokeOnTarget(targeted);
}
