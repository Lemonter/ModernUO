using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.BuffIcons;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/CombatTraining.cs) —
// AnimalTaming mastery: trains one pet into a chosen combat role (Empowerment/Berserk/
// ConsumeDamage/AsOne), each with its own damage-pipeline hook (CheckDamage/OnCreatureHit/
// RegenBonus/GetHitChanceBonus — all called from the shared SkillMasterySpell.OnDamage/
// OnHit static dispatchers). ServUO's DespiseCreature exclusion dropped (dungeon-specific
// pet type, doesn't exist here).
public enum TrainingType
{
    Empowerment,
    Berserk,
    ConsumeDamage,
    AsOne
}

public class CombatTrainingSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Combat Training", "", -1, 9002);

    public override double UpKeep
    {
        get
        {
            var taming = Caster.Skills[CastSkill].Base;
            var lore = Caster.Skills[SkillName.AnimalLore].Base;
            var asOne = SpellType == TrainingType.AsOne;

            var skillValue = taming + lore / 2;
            var masteryBase = skillValue switch
            {
                >= 180 => 6,
                >= 165 => 8,
                >= 150 => 10,
                _      => 12
            };

            return asOne ? masteryBase * 2 : masteryBase;
        }
    }

    public override double RequiredSkill => 90;
    public override int RequiredMana => 40;
    public override bool PartyEffects => false;
    public override SkillName CastSkill => SkillName.AnimalTaming;

    public TrainingType SpellType { get; set; }

    public int Phase { get; set; }
    public int DamageTaken { get; set; }
    private bool _expired;

    public CombatTrainingSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (Caster is PlayerMobile { AllFollowers.Count: 0 })
        {
            Caster.SendLocalizedMessage(1156112); // This ability requires you to have pets.
            return false;
        }

        if (GetSpell(Caster, typeof(CombatTrainingSpell)) is { } spell)
        {
            spell.Expire();
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast() => Caster.Target = new InternalTarget(this);

    public void OnSelected(TrainingType type, Mobile target)
    {
        if (!CheckSequence() ||
            (type == TrainingType.AsOne && Caster is PlayerMobile pm && !pm.AllFollowers.Any(mob => mob != target)))
        {
            FinishSequence();
            return;
        }

        SpellType = type;
        Target = target;
        Phase = 0;

        BeginTimer();

        Target.FixedParticles(0x373A, 10, 80, 5018, 0, 0, EffectLayer.Waist);

        if (Caster is PlayerMobile casterPm)
        {
            // You train ~2_NAME~ to use ~1_SKILLNAME~. Mana Upkeep: ~3_COST~
            casterPm.AddBuff(new BuffInfo(BuffIcon.CombatTraining, 1155933, 1156107, default, $"{SpellType}\t{Target.Name}\t{ScaleUpkeep()}"));
        }

        FinishSequence();
    }

    public override void EndEffects()
    {
        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.CombatTraining);
        }

        Caster.SendSound(0x1ED);
        _expired = true;
    }

    protected override void DoEffects()
    {
        Caster.FixedParticles(0x376A, 10, 30, 5052, 1261, 0, EffectLayer.LeftFoot, 0);
        Caster.DisruptiveAction();
    }

    public override bool OnTick()
    {
        if (Target == null || Target.IsDeadBondedPet)
        {
            Expire();
            return false;
        }

        return base.OnTick();
    }

    public double DamageMod
    {
        get
        {
            if (Target == null || SpellType == TrainingType.AsOne)
            {
                return 0.0;
            }

            return Math.Min(1.0, DamageTaken / (Target.HitsMax * .66));
        }
    }

    private void EndPhase1()
    {
        if (_expired)
        {
            return;
        }

        Phase = 2;
        Server.Timer.DelayCall(TimeSpan.FromSeconds(SpellType == TrainingType.Berserk ? 8 : 10), EndPhase2);
    }

    private void EndPhase2()
    {
        if (_expired)
        {
            return;
        }

        DamageTaken = 0;
        Phase = 0;

        if (SpellType == TrainingType.Berserk)
        {
            AddRageCooldown(Target);
        }
    }

    public static void CheckDamage(Mobile attacker, Mobile defender, DamageType type, ref int damage)
    {
        if (defender is BaseCreature { Controlled: true } or BaseCreature { Summoned: true })
        {
            var spell = EnumerateAllSpells().FirstOrDefault(sp => sp is CombatTrainingSpell && sp.Target == defender) as CombatTrainingSpell;

            if (spell == null)
            {
                return;
            }

            var storedDamage = damage;

            switch (spell.SpellType)
            {
                case TrainingType.Berserk:
                    if (InRageCooldown(defender))
                    {
                        return;
                    }

                    if (spell.Phase > 1)
                    {
                        damage -= (int)(damage * spell.DamageMod);
                        defender.FixedParticles(0x376A, 10, 30, 5052, 1261, 7, EffectLayer.LeftFoot, 0);
                    }

                    break;
                case TrainingType.ConsumeDamage:
                    if (spell.Phase < 2)
                    {
                        damage = 0;
                    }

                    break;
                case TrainingType.AsOne:
                    if (defender is BaseCreature { ControlMaster: PlayerMobile pm })
                    {
                        var list = pm.AllFollowers.Where(m => (m == defender || m.InRange(defender.Location, 3)) && m.CanBeHarmful(attacker)).ToList();

                        if (list.Count > 0)
                        {
                            damage /= list.Count;

                            foreach (var m in list.Where(mob => mob != defender))
                            {
                                m.Damage(damage, attacker, true, false);
                            }
                        }
                    }

                    return;
            }

            if (spell.Phase < 2)
            {
                if (spell.Phase != 1)
                {
                    spell.Phase = 1;

                    if (spell.SpellType != TrainingType.AsOne && (spell.SpellType != TrainingType.Berserk || !InRageCooldown(defender)))
                    {
                        Server.Timer.DelayCall(TimeSpan.FromSeconds(5), spell.EndPhase1);
                    }
                }

                if (spell.DamageTaken == 0)
                {
                    defender.FixedEffect(0x3779, 10, 30, 1743, 0);
                }

                spell.DamageTaken += storedDamage;
            }
        }
        else if (attacker is BaseCreature { Controlled: true } or BaseCreature { Summoned: true })
        {
            var spell = EnumerateAllSpells().FirstOrDefault(sp => sp is CombatTrainingSpell && sp.Target == attacker) as CombatTrainingSpell;

            if (spell is { SpellType: TrainingType.Empowerment, Phase: > 1 })
            {
                damage += (int)(damage * spell.DamageMod);
                attacker.FixedParticles(0x376A, 10, 30, 5052, 1261, 7, EffectLayer.LeftFoot, 0);
            }
        }
    }

    public static void OnCreatureHit(Mobile attacker, Mobile defender, ref int damage)
    {
        if (attacker is not (BaseCreature { Controlled: true } or BaseCreature { Summoned: true }))
        {
            return;
        }

        var spell = EnumerateAllSpells().FirstOrDefault(sp => sp is CombatTrainingSpell && sp.Target == attacker) as CombatTrainingSpell;

        if (spell is { SpellType: TrainingType.Berserk, Phase: > 1 })
        {
            damage += (int)(damage * spell.DamageMod);
            attacker.FixedParticles(0x376A, 10, 30, 5052, 1261, 7, EffectLayer.LeftFoot, 0);
        }
    }

    public static int RegenBonus(Mobile m)
    {
        if (m is not (BaseCreature { Controlled: true } or BaseCreature { Summoned: true }))
        {
            return 0;
        }

        var spell = EnumerateAllSpells().FirstOrDefault(sp => sp is CombatTrainingSpell && sp.Target == m) as CombatTrainingSpell;

        return spell is { SpellType: TrainingType.ConsumeDamage, Phase: > 1 } ? (int)(30.0 * spell.DamageMod) : 0;
    }

    public static int GetHitChanceBonus(Mobile m)
    {
        if (m is not (BaseCreature { Controlled: true } or BaseCreature { Summoned: true }))
        {
            return 0;
        }

        var spell = EnumerateAllSpells().FirstOrDefault(sp => sp is CombatTrainingSpell && sp.Target == m) as CombatTrainingSpell;

        return spell is { SpellType: TrainingType.ConsumeDamage, Phase: > 1 } ? (int)(45 * spell.DamageMod) : 0;
    }

    private class InternalTarget : Target
    {
        private readonly CombatTrainingSpell _spell;

        public InternalTarget(CombatTrainingSpell spell) : base(8, false, TargetFlags.None) => _spell = spell;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCreature bc || bc.ControlMaster != from || from.Spell != _spell)
            {
                return;
            }

            _spell.Caster.FixedEffect(0x3779, 10, 20, 1270, 0);
            _spell.Caster.SendSound(0x64E);

            var taming = (int)from.Skills[SkillName.AnimalTaming].Value;
            var lore = (int)from.Skills[SkillName.AnimalLore].Value;

            from.CheckTargetSkill(SkillName.AnimalTaming, bc, taming - 25, taming + 25);
            from.CheckTargetSkill(SkillName.AnimalLore, bc, lore - 25, lore + 25);

            from.CloseGump<ChooseTrainingGump>();
            from.SendGump(new ChooseTrainingGump(from, bc, _spell));
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            from.SendLocalizedMessage(1156110); // Your ability was canceled.
            _spell.FinishSequence();
        }
    }

    public static void AddRageCooldown(Mobile m) =>
        _rageCooldown[m] = Server.Timer.DelayCall(TimeSpan.FromSeconds(60), () => _rageCooldown.Remove(m));

    public static bool InRageCooldown(Mobile m) => _rageCooldown.ContainsKey(m);

    private static readonly Dictionary<Mobile, Timer> _rageCooldown = new();
}

public class ChooseTrainingGump : Gump
{
    private const int Hue = 0x07FF;

    private readonly CombatTrainingSpell _spell;
    private readonly BaseCreature _target;

    public ChooseTrainingGump(Mobile caster, BaseCreature target, CombatTrainingSpell spell) : base(100, 100)
    {
        _spell = spell;
        _target = target;

        AddBackground(0, 0, 260, 187, 3600);
        AddAlphaRegion(10, 10, 240, 167);
        AddImageTiled(220, 15, 30, 162, 10464);

        AddHtmlLocalized(20, 20, 150, 16, 1156113, Hue, false, false); // Select Training

        var y = 40;

        if (MasteryInfo.HasLearned(caster, SkillName.AnimalTaming, 1))
        {
            AddButton(20, y, 9762, 9763, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(43, y, 150, 16, 1156109, Hue, false, false); // Empowerment
            y += 20;
        }

        if (MasteryInfo.HasLearned(caster, SkillName.AnimalTaming, 2))
        {
            AddButton(20, y, 9762, 9763, 2, GumpButtonType.Reply, 0);
            AddHtmlLocalized(43, y, 150, 16, 1153271, Hue, false, false); // Berserk
            y += 20;
        }

        if (MasteryInfo.HasLearned(caster, SkillName.AnimalTaming, 3))
        {
            AddButton(20, y, 9762, 9763, 3, GumpButtonType.Reply, 0);
            AddHtmlLocalized(43, y, 150, 16, 1156108, Hue, false, false); // Consume Damage
            y += 20;
        }

        if (MasteryInfo.HasLearned(caster, SkillName.AnimalTaming, 1))
        {
            AddButton(20, y, 9762, 9763, 4, GumpButtonType.Reply, 0);
            AddHtmlLocalized(43, y, 150, 16, 1157544, Hue, false, false); // As One
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            _spell.FinishSequence();
            sender.Mobile.SendLocalizedMessage(1156110); // Your ability was canceled.
            return;
        }

        _spell.OnSelected((TrainingType)(info.ButtonID - 1), _target);
    }
}
