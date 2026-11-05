using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TastyTreat : Item
{
    private static readonly Dictionary<BaseCreature, DateTime> _table = new();

    [Constructible]
    public TastyTreat(int amount = 1) : base(2424)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1112774;

    public virtual double Bonus => 0.05;

    public virtual TimeSpan Duration => TimeSpan.FromMinutes(20);

    public virtual TimeSpan CoolDown => TimeSpan.FromMinutes(2);

    public static bool UnderInfluence(BaseCreature bc) => _table.ContainsKey(bc);

    public static void RemoveInfluence(BaseCreature bc)
    {
        _table.Remove(bc);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1113213); // * For Pets Only *

        if (Bonus == 0.10)
        {
            list.Add(1113215); // Stats Increased by 10%
        }
        else if (Bonus == 0.15)
        {
            list.Add(1113216); // Stats Increased by 15%
        }
        else
        {
            list.Add(1113214); // Stats Increased by 5%
        }

        list.Add(1113212, Duration.TotalMinutes.ToString()); // Duration: ~1_val~ minutes
        list.Add(1113218, CoolDown.TotalMinutes.ToString()); // Cooldown: ~1_val~ minutes
    }

    public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
    {
        TryFeed(from, target);

        return false;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            return;
        }

        m.BeginTarget(
            2, false, TargetFlags.Beneficial,
            (from, targeted) =>
            {
                if (targeted is Mobile mobile)
                {
                    TryFeed(from, mobile);
                }
            }
        );
    }

    public bool DoEffects(BaseCreature bc)
    {
        bc.AddStatMod(new StatMod(StatType.Str, bc.Serial + "Str", (int)(bc.RawStr * Bonus), Duration));
        bc.AddStatMod(new StatMod(StatType.Int, bc.Serial + "Int", (int)(bc.RawInt * Bonus), Duration));
        bc.AddStatMod(new StatMod(StatType.Dex, bc.Serial + "Dex", (int)(bc.RawDex * Bonus), Duration));

        bc.PlaySound(0x1EA);
        bc.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);

        bc.Loyalty = BaseCreature.MaxLoyalty;

        _table[bc] = Core.Now;

        Timer.DelayCall(Duration + CoolDown, () => RemoveInfluence(bc));

        Consume();
        return true;
    }

    private void TryFeed(Mobile from, Mobile target)
    {
        if (target is not BaseCreature bc || bc.IsDeadBondedPet)
        {
            return;
        }

        if (UnderInfluence(bc))
        {
            if (_table[bc] + (CoolDown + Duration) < Core.Now)
            {
                from.SendLocalizedMessage(1113049); // Your pet is still recovering from the last tasty treat.
            }
            else
            {
                from.SendLocalizedMessage(1113051); // Your pet is still enjoying the last tasty treat!
            }
        }
        else if (bc.ControlMaster == from)
        {
            from.SendLocalizedMessage(1113050); // Your pet looks much happier.
            DoEffects(bc);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class DeliciouslyTastyTreat : TastyTreat
{
    [Constructible]
    public DeliciouslyTastyTreat(int amount = 1)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double Bonus => 0.10;

    public override TimeSpan Duration => TimeSpan.FromMinutes(10);

    public override TimeSpan CoolDown => TimeSpan.FromMinutes(60);

    public override int LabelNumber => 1113004; // Deliciously Tasty Treat
}

[SerializationGenerator(0, false)]
public partial class IrresistiblyTastyTreat : TastyTreat
{
    [Constructible]
    public IrresistiblyTastyTreat(int amount = 1)
    {
        Stackable = true;
        Amount = amount;
    }

    public override double Bonus => 0.15;

    public override TimeSpan Duration => TimeSpan.FromMinutes(20);

    public override TimeSpan CoolDown => TimeSpan.FromMinutes(120);

    public override int LabelNumber => 1113005; // Irresistibly Tasty Treat
}
