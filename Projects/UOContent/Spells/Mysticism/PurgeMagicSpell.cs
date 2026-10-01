using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Ninjitsu;
using Server.Spells.Second;
using Server.Targeting;

namespace Server.Spells.Mysticism;

/// <summary>Mysticism 679. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/PurgeMagicSpell.cs); registration was already in
/// Spells/Initializer.cs, commented out.
///
/// Strips one buff, chosen at random from whatever the target actually has. If the target has
/// nothing to strip, the caster curses them instead — a delayed pure-damage hit that lands
/// when the victim next takes damage, scaled by how long the curse sat on them.
///
/// Two of the original's nine detectable buffs, Barrab Hemolymph and Urali Trance, are Eodon
/// tribal potion effects. Neither the potions nor the effects exist anywhere in this codebase
/// yet, so there is nothing to detect and nothing to strip; the other seven are all here.
/// When the Eodon potions get ported, add them to GetRandomBuff and RemoveBuff.</summary>
public class PurgeMagicSpell : MysticSpell, ITargetingSpell<Mobile>
{
    private static readonly SpellInfo _info = new(
        "Purge",
        "An Ort Sanct",
        230,
        9022,
        Reagent.Garlic,
        Reagent.MandrakeRoot,
        Reagent.SulfurousAsh,
        Reagent.FertileDirt
    );

    private static readonly Dictionary<Mobile, DateTime> _immunity = new();
    private static readonly Dictionary<Mobile, CurseEntry> _curses = new();

    public PurgeMagicSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Second;

    public int TargetRange => 12;

    public override bool CheckCast()
    {
        // A caster still carrying the curse can't cast it on anyone else.
        if (IsUnderCurseEffects(Caster))
        {
            Caster.SendLocalizedMessage(1154212); // You cannot cast this spell while you are cursed.
            return false;
        }

        return base.CheckCast();
    }

    public void Target(Mobile m)
    {
        if (!Caster.CanSee(m))
        {
            Caster.SendLocalizedMessage(500237); // Target can not be seen.
            return;
        }

        if (!CheckHSequence(m))
        {
            return;
        }

        SpellHelper.Turn(Caster, m);

        if (IsImmune(m) || IsUnderCurseEffects(m))
        {
            Caster.SendLocalizedMessage(1080119); // Your target resists your attempt to purge their magic.
            return;
        }

        if (CheckResisted(m))
        {
            m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
            return;
        }

        Caster.PlaySound(0x655);
        Effects.SendLocationParticles(
            EffectItem.Create(m.Location, m.Map, EffectItem.DefaultDuration),
            0x3728,
            1,
            13,
            0x834,
            0,
            0x13B2,
            0
        );

        var buff = GetRandomBuff(m);

        if (buff == BuffType.None)
        {
            // Nothing to take, so leave a curse behind instead.
            Caster.SendLocalizedMessage(1080120); // Your target has no magic to purge.

            var curseSeconds = Math.Max(1.0, (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 28);
            ApplyCurse(m, TimeSpan.FromSeconds(curseSeconds));
            return;
        }

        RemoveBuff(m, buff);

        m.SendLocalizedMessage(1080117);      // Your magical protection has been purged.
        Caster.SendLocalizedMessage(1080118); // You have purged your target's magical protection.

        var immuneSeconds = Math.Max(1.0, (Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value) / 15);
        _immunity[m] = Core.Now + TimeSpan.FromSeconds(immuneSeconds);
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
    }

    public static bool IsImmune(Mobile m) =>
        _immunity.TryGetValue(m, out var until) && until > Core.Now;

    public static bool IsUnderCurseEffects(Mobile m) => _curses.ContainsKey(m);

    /// <summary>Called from AOS.Damage — the curse discharges the moment its victim is hurt,
    /// for 5 damage per second it had been sitting there.</summary>
    public static void OnDamage(Mobile m)
    {
        if (!_curses.Remove(m, out var entry))
        {
            return;
        }

        entry.Token.Cancel();

        var seconds = (int)(Core.Now - entry.Started).TotalSeconds;
        var damage = 5 * seconds;

        if (damage > 0)
        {
            AOS.Damage(m, entry.Caster, damage, false, 0, 0, 0, 0, 0, 0, 100);
        }
    }

    private void ApplyCurse(Mobile m, TimeSpan duration)
    {
        if (_curses.ContainsKey(m))
        {
            return;
        }

        m.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
        m.PlaySound(0x307);

        Timer.StartTimer(duration, () => _curses.Remove(m), out var token);

        _curses[m] = new CurseEntry(Caster, Core.Now, token);
    }

    private static BuffType GetRandomBuff(Mobile m)
    {
        using var candidates = Collections.PooledRefList<BuffType>.Create();

        if (MagicReflectSpell.HasReflect(m))
        {
            candidates.Add(BuffType.MagicReflect);
        }

        if (ReactiveArmorSpell.HasArmor(m))
        {
            candidates.Add(BuffType.ReactiveArmor);
        }

        if (ProtectionSpell.Registry.ContainsKey(m))
        {
            candidates.Add(BuffType.Protection);
        }

        // Animal Form is explicitly not purgeable.
        var context = TransformationSpellHelper.GetContext(m);

        if (context != null && context.Spell is not AnimalForm)
        {
            candidates.Add(BuffType.Transformation);
        }

        var str = m.GetStatMod("[Magic] Str Buff") != null;
        var dex = m.GetStatMod("[Magic] Dex Buff") != null;
        var intel = m.GetStatMod("[Magic] Int Buff") != null;

        if (str && dex && intel)
        {
            // All three at once is Bless; it hides the individual stat buffs from detection.
            candidates.Add(BuffType.Bless);
        }
        else
        {
            if (str)
            {
                candidates.Add(BuffType.StrBonus);
            }

            if (dex)
            {
                candidates.Add(BuffType.DexBonus);
            }

            if (intel)
            {
                candidates.Add(BuffType.IntBonus);
            }
        }

        return candidates.Count == 0 ? BuffType.None : candidates[Utility.Random(candidates.Count)];
    }

    private static void RemoveBuff(Mobile m, BuffType buff)
    {
        switch (buff)
        {
            case BuffType.MagicReflect:
                {
                    MagicReflectSpell.EndReflect(m);
                    break;
                }
            case BuffType.ReactiveArmor:
                {
                    ReactiveArmorSpell.EndArmor(m);
                    break;
                }
            case BuffType.Protection:
                {
                    ProtectionSpell.EndProtection(m);
                    break;
                }
            case BuffType.Transformation:
                {
                    TransformationSpellHelper.RemoveContext(m, true);
                    break;
                }
            case BuffType.Bless:
                {
                    m.RemoveStatMod("[Magic] Str Buff");
                    m.RemoveStatMod("[Magic] Dex Buff");
                    m.RemoveStatMod("[Magic] Int Buff");
                    break;
                }
            case BuffType.StrBonus:
                {
                    m.RemoveStatMod("[Magic] Str Buff");
                    break;
                }
            case BuffType.DexBonus:
                {
                    m.RemoveStatMod("[Magic] Dex Buff");
                    break;
                }
            case BuffType.IntBonus:
                {
                    m.RemoveStatMod("[Magic] Int Buff");
                    break;
                }
        }
    }

    private enum BuffType
    {
        None,
        MagicReflect,
        ReactiveArmor,
        Protection,
        Transformation,
        Bless,
        StrBonus,
        DexBonus,
        IntBonus
    }

    private record struct CurseEntry(Mobile Caster, DateTime Started, TimerExecutionToken Token);
}
