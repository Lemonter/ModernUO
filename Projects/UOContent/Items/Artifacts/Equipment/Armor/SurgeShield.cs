using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Mobiles;

using Server.Engines.BuffIcons;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Armor/SurgeShield.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SurgeShield : BronzeShield
{
    public override int LabelNumber => 1116232;

    private int _charges;

    private SurgeType _surge;

    [CommandProperty(AccessLevel.GameMaster)]
    public int Charges { get { return _charges; } set { _charges = value; InvalidateProperties(); } }

    [CommandProperty(AccessLevel.GameMaster)]
    public SurgeType Surge
    {
        get { return _surge; }
        set
        {
            _surge = value;

            if (_surge == SurgeType.None)
            {
                Charges = 0;
            }
            else
            {
                Charges = 50;
            }
        }
    }

    [Constructible]
    public SurgeShield()
    {
        if (0.90 > Utility.RandomDouble())
        {
            Hue = 2125;
        }
        else
        {
            Hue = 448;
        }

        switch(Utility.Random(5))
        {
            case 0: PhysicalBonus = 5; break;
            case 1: FireBonus = 5; break;
            case 2: ColdBonus = 5; break;
            case 3: PoisonBonus = 5; break;
            case 4: EnergyBonus = 5; break;
        }

        switch(Utility.Random(3))
        {
            case 0: Surge = SurgeType.Hits; break;
            case 1: Surge = SurgeType.Stam; break;
            case 2: Surge = SurgeType.Mana; break;
        }

        if(Utility.RandomBool())
        {
            Attributes.AttackChance = 5;
        }
        else
        {
            Attributes.LowerManaCost = 4;
        }

        if(Utility.RandomBool())
        {
            Attributes.SpellChanneling = 1;
        }
        else
        {
            Attributes.CastSpeed = 1;
        }
    }

    public static Dictionary<Mobile, SurgeType> Table { get { return _table; } }

    private static Dictionary<Mobile, SurgeType> _table = new Dictionary<Mobile, SurgeType>();

    public override void OnDoubleClick(Mobile from)
    {
        if (Surge == SurgeType.None || Charges <= 0)
        {
            base.OnDoubleClick(from);
        }
        else if (from.FindItemOnLayer(Layer.TwoHanded) != this)
        {
            from.SendLocalizedMessage(1116250); //That must be equipped before you can use it.
        }
        else if (!IsUnderEffects(from, _surge))
        {
            Charges--;

            _table[from] = _surge;
            Timer.DelayCall(TimeSpan.FromSeconds(20.0), RemoveEffects, from);

            from.PlaySound(0x0F5);
            from.PlaySound(0x1ED);
            from.FixedParticles(0x375A, 1, 30, 9966, 33, 2, EffectLayer.Waist);
            from.FixedParticles(0x37B9, 1, 30, 9502, 43, 3, EffectLayer.Waist);

            from.SendMessage("Вы чувствуете прилив энергии.");

            (from as PlayerMobile)?.AddBuff(new BuffInfo(BuffIcon.Surge, 1151397, 1151398, default, _surge.ToString()));
        }
        else
        {
            from.SendLocalizedMessage(1116174); //You must wait for the energy to recharge before using the surge effect again.
        }
    }

    public static void RemoveEffects(object obj)
    {
        Mobile from = (Mobile)obj;

        if (_table.ContainsKey(from))
        {
            _table.Remove(from);
        }

        (from as PlayerMobile)?.RemoveBuff(BuffIcon.Surge);
        //TODO: Message?
    }

    public static bool IsUnderEffects(Mobile from, SurgeType type)
    {
        return _table.ContainsKey(from) && _table[from] == type;
    }
}

public enum SurgeType
    {
        None,
        Hits,
        Mana,
        Stam
    }
