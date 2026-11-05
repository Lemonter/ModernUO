using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class EnchantedWheelbarrow : Item, ISecurable
{
    private static readonly Type[] _decorativePlants =
    {
        typeof(DecorativePlant), typeof(DecorativePlantWhiteFlowers), typeof(DecorativePlantVines),
        typeof(DecorativePlantFlax), typeof(DecorativePlantPoppies), typeof(DecorativePlantLilypad)
    };

    private TimerExecutionToken _timerToken;

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Harvest
    {
        get => _harvest;
        set
        {
            if (value && ItemID is 0xA0E6 or 0xA0E7)
            {
                ItemID += 2;
            }
            else if (ItemID is 0xA0E8 or 0xA0E9)
            {
                ItemID -= 2;
            }

            _harvest = value;
            this.MarkDirty();
        }
    }

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextHarvest;

    [Constructible]
    public EnchantedWheelbarrow() : base(0xA0E6)
    {
        Harvest = true;
        Weight = 5.0;
    }

    public override int LabelNumber => 1125214; // enchanted wheelbarrow

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!_harvest)
        {
            StartTimer();
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _timerToken.Cancel();
    }

    public void StopTimer() => _timerToken.Cancel();

    public void StartTimer()
    {
        if (_timerToken.Running)
        {
            return;
        }

        Timer.StartTimer(TimeSpan.FromHours(1.0), TimeSpan.FromHours(1.0), OnTick, out _timerToken);
    }

    public void OnTick()
    {
        if (_harvest)
        {
            StopTimer();
            return;
        }

        if (NextHarvest < Core.Now)
        {
            Harvest = true;
        }
    }

    public bool CheckAccessible(Mobile from, Item item)
    {
        if (from.AccessLevel >= AccessLevel.GameMaster)
        {
            return true; // Staff can access anything
        }

        var house = BaseHouse.FindHouseAt(item);

        if (house == null)
        {
            return false;
        }

        return Level switch
        {
            SecureLevel.Owner    => house.IsOwner(from),
            SecureLevel.CoOwners => house.IsCoOwner(from),
            SecureLevel.Friends  => house.IsFriend(from),
            SecureLevel.Anyone   => true,
            SecureLevel.Guild    => house.IsGuildMember(from),
            _                    => false
        };
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public void Flip()
    {
        if (ItemID is 0xA0E6 or 0xA0E8)
        {
            ItemID += 1;
        }
        else
        {
            ItemID -= 1;
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 3))
        {
            m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            return;
        }

        if (!IsLockedDown)
        {
            m.SendLocalizedMessage(1114298); // This must be locked down in order to use it.
            return;
        }

        if (!CheckAccessible(m, this))
        {
            return;
        }

        if (!_harvest)
        {
            m.SendLocalizedMessage(1158329); // There is nothing to harvest.
            return;
        }

        if (Activator.CreateInstance(_decorativePlants[Utility.Random(_decorativePlants.Length)]) is not Item plant)
        {
            return;
        }

        m.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1158330); // *You collect a plant from the wheelbarrow*
        m.AddToBackpack(plant);
        Harvest = false;
        NextHarvest = Core.Now + TimeSpan.FromDays(7);
        StartTimer();
    }
}

[SerializationGenerator(0, false)]
public partial class DecorativePlant : Item
{
    private static readonly int[] _ids =
    {
        0xA0E1, 0xA0E2, 0xA0E3, 0xA0E4, 0xA0E5, 0xA0ED, 0xA11B, 0xA11C, 0xA11F, 0xA120, 0xA121, 0xA122, 0xA123, 0xA124,
        0xA125, 0xA128, 0xA12B
    };

    [Constructible]
    public DecorativePlant() : base(_ids[Utility.Random(_ids.Length)])
    {
    }

    [Constructible]
    public DecorativePlant(int id) : base(id)
    {
    }

    public override int LabelNumber => 1125205; // decorative plant
}

[Flippable(0xA0DD, 0xA0DE)]
[SerializationGenerator(0, false)]
public partial class DecorativePlantWhiteFlowers : DecorativePlant
{
    [Constructible]
    public DecorativePlantWhiteFlowers() : base(0xA0DD)
    {
    }
}

[Flippable(0xA0DF, 0xA0E0)]
[SerializationGenerator(0, false)]
public partial class DecorativePlantVines : DecorativePlant
{
    [Constructible]
    public DecorativePlantVines() : base(0xA0DF)
    {
    }
}

[Flippable(0xA11D, 0xA11E)]
[SerializationGenerator(0, false)]
public partial class DecorativePlantFlax : DecorativePlant
{
    [Constructible]
    public DecorativePlantFlax() : base(0xA11D)
    {
    }
}

[Flippable(0xA126, 0xA127)]
[SerializationGenerator(0, false)]
public partial class DecorativePlantPoppies : DecorativePlant
{
    [Constructible]
    public DecorativePlantPoppies() : base(0xA126)
    {
    }
}

[Flippable(0xA129, 0xA12A)]
[SerializationGenerator(0, false)]
public partial class DecorativePlantLilypad : DecorativePlant
{
    [Constructible]
    public DecorativePlantLilypad() : base(0xA129)
    {
    }
}
