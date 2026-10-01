namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BardSpells/
// BardSpell.cs) — shared base for the 6 Provocation/Peacemaking/Discordance mastery
// "spellsongs". CollectiveBonus (a real party-size-scaled bonus in ServUO) is pinned to 0
// here since party-wide sharing is dropped across this whole port (see
// SkillMasterySpell.cs); GetSlayerBonus (a slayer-weapon damage multiplier against the
// target) is pinned to 1.0 — a real slayer-type lookup could be added later without
// touching any of the 6 concrete spells, they all already call through this one hook.
public abstract class BardSpell : SkillMasterySpell
{
    protected BardSpell(Mobile caster, Item scroll, SpellInfo info) : base(caster, scroll, info)
    {
    }

    // CollectiveBonus is already 0 on the base SkillMasterySpell — nothing to add here.
    protected double GetSlayerBonus() => 1.0;
}
