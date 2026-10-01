using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Items/Rewards/VvVPotions.cs).
[Flags]
public enum PotionType
{
    None = 0x0,
    AntiParalysis = 0x1,
    Supernova = 0x2,
    StatLossRemoval = 0x4,
    GreaterStamina = 0x8
}

[SerializationGenerator(0, false)]
public partial class VvVPotionKeg : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private PotionType _potionType;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private int _charges;

    public override double DefaultWeight => 10 + Charges * 1.8;

    [Constructible]
    public VvVPotionKeg() : this(PotionType.AntiParalysis)
    {
    }

    [Constructible]
    public VvVPotionKeg(PotionType type) : base(6870)
    {
        _potionType = type;
        _charges = 10;

        Hue = type switch
        {
            PotionType.Supernova        => 13,
            PotionType.StatLossRemoval  => 2500,
            PotionType.GreaterStamina   => 437,
            _                           => 2543
        };
    }

    [SerializableFieldChanged(1)]
    private void OnChargesChanged(int oldValue, int newValue)
    {
        if (newValue <= 0)
        {
            Delete();
        }
    }

    public override void AddNameProperty(IPropertyList list)
    {
        var str = _potionType switch
        {
            PotionType.Supernova       => "#1094718",
            PotionType.StatLossRemoval => "#1155541",
            PotionType.GreaterStamina  => "#1094764",
            _                          => "#1155543"
        };

        list.Add(1155535, str); // A Batch of ~1_ITEMS~
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042004); // That must be in your pack for you to use it
            return;
        }

        if (m.AccessLevel < AccessLevel.Counselor && !ViceVsVirtueSystem.IsVvV(m))
        {
            m.SendLocalizedMessage(1155496); // This item can only be used by VvV participants!
            return;
        }

        Item potion = _potionType switch
        {
            PotionType.AntiParalysis   => new AntiParalysisPotion(),
            PotionType.Supernova       => new SupernovaPotion(),
            PotionType.StatLossRemoval => new StatLossRemovalPotion(),
            PotionType.GreaterStamina  => new GreaterStaminaPotion(),
            _                          => null
        };

        if (potion == null)
        {
            return;
        }

        m.SendLocalizedMessage(502242); // You pour some of the keg's contents into an empty bottle...

        if (m.Backpack == null || !m.Backpack.TryDropItem(m, potion, false))
        {
            m.SendLocalizedMessage(1155570); // Your backpack could not hold the item. Free up some space and try again.
            potion.Delete();
        }
        else
        {
            m.SendLocalizedMessage(502243); // ...and place it into your backpack.
            m.PlaySound(0x240);

            Charges--;
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1155569, Charges.ToString()); // Potions: ~1_val~
        list.Add(1154937); // VvV Item
    }
}

[SerializationGenerator(0, false)]
public abstract partial class VvVPotion : Item
{
    public virtual TimeSpan CooldownDuration => TimeSpan.MinValue;
    public virtual PotionType CooldownType => PotionType.None;

    private static readonly Dictionary<Mobile, Dictionary<PotionType, DateTime>> _cooldown = new();

    public static void RemoveFromCooldown(Mobile m, PotionType type)
    {
        if (_cooldown.TryGetValue(m, out var byType) && byType.Remove(type) && byType.Count == 0)
        {
            _cooldown.Remove(m);
        }
    }

    public override int LabelNumber =>
        CooldownType switch
        {
            PotionType.AntiParalysis   => 1155543,
            PotionType.Supernova       => 1094718,
            PotionType.StatLossRemoval => 1155541,
            PotionType.GreaterStamina  => 1094764,
            _                          => base.LabelNumber
        };

    protected VvVPotion() : base(3849) => Stackable = true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1154937); // VvV Item
    }

    public bool IsInCooldown(Mobile m, ref DateTime dt)
    {
        if (!_cooldown.TryGetValue(m, out var byType))
        {
            return false;
        }

        if (byType.TryGetValue(PotionType.GreaterStamina, out dt))
        {
            if (dt < Core.Now)
            {
                RemoveFromCooldown(m, PotionType.GreaterStamina);
                return false;
            }

            return true;
        }

        if (byType.TryGetValue(CooldownType, out dt))
        {
            if (dt < Core.Now)
            {
                RemoveFromCooldown(m, CooldownType);
                return false;
            }

            return true;
        }

        return false;
    }

    public void AddToCooldown(Mobile m)
    {
        if (m.AccessLevel >= AccessLevel.Counselor)
        {
            return;
        }

        _cooldown.TryAdd(m, new Dictionary<PotionType, DateTime>());
        _cooldown[m][CooldownType] = Core.Now + CooldownDuration;
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!Movable)
        {
            return;
        }

        if (!IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042004); // That must be in your pack for you to use it
            return;
        }

        var dt = Core.Now;

        if (m.AccessLevel < AccessLevel.Counselor && ViceVsVirtueSystem.Enabled && !ViceVsVirtueSystem.IsVvV(m))
        {
            m.SendLocalizedMessage(1155496); // This item can only be used by VvV participants!
        }
        else if (!BasePotion.HasFreeHand(m))
        {
            m.SendLocalizedMessage(502172); // You must have a free hand to drink a potion.
        }
        else if (IsInCooldown(m, ref dt))
        {
            var left = dt - Core.Now;

            if (left.TotalMinutes > 2)
            {
                m.SendLocalizedMessage(1114110, ((int)left.TotalMinutes).ToString()); // You must wait ~1_minutes~ minutes before using another one of these.
            }
            else
            {
                m.SendLocalizedMessage(1114109, ((int)left.TotalSeconds).ToString()); // You must wait ~1_seconds~ seconds before using another one of these.
            }
        }
        else if (CheckUse(m))
        {
            UseEffects(m);
            Use(m);
        }
    }

    public virtual bool CheckUse(Mobile m) => true;

    public abstract void Use(Mobile m);

    public virtual void UseEffects(Mobile m)
    {
        m.RevealingAction();
        m.PlaySound(0x2D6);

        if (m.Body.IsHuman && !m.Mounted)
        {
            m.Animate(34, 5, 1, true, false, 0);
        }

        if (CooldownDuration != TimeSpan.MinValue)
        {
            AddToCooldown(m);
        }

        Timer.DelayCall(TimeSpan.FromMilliseconds(500), () => DrinkEffects(m));
    }

    public virtual void DrinkEffects(Mobile m)
    {
    }
}

[SerializationGenerator(0, false)]
public partial class AntiParalysisPotion : VvVPotion
{
    public override PotionType CooldownType => PotionType.AntiParalysis;

    [Constructible]
    public AntiParalysisPotion() => Hue = 2543;

    public override bool CheckUse(Mobile m)
    {
        if (!m.Paralyzed)
        {
            m.SendLocalizedMessage(1155544); // You are not currently paralyzed
            return false;
        }

        return true;
    }

    public override void Use(Mobile m)
    {
        m.Paralyzed = false;
        m.Stam /= 2;

        Consume();
    }

    public override void DrinkEffects(Mobile m)
    {
        m.FixedEffect(0x375A, 10, 15);
        m.PlaySound(0x1E7);
    }
}

[SerializationGenerator(0, false)]
public partial class SupernovaPotion : VvVPotion
{
    public override TimeSpan CooldownDuration => TimeSpan.FromMinutes(2);
    public override PotionType CooldownType => PotionType.Supernova;

    [Constructible]
    public SupernovaPotion() => Hue = 13;

    public override void Use(Mobile m)
    {
        Effects.SendMovingEffect(m, new Entity(Serial.Zero, new Point3D(m.X, m.Y, m.Z + 25), m.Map), ItemID, 3, 0, false, false, Hue, 0);

        const int count = 5;

        Timer.DelayCall(
            TimeSpan.FromSeconds(1),
            () =>
            {
                m.PlaySound(0x1DD);

                for (var i = 0; i < count; i++)
                {
                    Timer.DelayCall(
                        TimeSpan.FromMilliseconds(i * 170),
                        radius => Misc.Geometry.Circle2D(
                            m.Location,
                            m.Map,
                            radius,
                            (pnt, map) => Effects.SendLocationEffect(pnt, map, 0x3709, 30, 10, 1458, 5)
                        ),
                        i
                    );
                }
            }
        );

        Timer.DelayCall(
            TimeSpan.FromMilliseconds(170 * count),
            () =>
            {
                foreach (var mob in m.Map.GetMobilesInRange(m.Location, count))
                {
                    if (mob != m && Spells.SpellHelper.ValidIndirectTarget(m, mob) && m.CanBeHarmful(mob, false))
                    {
                        m.DoHarmful(mob);
                        AOS.Damage(mob, m, Utility.RandomMinMax(40, 60), 0, 100, 0, 0, 0);
                    }
                }
            }
        );

        if (m.AccessLevel == AccessLevel.Player)
        {
            Consume();
        }
    }

    public override void UseEffects(Mobile m) => AddToCooldown(m);
}

[SerializationGenerator(0, false)]
public partial class StatLossRemovalPotion : VvVPotion
{
    public override TimeSpan CooldownDuration => TimeSpan.FromMinutes(20);
    public override PotionType CooldownType => PotionType.StatLossRemoval;

    [Constructible]
    public StatLossRemovalPotion() => Hue = 2500;

    public override bool CheckUse(Mobile m)
    {
        if (!ViceVsVirtueSystem.InSkillLoss(m))
        {
            m.SendLocalizedMessage(1155542); // You are not currently under the effects of stat loss.
            return false;
        }

        return true;
    }

    public override void Use(Mobile m)
    {
        m.SendLocalizedMessage(1155540); // You feel the effects of your stat loss fade.
        ViceVsVirtueSystem.ClearSkillLoss(m);

        Consume();
    }

    public override void DrinkEffects(Mobile m)
    {
        m.PlaySound(0xF6);
        m.PlaySound(0x1F7);
        m.FixedParticles(0x3709, 1, 30, 9963, 13, 3, EffectLayer.Head);
    }
}

[SerializationGenerator(0, false)]
public partial class GreaterStaminaPotion : VvVPotion
{
    public override TimeSpan CooldownDuration => TimeSpan.FromSeconds(10);
    public override PotionType CooldownType => PotionType.GreaterStamina;

    [Constructible]
    public GreaterStaminaPotion() => Hue = 437;

    public override void Use(Mobile m)
    {
        Timer.DelayCall(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            10,
            () =>
            {
                var gain = Utility.RandomMinMax(10, 13);

                if (m.Stam + gain > m.StamMax)
                {
                    gain = m.StamMax - m.Stam;
                }

                m.FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
                m.Stam += gain;
            }
        );

        Consume();
    }

    public override void DrinkEffects(Mobile m)
    {
        m.FixedEffect(0x375A, 10, 15);
        m.PlaySound(0x1E7);
    }
}
