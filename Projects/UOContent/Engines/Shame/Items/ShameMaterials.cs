using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Items/QuartzGrit.cs, CursedOilstone.cs,
///     CorrosiveAsh.cs, WhetstoneOfEnervation.cs) — the three crafting components the dungeon's
///     creatures drop and the whetstone they combine into.
///
///     Each component, used from the pack, consumes itself plus one of each other component to form a
///     Whetstone of Enervation; the whetstone strips the damage-increase property off an exceptional
///     weapon. ServUO checks <c>TimesImbued</c>, which this codebase doesn't track, and uses the old
///     <c>ItemQuality</c> enum — weapons here carry <see cref="WeaponQuality" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class QuartzGrit : Item
{
    public override int LabelNumber => 1151808; // Quartz Grit

    [Constructible]
    public QuartzGrit() : this(1)
    {
    }

    [Constructible]
    public QuartzGrit(int amount) : base(0x423A)
    {
        Hue = 1151;
        Weight = 1;

        Stackable = true;
        Amount = amount;
    }

    public override void OnDoubleClick(Mobile from) => ShameMaterials.TryCombine(from, this);
}

[SerializationGenerator(0, false)]
public partial class CursedOilstone : Item
{
    public override int LabelNumber => 1151810; // Cursed Oilstone

    [Constructible]
    public CursedOilstone() : this(1)
    {
    }

    [Constructible]
    public CursedOilstone(int amount) : base(0x0F8B)
    {
        Weight = 1;

        Stackable = true;
        Amount = amount;
    }

    public override void OnDoubleClick(Mobile from) => ShameMaterials.TryCombine(from, this);
}

[SerializationGenerator(0, false)]
public partial class CorrosiveAsh : Item
{
    public override int LabelNumber => 1151809; // Corrosive Ash

    [Constructible]
    public CorrosiveAsh() : this(1)
    {
    }

    [Constructible]
    public CorrosiveAsh(int amount) : base(0x423A)
    {
        Hue = 1360;
        Weight = 1;

        Stackable = true;
        Amount = amount;
    }

    public override void OnDoubleClick(Mobile from) => ShameMaterials.TryCombine(from, this);
}

[SerializationGenerator(0, false)]
public partial class WhetstoneOfEnervation : Item
{
    public override int LabelNumber => 1151811; // Whetstone of Enervation

    [Constructible]
    public WhetstoneOfEnervation() : this(1)
    {
    }

    [Constructible]
    public WhetstoneOfEnervation(int amount) : base(0x1368)
    {
        Hue = 1458;
        Weight = 1;

        Amount = amount;
        Stackable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.BeginTarget(-1, false, TargetFlags.None, OnTarget);
    }

    private void OnTarget(Mobile from, object targeted)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (targeted is not BaseWeapon weapon || !weapon.IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1046439); // Invalid target.
            return;
        }

        if (weapon.Quality != WeaponQuality.Exceptional || weapon.Attributes.WeaponDamage <= 0)
        {
            from.SendLocalizedMessage(1046439); // Invalid target.
            return;
        }

        weapon.Attributes.WeaponDamage = 0;
        from.SendLocalizedMessage(1151814); // You have removed the damage increase from this weapon.

        Consume();
    }
}

/// <summary>Shared combine rule: any one component plus the other two forms a whetstone.</summary>
public static class ShameMaterials
{
    private const int CombineMessage = 1151812;
    private const int MissingComponent = 1151813;

    public static void TryCombine(Mobile from, Item used)
    {
        if (!used.IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        var pack = from.Backpack;

        var needsGrit = used is not QuartzGrit;
        var needsOilstone = used is not CursedOilstone;
        var needsAsh = used is not CorrosiveAsh;

        if (needsGrit && pack.GetAmount(typeof(QuartzGrit)) == 0)
        {
            from.SendLocalizedMessage(MissingComponent, "#1151808"); // Quartz Grit
            return;
        }

        if (needsOilstone && pack.GetAmount(typeof(CursedOilstone)) == 0)
        {
            from.SendLocalizedMessage(MissingComponent, "#1151810"); // Cursed Oilstone
            return;
        }

        if (needsAsh && pack.GetAmount(typeof(CorrosiveAsh)) == 0)
        {
            from.SendLocalizedMessage(MissingComponent, "#1151809"); // Corrosive Ash
            return;
        }

        Combine(from, used);
    }

    private static void Combine(Mobile from, Item used)
    {
        var pack = from.Backpack;

        if (used is not QuartzGrit)
        {
            pack.ConsumeTotal(new[] { typeof(QuartzGrit) }, new[] { 1 });
        }

        if (used is not CursedOilstone)
        {
            pack.ConsumeTotal(new[] { typeof(CursedOilstone) }, new[] { 1 });
        }

        if (used is not CorrosiveAsh)
        {
            pack.ConsumeTotal(new[] { typeof(CorrosiveAsh) }, new[] { 1 });
        }

        used.Consume();

        from.AddToBackpack(new WhetstoneOfEnervation());

        // You have managed to form the items into a rancid smelling, crag covered, hardened lump...
        from.SendLocalizedMessage(CombineMessage);
    }
}
