using System;
using ModernUO.Serialization;
using Server.Spells;

namespace Server.Items;

/// <summary>The stone Mysticism's Healing Stone spell leaves in your pack. Ported from ServUO
/// (Scripts/Items/Consumables/HealingStone.cs).
///
/// Two pools, which is the whole point of the item: LifeForce is the total it can ever give
/// back and only ever goes down, while MaxHeal is the per-use ceiling — it collapses to 1
/// after a heal and climbs back to full over the next 15 seconds. So the stone is a big
/// reserve you can only draw on in sips.
///
/// Adaptations: Poison.RealLevel doesn't exist here, so the cure cost uses Poison.Level; the
/// free-hand test reuses BasePotion.HasFreeHand, already the codebase's shared answer to that
/// question.</summary>
[SerializationGenerator(0, false)]
public partial class HealingStone : Item
{
    [SerializableField(0)]
    private int _lifeForce;

    [SerializableField(1)]
    private int _maxHeal;

    [SerializableField(2)]
    private int _maxLifeForce;

    [SerializableField(3)]
    private int _maxHealTotal;

    private bool _cooldown;
    private TimerExecutionToken _regenToken;

    [Constructible]
    public HealingStone(int amount = 1, int maxHeal = 1) : base(0x4078)
    {
        Movable = true;
        LootType = LootType.Blessed;

        _lifeForce = amount;
        _maxLifeForce = amount;
        _maxHeal = maxHeal;
        _maxHealTotal = maxHeal;
    }

    public override int LabelNumber => 1115274; // Healing Stone

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1115274, $"{_lifeForce}"); // Life Force: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (!from.InRange(GetWorldLocation(), 1))
        {
            from.SendLocalizedMessage(502138); // That is too far away for you to use.
            return;
        }

        if (!BasePotion.HasFreeHand(from))
        {
            from.SendLocalizedMessage(1080116); // You must have a free hand to use a Healing Stone.
            return;
        }

        if (_cooldown)
        {
            return;
        }

        if (from.Poison != null)
        {
            var toUse = Math.Min(120, from.Poison.Level * 25);

            if (toUse > _lifeForce)
            {
                // Not enough left to purge it outright — the attempt still costs something.
                LifeForce = _lifeForce - toUse / 3;
                from.SendLocalizedMessage(1080118); // You cannot heal yourself in your current state.
            }
            else if (from.CurePoison(from))
            {
                LifeForce = _lifeForce - toUse;
                from.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                from.PlaySound(0x1E0);
            }
        }
        else if (from.Hits < from.HitsMax)
        {
            var toHeal = Math.Min(_maxHeal, from.HitsMax - from.Hits);
            toHeal = Math.Min(toHeal, _lifeForce);

            if (toHeal > 0)
            {
                SpellHelper.Heal(toHeal, from, from);

                LifeForce = _lifeForce - toHeal;
                MaxHeal = 1;

                from.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
                from.PlaySound(0x202);

                BeginRegen();
            }
        }
        else
        {
            from.SendLocalizedMessage(1049547); // You decide against drinking this potion, as you are already at full health.
            return;
        }

        _cooldown = true;
        Timer.DelayCall(TimeSpan.FromSeconds(2), () => _cooldown = false);

        if (_lifeForce <= 0)
        {
            Delete();
        }
        else
        {
            InvalidateProperties();
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _regenToken.Cancel();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_maxHeal < _maxHealTotal)
        {
            BeginRegen();
        }
    }

    /// <summary>MaxHeal climbs back to its ceiling over 15 seconds, a fifteenth per tick.</summary>
    private void BeginRegen()
    {
        _regenToken.Cancel();

        var step = Math.Max(1, _maxHealTotal / 15);

        Timer.StartTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), () =>
        {
            if (Deleted)
            {
                _regenToken.Cancel();
                return;
            }

            MaxHeal = Math.Min(_maxHealTotal, _maxHeal + step);

            if (_maxHeal >= _maxHealTotal)
            {
                _regenToken.Cancel();
            }
        }, out _regenToken);
    }
}
