using System;
using Server.Engines.BuffIcons;
using Server.Items;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BardSpells/
// invigorate.cs) — Provocation mastery: stat buff + periodic self-heal.
// Party-wide sharing dropped (self-only) — see BardSpell.cs.
public class InvigorateSpell : BardSpell
{
    private const string StatModName = "Invigorate";

    private static readonly SpellInfo Info = new("Invigorate", "An Zu", -1, 9002);

    public override double RequiredSkill => 90;
    public override double UpKeep => 5;
    public override int RequiredMana => 22;
    public override SkillName CastSkill => SkillName.Provocation;

    private DateTime _nextHeal;
    private int _hpBonus;
    private int _statBonus;

    public InvigorateSpell(Mobile caster, Item scroll) : base(caster, scroll, Info) =>
        _nextHeal = Core.Now + TimeSpan.FromSeconds(4);

    public override void OnCast()
    {
        if (GetSpell(Caster, GetType()) is { } spell)
        {
            spell.Expire();
            Caster.SendLocalizedMessage(1115774); // You halt your spellsong.
        }
        else if (CheckSequence())
        {
            _statBonus = (int)BaseSkillBonus;
            _hpBonus = (int)(2.5 * BaseSkillBonus);

            Caster.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
            Caster.SendLocalizedMessage(1115737); // You feel invigorated by the bard's spellsong.

            if (Caster is Mobiles.PlayerMobile pm)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.Invigorate, 1115613, 1115730, default, $"{_hpBonus}\t{_statBonus}\t{_statBonus}\t{_statBonus}"));
            }

            Caster.AddStatMod(new StatMod(StatType.Str, StatModName + "str", _statBonus, TimeSpan.Zero));
            Caster.AddStatMod(new StatMod(StatType.Dex, StatModName + "dex", _statBonus, TimeSpan.Zero));
            Caster.AddStatMod(new StatMod(StatType.Int, StatModName + "int", _statBonus, TimeSpan.Zero));

            BeginTimer();
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Invigorate);
        }

        Caster.RemoveStatMod(StatModName + "str");
        Caster.RemoveStatMod(StatModName + "dex");
        Caster.RemoveStatMod(StatModName + "int");
    }

    public override bool OnTick()
    {
        var tick = base.OnTick();

        if (_nextHeal > Core.Now)
        {
            return tick;
        }

        if (Caster.Hits < Caster.HitsMax)
        {
            var healRange = (int)(BaseSkillBonus * 2);
            Caster.Heal(Utility.RandomMinMax(healRange - 2, healRange + 2));
            Caster.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
            Caster.PlaySound(0x1F2);
        }

        _nextHeal = Core.Now + TimeSpan.FromSeconds(4);
        return tick;
    }

    public override int StatBonus() => _hpBonus; // HP Bonus, read from AOS.cs

    public static int GetHPBonus(Mobile m) => GetSpell(m, typeof(InvigorateSpell)) is InvigorateSpell spell ? spell.StatBonus() : 0;
}
