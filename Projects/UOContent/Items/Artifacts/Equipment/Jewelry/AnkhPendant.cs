using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Regions;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Jewelry/AnkhPendant.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class AnkhPendant : BaseNecklace
{
    public static void Initialize()
    {
        EventSink.Speech += EventSink_Speech;
    }

    public override int LabelNumber => 1079525; // Ankh Pendant

    [Constructible]
    public AnkhPendant()
        : base(0x3BB5)
    {
        Hue = Utility.RandomBool() ? 2213 : 0;
    }

    public static void EventSink_Speech(SpeechEventArgs e)
    {
        Mobile from = e.Mobile;
        Item ankh = from.FindItemOnLayer(Layer.Neck);

        if (!(ankh is AnkhPendant))
        {
            return;
        }

        string str = e.Speech.ToLower();
        VirtueType t = VirtueType.None;

        if (str == "ahm")
        {
            t = VirtueType.Honesty;
        }
        else if (str == "mu")
        {
            t = VirtueType.Compassion;
        }
        else if (str == "ra")
        {
            t = VirtueType.Valor;
        }
        else if (str == "beh")
        {
            t = VirtueType.Justice;
        }
        else if (str == "cah")
        {
            t = VirtueType.Sacrafice;
        }
        else if (str == "summ")
        {
            t = VirtueType.Honor;
        }
        else if (str == "om")
        {
            t = VirtueType.Spirituality;
        }
        else if (str == "lum")
        {
            t = VirtueType.Humility;
        }

        if (t != VirtueType.None && CheckShrine(t, from))
        {
            ApplyBonus(t, from);
        }
    }

    public static int GetHitsRegenModifier(Mobile from)
    {
        if (!_table.ContainsKey(from))
        {
            return 0;
        }

        if (CheckExpired(from))
        {
            return 0;
        }

        AnkhPendantBonusContext context = _table[from];

        if (context == null)
        {
            return 0;
        }

        switch (context.VType)
        {
            case VirtueType.Honesty: break;
            case VirtueType.Compassion:
                return 2;
            case VirtueType.Valor: break;
            case VirtueType.Justice:
                return context.DoBump ? 2 : 1;
            case VirtueType.Sacrafice:
                return context.DoBump ? 2 : 1;
            case VirtueType.Honor: break;
            case VirtueType.Spirituality:
                return context.DoBump ? 2 : 1;
            case VirtueType.Humility:
                if (context.Random == 0)
                {
                    return 3;
                }
                break;
        }
        return 0;
    }

    public static int GetStamRegenModifier(Mobile from)
    {
        if (!_table.ContainsKey(from))
        {
            return 0;
        }

        if (CheckExpired(from))
        {
            return 0;
        }

        AnkhPendantBonusContext context = _table[from];

        if (context == null)
        {
            return 0;
        }

        switch (context.VType)
        {
            case VirtueType.Honesty: break;
            case VirtueType.Compassion: break;
            case VirtueType.Valor:
                return 2;
            case VirtueType.Justice:
                break;
            case VirtueType.Sacrafice:
                return context.DoBump ? 2 : 1;
            case VirtueType.Honor:
                return context.DoBump ? 2 : 1;
            case VirtueType.Spirituality:
                return context.DoBump2 ? 2 : 1;
            case VirtueType.Humility:
                if (context.Random == 1)
                {
                    return 3;
                }
                break;
        }
        return 0;
    }

    public static int GetManaRegenModifier(Mobile from)
    {
        if (!_table.ContainsKey(from))
        {
            return 0;
        }

        if (CheckExpired(from))
        {
            return 0;
        }

        AnkhPendantBonusContext context = _table[from];

        if (context == null)
        {
            return 0;
        }

        switch (context.VType)
        {
            case VirtueType.Honesty:
                return 2;
            case VirtueType.Compassion: break;
            case VirtueType.Valor:
                break;
            case VirtueType.Justice:
                return context.DoBump ? 2 : 1;
            case VirtueType.Sacrafice:
                break;
            case VirtueType.Honor:
                return context.DoBump ? 2 : 1;
            case VirtueType.Spirituality:
                return context.DoBump3 ? 2 : 1;
            case VirtueType.Humility:
                if (context.Random == 2)
                {
                    return 3;
                }
                break;
        }
        return 0;
    }

    public static bool CheckExpired(Mobile from)
    {
        if (_table.ContainsKey(from) && _table[from].Expired)
        {
            AddToCooldown(from);
            return true;
        }
        return false;
    }

    public static bool CheckShrine(VirtueType t, Mobile from)
    {
        Region r = from.Region;
        Map map = from.Map;

        if (r is DungeonRegion || r is TownRegion || (map != Map.Trammel && map != Map.Felucca))
        {
            return false;
        }

        bool atShrine = false;

        for (int i = 0; i < _shrineLocs.Length; i++)
        {
            if (_shrineLocs[i].Contains(new Point2D(from.X, from.Y)) && (int)t == i + 1)
            {
                atShrine = true;
                break;
            }
        }

        if (atShrine)
        {
            if (IsUnderEffects(from))
            {
                from.SendLocalizedMessage(1079544, String.Format("#{0}", GetCliloc(_table[from].VType)));
                return false;
            }

            if (IsWaitingCooldown(from))
            {
                TimeSpan ts = Core.Now - _cooldown[from];

                if (ts.TotalHours >= 1)
                {
                    from.SendLocalizedMessage(1079550, ((int)ts.TotalHours).ToString()); //You can improve your fortunes again in about ~1_TIME~ hours.
                }
                else
                {
                    from.SendLocalizedMessage(1079547); //Your fortunes are about to improve.
                }

                return false;
            }
        }

        return atShrine;
    }

    public static Rectangle2D[] ShrineLocs => _shrineLocs;

    private static Rectangle2D[] _shrineLocs = new Rectangle2D[]
    {
        new Rectangle2D(4208, 563, 2, 2), //Honesty
        new Rectangle2D(1857, 874, 2, 2), //Compassion
        new Rectangle2D(2491, 3930, 2, 2), //Valor
        new Rectangle2D(1300, 633, 2, 2), //Justice
        new Rectangle2D(3354, 289, 2, 2), //Sacrafice
        new Rectangle2D(1726, 3527, 2, 2), //Honor
        new Rectangle2D(1605, 2489, 2, 2), //Spirituality
        new Rectangle2D(4273, 3696, 2, 2), //Humility
    };

    private static void ApplyBonus(VirtueType t, Mobile from)
    {
        _table[from] = new AnkhPendantBonusContext(from, t);

        from.Delta(MobileDelta.WeaponDamage);

        from.SendLocalizedMessage(1079546, String.Format("#{0}", GetCliloc(t)));
    }

    private static int GetCliloc(VirtueType t)
    {
        switch (t)
        {
            default:
            case VirtueType.Honesty: return 1079539;
            case VirtueType.Compassion: return 1079535;
            case VirtueType.Valor: return 1079543;
            case VirtueType.Justice: return 1079536;
            case VirtueType.Sacrafice: return 1079538;
            case VirtueType.Honor: return 1079540;
            case VirtueType.Spirituality: return 1079542;
            case VirtueType.Humility: return 1079541;
        }
    }

    public static bool IsUnderEffects(Mobile from)
    {
        return _table.ContainsKey(from);
    }

    public static bool IsWaitingCooldown(Mobile from)
    {
        if (_cooldown.ContainsKey(from) && _cooldown[from] < Core.Now)
        {
            _cooldown.Remove(from);
        }

        return _cooldown.ContainsKey(from);
    }

    public static void AddToCooldown(Mobile from)
    {
        if (_table.ContainsKey(from))
        {
            _table.Remove(from);
        }

        from.Delta(MobileDelta.WeaponDamage);

        if (from.NetState != null)
        {
            from.SendLocalizedMessage(1079553); //The effects of meditating at the shrine have worn off.
        }

        _cooldown[from] = Core.Now + TimeSpan.FromHours(24);
    }

    private static Dictionary<Mobile, AnkhPendantBonusContext> _table = new Dictionary<Mobile, AnkhPendantBonusContext>();

    private static Dictionary<Mobile, DateTime> _cooldown = new Dictionary<Mobile, DateTime>();

    private class AnkhPendantBonusContext
    {
        private Mobile _mobile;

        private VirtueType _type;

        private int _random;

        private bool _doBump;

        private bool _doBump2;

        private bool _doBump3;

        private DateTime _expires;

        public VirtueType VType => _type;

        public bool DoBump => _doBump;

        public bool DoBump2 => _doBump2;

        public bool DoBump3 => _doBump3;

        public int Random => _random;

        public bool Expired => Core.Now > _expires;

        public AnkhPendantBonusContext(Mobile from, VirtueType type)
        {
            _mobile = from;
            _type = type;
            _random = -1;
            _expires = Core.Now + TimeSpan.FromMinutes(60);

            switch (type)
            {
                case VirtueType.Honesty:
                case VirtueType.Compassion:
                case VirtueType.Valor: break;
                case VirtueType.Humility:
                    _random = Utility.Random(3);
                    break;
                case VirtueType.Justice:
                case VirtueType.Sacrafice:
                case VirtueType.Honor:
                    _doBump = Utility.RandomBool();
                    break;
                case VirtueType.Spirituality:
                    _doBump = 0.25 > Utility.RandomDouble();
                    _doBump2 = 0.25 > Utility.RandomDouble();
                    _doBump3 = 0.25 > Utility.RandomDouble();
                    break;

            }
        }
    }
}

public enum VirtueType
    {
        None = 0,
        Honesty = 1,
        Compassion = 2,
        Valor = 3,
        Justice = 4,
        Sacrafice = 5,
        Honor = 6,
        Spirituality = 7,
        Humility = 8
    }
