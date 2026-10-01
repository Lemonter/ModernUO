using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/BodyGuard.cs) — Parry
// mastery: the caster tanks a share of damage aimed at a protected ally within 2 tiles.
public class BodyGuardSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new("Body Guard", "", -1, 9002);

    public override int RequiredMana => 40;
    public override int DisruptMessage => 1156103; // Bodyguard has expired.
    public override bool BlocksMovement => false;
    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.0);

    public override SkillName CastSkill => SkillName.Parry;

    private double _block;
    public int BlockPercent => (int)_block;

    public BodyGuardSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override bool CheckCast()
    {
        if (_table.TryGetValue(Caster, out var pending))
        {
            RemoveGumpTimer(pending, Caster);
            return false;
        }

        if (GetSpell(Caster, GetType()) is BodyGuardSpell spell)
        {
            spell.Expire(true);
            return false;
        }

        return HasShield() && base.CheckCast();
    }

    public override void OnCast()
    {
        if (Caster is BaseCreature { ControlMaster: { } master } &&
            Caster.CanSee(master) && Caster.InRange(master.Location, 8))
        {
            SpellHelper.Turn(Caster, master);
            OnTarget(master);
            return;
        }

        Caster.BeginTarget(8, false, TargetFlags.None, (m, o) =>
        {
            if (!Caster.CanSee(o))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (o is Mobile targetMobile)
            {
                SpellHelper.Turn(Caster, targetMobile);
                OnTarget(targetMobile);
            }
        });
    }

    protected override void OnTarget(object o)
    {
        if (!HasShield() || o is not Mobile protectee)
        {
            return;
        }

        if (GetSpell(Caster, typeof(BodyGuardSpell)) is { } existing && existing.Target == protectee)
        {
            Caster.SendLocalizedMessage(1156094); // Your target is already under the effect of this ability.
            return;
        }

        if (!protectee.Alive)
        {
            Caster.SendLocalizedMessage(501857); // This spell won't work on that!
            return;
        }

        if (!Caster.CanBeBeneficial(protectee, true))
        {
            Caster.SendLocalizedMessage(1001017); // You cannot perform beneficial acts on your target.
            return;
        }

        if (protectee == Caster)
        {
            return;
        }

        var responsible = protectee is BaseCreature { Summoned: false, ControlMaster: PlayerMobile master } ? master : protectee;

        Caster.FixedParticles(0x376A, 9, 32, 5030, 1168, 0, EffectLayer.Waist, 0);

        if (Caster.Player)
        {
            Caster.PlaySound(Caster.Female ? 0x338 : 0x44A);
        }
        else if (Caster is BaseCreature bc)
        {
            Caster.PlaySound(bc.GetAngerSound());
        }

        if (Caster is PlayerMobile)
        {
            protectee.SendGump(new AcceptBodyguardGump(Caster, protectee, this));
            AddGumpTimer(responsible, Caster);
        }
        else
        {
            AcceptBodyGuard(responsible);
        }
    }

    public void AcceptBodyGuard(Mobile toGuard)
    {
        RemoveGumpTimer(toGuard, Caster);

        if (CheckBSequence(toGuard))
        {
            _block = (Caster.Skills[CastSkill].Value + GetWeaponSkill() + GetMasteryLevel() * 40) / 3 / 2.4;
            Target = toGuard;

            Expires = Core.Now + TimeSpan.FromSeconds(90);
            BeginTimer();

            Caster.SendLocalizedMessage(1049452, "\t" + toGuard.Name); // You are now protecting ~2_NAME~.
            toGuard.SendLocalizedMessage(1049451, Caster.Name);        // You are now being protected by ~1_NAME~.

            // ~1_NAME~ receives ~2_DAMAGE~% of all damage dealt to ~3_NAME~. All damage
            // dealt to ~3_NAME~ will be reduced by ~4_DAMAGE~%. Body guard must be within
            // 2 tiles.
            var args = $"{Caster.Name}\t{(int)(_block + 5)}\t{toGuard.Name}\t{(int)_block}";

            if (Caster is PlayerMobile casterPm)
            {
                casterPm.AddBuff(new BuffInfo(BuffIcon.Bodyguard, 1155924, 1156061, TimeSpan.FromSeconds(90), args));
            }

            if (toGuard is PlayerMobile toGuardPm)
            {
                toGuardPm.AddBuff(new BuffInfo(BuffIcon.Bodyguard, 1155924, 1156061, TimeSpan.FromSeconds(90), args));
            }
        }

        FinishSequence();
    }

    private bool HasShield()
    {
        if (!Caster.Player)
        {
            return true;
        }

        if (Caster.FindItemOnLayer(Layer.TwoHanded) is BaseShield)
        {
            return true;
        }

        Caster.SendLocalizedMessage(1156096); // You must be wielding a shield to use this ability!
        return false;
    }

    public override bool OnTick()
    {
        if (!HasShield())
        {
            Expire();
            return false;
        }

        return base.OnTick();
    }

    public override void EndEffects()
    {
        if (Target is PlayerMobile targetPm)
        {
            Target.SendLocalizedMessage(1156103); // Bodyguard has expired.
            targetPm.RemoveBuff(BuffIcon.Bodyguard);
        }

        Caster.SendLocalizedMessage(1156103);

        if (Caster is PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Bodyguard);
        }
    }

    public void DeclineBodyGuard(Mobile protectee)
    {
        Caster.SendLocalizedMessage(1049454, "\t" + protectee.Name); // ~2_NAME~ has declined your protection.
        protectee.SendLocalizedMessage(1049453, Caster.Name);        // You have declined protection from ~1_NAME~.

        RemoveGumpTimer(protectee, Caster);
        FinishSequence();
    }

    public override void OnTargetDamaged(Mobile attacker, Mobile defender, DamageType type, ref int damage)
    {
        if (defender != Target || !Caster.InRange(defender, 2))
        {
            return;
        }

        var mod = BlockPercent / 100.0;
        var originalDamage = damage;

        damage -= (int)(damage * mod);

        // Tooltip promises the bodyguard absorbs (block%+5)% of the ORIGINAL hit — was
        // computing this off the already-reduced `damage` instead (D*(1-mod)*(1.05-mod)),
        // which both undershot the number and made it shrink as block% rose instead of grow.
        var casterDamage = (int)(originalDamage * (mod + .05));

        if (type == DamageType.Spell)
        {
            casterDamage /= 2;
        }

        Caster.Damage(casterDamage, attacker);
    }

    private static readonly Dictionary<Mobile, Mobile> _table = new();

    public static void RemoveGumpTimer(Mobile m, Mobile caster)
    {
        _table.Remove(caster);

        if (m.HasGump<AcceptBodyguardGump>())
        {
            m.CloseGump<AcceptBodyguardGump>();
            m.SendLocalizedMessage(1156103);
            caster.SendLocalizedMessage(1156103);
        }
    }

    public static void AddGumpTimer(Mobile m, Mobile caster)
    {
        _table[caster] = m;
        Server.Timer.DelayCall(TimeSpan.FromSeconds(10), () => RemoveGumpTimer(m, caster));
    }
}

public class AcceptBodyguardGump : Gump
{
    private readonly Mobile _protectee;
    private readonly BodyGuardSpell _spell;

    public AcceptBodyguardGump(Mobile protector, Mobile protectee, BodyGuardSpell spell) : base(150, 50)
    {
        _protectee = protectee;
        _spell = spell;

        Closable = false;

        AddBackground(0, 0, 396, 218, 3600);
        AddImageTiled(15, 15, 365, 190, 2624);
        AddAlphaRegion(15, 15, 365, 190);

        AddHtmlLocalized(30, 20, 360, 25, 1156099, 0x7FFF, false, false); // Another player is offering to bodyguard you:
        AddLabel(90, 55, 1153, $"{protector.Name} will body guard {protectee.Name}");

        AddImage(50, 45, 9005);
        AddImageTiled(80, 80, 200, 1, 9107);
        AddImageTiled(95, 82, 200, 1, 9157);

        AddRadio(30, 110, 9727, 9730, true, 1);
        AddHtmlLocalized(65, 115, 300, 25, 1049444, 0x7FFF, false, false); // Yes, I would like their protection.

        AddRadio(30, 145, 9727, 9730, false, 0);
        AddHtmlLocalized(65, 148, 300, 25, 1049445, 0x7FFF, false, false); // No thanks, I can take care of myself.

        AddButton(160, 175, 247, 248, 2, GumpButtonType.Reply, 0);

        AddImage(215, 0, 50581);
        AddImageTiled(15, 14, 365, 1, 9107);
        AddImageTiled(380, 14, 1, 190, 9105);
        AddImageTiled(15, 205, 365, 1, 9107);
        AddImageTiled(15, 14, 1, 190, 9105);
        AddImageTiled(0, 0, 395, 1, 9157);
        AddImageTiled(394, 0, 1, 217, 9155);
        AddImageTiled(0, 216, 395, 1, 9157);
        AddImageTiled(0, 0, 1, 217, 9155);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 2)
        {
            return;
        }

        if (info.IsSwitched(1))
        {
            _spell.AcceptBodyGuard(_protectee);
        }
        else
        {
            _spell.DeclineBodyGuard(_protectee);
        }
    }
}
