using ModernUO.Serialization;

using Server.Engines.BuffIcons;

using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/SpellFocusingSash.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SpellFocusingSash : BodySash
{
    public override int LabelNumber => 1150059; // Spell Focusing Sash

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    public override double DefaultWeight => 1;

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public Mobile SpellCastTarget { get; set; }

    [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
    public int SpellCastCount { get; set; }

    [CommandProperty(AccessLevel.Counselor, true)]
    public int SpellDamageOffset
    {
        get
        {
            if (SpellCastCount <= 5)
            {
                return -(SpellCastCount * 6);
            }

            return (SpellCastCount - 6) * 2;
        }
    }

    [Constructible]
    public SpellFocusingSash()
        : this(0)
    {
    }

    public SpellFocusingSash(int hue)
    {
        Hue = hue;

        Attributes.BonusMana = 1;
        Attributes.DefendChance = 5;

    }

    public override void AddWeightProperty(IPropertyList list)
    {
        base.AddWeightProperty(list);

        list.Add(1150058); // Spell Focusing
    }

    private void RefreshBuffInfo(Mobile caster)
    {
        var offset = SpellDamageOffset;

        if (offset < 0)
        {
            (caster as PlayerMobile)?.RemoveBuff(BuffIcon.SpellFocusingBuff);

            if (caster == Parent)
            {
                // Spell Focusing
                // Target: ~1_val~<br>Damage Modifier: ~2_val~%
                (caster as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.SpellFocusingDebuff, 1151391, 1151392, default, $"{SpellCastTarget?.Name ?? "None"}\t{offset}"));
            }
        }
        else
        {
            (caster as PlayerMobile)?.RemoveBuff(BuffIcon.SpellFocusingDebuff);

            if (caster == Parent)
            {
                // Spell Focusing
                // Target: ~1_val~<br>Damage Modifier: ~2_val~%
                (caster as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.SpellFocusingBuff, 1151391, 1151392, default, $"{SpellCastTarget?.Name ?? "None"}\t{offset}"));
            }
        }
    }

    public void ValidateTarget(Mobile caster, Mobile target, out int sdiOffset)
    {
        sdiOffset = 0;

        try
        {
            if (caster == null || caster.Deleted || !caster.Alive || caster != Parent)
            {
                SpellCastTarget = null;
                SpellCastCount = 0;

                return;
            }

            if (SpellCastTarget != target)
            {
                SpellCastTarget = target;
                SpellCastCount = 0;

                caster.SendLocalizedMessage(1150117); // Spell focusing damage has reset.
            }

            if (SpellCastTarget == null)
            {
                SpellCastTarget = null;
                SpellCastCount = 0;

                return;
            }

            if (!SpellCastTarget.Alive || SpellCastTarget.Deleted)
            {
                SpellCastTarget = null;
                SpellCastCount = 0;

                caster.SendLocalizedMessage(1150117); // Spell focusing damage has reset.

                return;
            }

            // offset range is -30 to +30
            // first 5 casts are multiples of -6
            // 6th cast is negated
            // every cast after is +2

            if (++SpellCastCount <= 21)
            {
                sdiOffset = SpellDamageOffset;

                if (sdiOffset == 0)
                {
                    caster.SendLocalizedMessage(1150118); // Spell focusing damage has now been tuned to your opponent.
                }
            }

            if (SpellCastCount >= 21)
            {
                SpellCastTarget = null;
                SpellCastCount = 0;

                caster.SendLocalizedMessage(1150116); // Spell focusing damage has peaked.
            }
        }
        finally
        {
            RefreshBuffInfo(caster);
        }
    }

    public override void OnRemoved(IEntity oldParent)
    {
        base.OnRemoved(oldParent);

        SpellCastTarget = null;
        SpellCastCount = 0;

        RefreshBuffInfo(oldParent as Mobile);
        RefreshBuffInfo(Parent as Mobile);
    }
}
