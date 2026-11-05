using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Engines.Craft;

using Server.Engines.BuffIcons;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Clothing/RunedSashOfWarding.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class RunedSashOfWarding : BodySash
{
    public static Dictionary<Mobile, WardingEffect> Table { get { return _table; } }

    private static Dictionary<Mobile, WardingEffect> _table = new Dictionary<Mobile, WardingEffect>();

    public override int LabelNumber => 1116231;

    private int _charges;

    private WardingEffect _ward;

    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges { get { return _charges; } set { _charges = value; InvalidateProperties(); } }

    [CommandProperty(AccessLevel.GameMaster)]
    public WardingEffect Ward { get { return _ward; } set { _ward = value; } }

    public override int InitMinHits => 255;

    public override int InitMaxHits => 255;

    [Constructible]
    public RunedSashOfWarding()
    {
        Hue = 1157;
        _charges = 50;

        if (Utility.RandomBool())
        {
            _ward = WardingEffect.WeaponDamage;
        }
        else
        {
            _ward = WardingEffect.SpellDamage;
        }

    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_charges <= 0)
        {
            from.SendLocalizedMessage(501259); //This magic item is out of charges.
        }
        if (IsUnderEffects(from, _ward))
        {
            from.SendLocalizedMessage(502173); //You are already under a similar effect.
        }
        else if (from.FindItemOnLayer(Layer.MiddleTorso) == this)
        {
            Charges--;

            _table[from] = _ward;
            Timer.DelayCall(TimeSpan.FromSeconds(10.0), RemoveEffects, from);

            from.FixedParticles(14154, 1, 30, 9964, 3, 3, EffectLayer.Waist);

            for (int i = -1; i <= 1; i++)
            {
                IEntity entity = new Entity(Serial.Zero, new Point3D(from.X + i, from.Y, from.Z), from.Map);
                IEntity to = new Entity(Serial.Zero, new Point3D(from.X + i, from.Y, from.Z + 50), from.Map);
                Effects.SendMovingParticles(entity, to, 7956, 1, 0, false, false, 33, 3, 9501, 1, 0, EffectLayer.Head, 0x100);
            }

            from.PlaySound(0x5C1);
            from.SendLocalizedMessage(1116243); //The runes glow and a magical warding forms around your body.

            (from as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Warding, 1151395, 1151396, default, _ward == WardingEffect.WeaponDamage ? 1116172 : 1116173));
        }
        else
        {
            from.SendLocalizedMessage(1112886); //You must be wearing this item in order to use it.
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060741, _charges.ToString());

        switch (_ward)
        {
            case WardingEffect.WeaponDamage:
                list.Add(1116172);
                break;
            case WardingEffect.SpellDamage:
                list.Add(1116173);
                break;
        }

        list.Add(1060639, $"{{this.HitPoints}}\t{{this.MaxHitPoints}}"); // durability ~1_val~ / ~2_val~
    }

    public static bool IsUnderEffects(Mobile from, WardingEffect type)
    {
        return _table.ContainsKey(from) && _table[from] == type;
    }

    public static void RemoveEffects(object obj)
    {
        Mobile from = (Mobile)obj;

        if (_table.ContainsKey(from))
        {
            _table.Remove(from);
        }

        from.SendLocalizedMessage(1116244); //The magical ward around you dissipates.

        (from as PlayerMobile)?.RemoveBuff(BuffIcon.Warding);
    }
}

[SerializationGenerator(0, false)]
public partial class GargishRunedSashOfWarding : RunedSashOfWarding
{
    public override int RequiredRaces => Race.AllowGargoylesOnly;

    [Constructible]
    public GargishRunedSashOfWarding()
    {
        ItemID = 0x46B4;
    }
}

public enum WardingEffect
    {
        WeaponDamage,
        SpellDamage
    }
