using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Ninjitsu;
using Server.Spells.Spellweaving;

namespace Server.Spells.Mysticism;

/// <summary>Mysticism 680. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/EnchantSpell .cs — the stray space in that
/// filename is theirs); registration was already in Spells/Initializer.cs, commented out.
///
/// Temporarily grants your equipped weapon one of the five hit-spell properties. The caster
/// picks which through a gump before the cast starts.
///
/// The bonus goes through the Enhancement layer (Misc/Enhancement.cs, ported alongside this)
/// under the title "EnchantAttribute", exactly as the original — so the weapon's own
/// properties are never written to and the effect cannot outlive its timer. At 80+ in both
/// skills the same entry also carries Spell Channeling +1 and Cast Speed -1, and the spell
/// remembers that it applied the malus.</summary>
public class EnchantSpell : MysticSpell
{
    private const string EnhancementTitle = "EnchantAttribute";

    private static readonly SpellInfo _info = new(
        "Enchant",
        "In Ort Ylem",
        230,
        9022,
        Reagent.SpidersSilk,
        Reagent.MandrakeRoot,
        Reagent.SulfurousAsh
    );

    private static readonly Dictionary<Mobile, EnchantmentTimer> _table = new();

    public static readonly AosWeaponAttribute[] Attributes =
    {
        AosWeaponAttribute.HitLightning,
        AosWeaponAttribute.HitFireball,
        AosWeaponAttribute.HitHarm,
        AosWeaponAttribute.HitMagicArrow,
        AosWeaponAttribute.HitDispel
    };

    private readonly AosWeaponAttribute _attribute;
    private readonly bool _chosen;

    public EnchantSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public EnchantSpell(Mobile caster, Item scroll, AosWeaponAttribute attribute)
        : base(caster, scroll, _info)
    {
        _attribute = attribute;
        _chosen = true;
    }

    public override SpellCircle Circle => SpellCircle.Second;

    public override bool CheckCast()
    {
        if (!_chosen)
        {
            // No property picked yet — put the menu up and abandon this cast; the gump
            // starts a fresh one with the choice baked in.
            EnchantGump.DisplayTo(Caster, Scroll);
            return false;
        }

        if (!base.CheckCast())
        {
            return false;
        }

        var weapon = Caster.Weapon as BaseWeapon;

        if (weapon == null || Caster.FindItemOnLayer(Layer.OneHanded) != weapon &&
            Caster.FindItemOnLayer(Layer.TwoHanded) != weapon)
        {
            Caster.SendLocalizedMessage(501078); // You must be holding a weapon.
            return false;
        }

        if (IsUnderSpellEffects(Caster, weapon))
        {
            Caster.SendLocalizedMessage(501775); // This spell is already in effect.
            return false;
        }

        if (weapon.Consecrated || ImmolatingWeaponSpell.IsImmolating(weapon))
        {
            // You cannot use this ability while your weapon is enchanted.
            Caster.SendLocalizedMessage(1080128);
            return false;
        }

        // ServUO calls its own FocusAttack.IsActive; there's no such accessor here, so the
        // check goes through SpecialMove.GetCurrentMove, which is how this codebase asks
        // "which special move is armed right now".
        if (SpecialMove.GetCurrentMove(Caster) is FocusAttack)
        {
            // You cannot enchant an item while under the effects of focus attack.
            Caster.SendLocalizedMessage(1080446);
            return false;
        }

        foreach (var attr in Attributes)
        {
            if (weapon.WeaponAttributes[attr] > 0)
            {
                // The weapon must be free of hit spell effects.
                Caster.SendLocalizedMessage(1080127);
                return false;
            }
        }

        return true;
    }

    public override void OnCast()
    {
        if (CheckSequence() && Caster.Weapon is BaseWeapon weapon)
        {
            Caster.PlaySound(0x64E);
            Caster.FixedEffect(0x36CB, 1, 9, 1915, 0);

            var primary = Caster.Skills[CastSkill].Value;
            var secondary = Caster.Skills[DamageSkill].Value;

            var bonus = (int)(60 * (primary + secondary) / 240);
            var duration = TimeSpan.FromSeconds((primary + secondary) / 2.0 + 30.0);

            Enhancement.SetValue(Caster, _attribute, bonus, EnhancementTitle);

            var malus = false;

            // SpellChanneling lives on AosAttribute here, not AosWeaponAttribute as in ServUO.
            if (primary >= 80 && secondary >= 80 && weapon.Attributes[AosAttribute.SpellChanneling] == 0)
            {
                Enhancement.SetValue(Caster, AosAttribute.SpellChanneling, 1, EnhancementTitle);
                Enhancement.SetValue(Caster, AosAttribute.CastSpeed, -1, EnhancementTitle);
                malus = true;
            }

            weapon.EnchantedWeilder = Caster;
            weapon.InvalidateProperties();

            var timer = new EnchantmentTimer(Caster, weapon, _attribute, bonus, malus, duration);
            timer.Start();

            _table[Caster] = timer;

            (Caster as PlayerMobile)?.AddBuff(
                new BuffInfo(BuffIcon.Enchant, 1080126, GetAttributeCliloc(_attribute), duration, $"{bonus}")
            );
        }

        FinishSequence();
    }

    public static bool IsUnderSpellEffects(Mobile m, BaseWeapon weapon) =>
        _table.TryGetValue(m, out var timer) && timer.Weapon == weapon;

    public static AosWeaponAttribute BonusAttribute(Mobile m) =>
        _table.TryGetValue(m, out var timer) ? timer.Attribute : 0;

    public static int BonusValue(Mobile m) => _table.TryGetValue(m, out var timer) ? timer.Bonus : 0;

    public static bool CastingMalus(Mobile m, BaseWeapon weapon) =>
        _table.TryGetValue(m, out var timer) && timer.Malus;

    public static void RemoveEnchantment(Mobile m)
    {
        if (_table.TryGetValue(m, out var timer))
        {
            timer.Expire();
        }
    }

    /// <summary>Unequipping the weapon drops the enchantment with it.</summary>
    public static void OnWeaponRemoved(BaseWeapon weapon, Mobile m)
    {
        if (_table.TryGetValue(m, out var timer) && timer.Weapon == weapon)
        {
            timer.Expire();
        }
    }

    private static int GetAttributeCliloc(AosWeaponAttribute attr) =>
        attr switch
        {
            AosWeaponAttribute.HitLightning  => 1079701,
            AosWeaponAttribute.HitFireball   => 1079703,
            AosWeaponAttribute.HitHarm       => 1079700,
            AosWeaponAttribute.HitMagicArrow => 1079702,
            _                                => 1079699 // HitDispel
        };

    private class EnchantmentTimer : Timer
    {
        private readonly Mobile _owner;

        internal EnchantmentTimer(
            Mobile owner, BaseWeapon weapon, AosWeaponAttribute attribute, int bonus, bool malus,
            TimeSpan duration
        ) : base(duration)
        {
            _owner = owner;
            Weapon = weapon;
            Attribute = attribute;
            Bonus = bonus;
            Malus = malus;
        }

        internal BaseWeapon Weapon { get; }
        internal AosWeaponAttribute Attribute { get; }
        internal int Bonus { get; }
        internal bool Malus { get; }

        protected override void OnTick()
        {
            Expire();
        }

        internal void Expire()
        {
            Stop();
            _table.Remove(_owner);

            _owner.SendLocalizedMessage(1115273); // The enchantment on your weapon has expired.
            _owner.PlaySound(0x1E6);

            Enhancement.RemoveMobile(_owner, EnhancementTitle);

            if (Weapon?.Deleted == false)
            {
                Weapon.EnchantedWeilder = null;
                Weapon.InvalidateProperties();
            }

            (_owner as PlayerMobile)?.RemoveBuff(BuffIcon.Enchant);
        }
    }
}

/// <summary>Property picker for Enchant. Ported in spirit from ServUO's EnchantSpellGump;
/// written against this codebase's plain Gump, same as MasterySelectionGump.cs.</summary>
public class EnchantGump : Gump
{
    private static readonly int[] _labels =
    {
        1079701, // Hit Lightning
        1079703, // Hit Fireball
        1079700, // Hit Harm
        1079702, // Hit Magic Arrow
        1079699  // Hit Dispel
    };

    private readonly Item _scroll;

    private EnchantGump(Item scroll) : base(60, 36)
    {
        _scroll = scroll;

        AddBackground(0, 0, 260, 60 + EnchantSpell.Attributes.Length * 24, 9380);
        AddHtmlLocalized(0, 12, 260, 16, 1080122, "", 0, false, false); // Select the enchantment

        var y = 40;

        for (var i = 0; i < EnchantSpell.Attributes.Length; i++)
        {
            AddButton(24, y, 4005, 4007, i + 1);
            AddHtmlLocalized(64, y, 180, 16, _labels[i], "", 0, false, false);
            y += 24;
        }
    }

    /// <summary>Rule #13 — never construct a gump that could come out empty. The attribute
    /// list is a static array so it can't be empty, but the caster still has to be a player
    /// with a client to send it to.</summary>
    public static void DisplayTo(Mobile from, Item scroll)
    {
        if (from is not PlayerMobile pm || pm.NetState == null)
        {
            return;
        }

        pm.CloseGump<EnchantGump>();
        pm.SendGump(new EnchantGump(scroll));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID - 1;

        if (index < 0 || index >= EnchantSpell.Attributes.Length)
        {
            return;
        }

        new EnchantSpell(sender.Mobile, _scroll, EnchantSpell.Attributes[index]).Cast();
    }
}
