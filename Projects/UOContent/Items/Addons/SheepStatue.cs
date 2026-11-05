using ModernUO.Serialization;
using System;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/SheepStatue.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SheepStatue : BaseAddon, IRewardItem
{
    [SerializableField(0, fieldChanged: nameof(OnIsRewardItemChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [SerializableField(1, fieldChanged: nameof(OnResourceCountChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _resourceCount;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextResourceCount;

    private void OnIsRewardItemChanged(bool oldValue, bool newValue) => InvalidateProperties();

    private void OnResourceCountChanged(int oldValue, int newValue)
    {
        if (Components.Count > 0)
        {
            if (newValue == 0 && Components[0].ItemID != 0x4A95)
            {
                Components[0].ItemID = 0x4A95;
            }
            else if (newValue > 0 && Components[0].ItemID != 0x4A94)
            {
                Components[0].ItemID = 0x4A94;
            }
        }

        InvalidateProperties();
    }

    public override bool ForceShowProperties => true;

    public SheepStatue(int itemID)
        : base()
    {
        AddComponent(new InternalAddonComponent(itemID), 0, 0, 0);
        _nextResourceCount = Core.Now + TimeSpan.FromDays(1);
    }

    public override BaseAddonDeed Deed
    {
        get
        {
            SheepStatueDeed deed = new SheepStatueDeed();
            deed.IsRewardItem = _isRewardItem;
            deed.ResourceCount = _resourceCount;

            return deed;
        }
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        BaseHouse house = BaseHouse.FindHouseAt(this);

        /*
        * Unique problems have unique solutions.  OSI does not have a problem with 1000s of mining carts
        * due to the fact that they have only a miniscule fraction of the number of 10 year vets that a
        * typical RunUO shard will have (RunUO's scaled down account aging system makes this a unique problem),
        * and the "freeness" of free accounts. We also dont have mitigating factors like inactive (unpaid)
        * accounts not gaining veteran time.
        *
        * The lack of high end vets and vet rewards on OSI has made testing the *exact* ranging/stacking
        * behavior of these things all but impossible, so either way its just an estimation.
        *
        * If youd like your shard's carts/stumps to work the way they did before, simply replace the check
        * below with this line of code:
        *
        * if (!from.InRange(GetWorldLocation(), 2)
        *
        * However, I am sure these checks are more accurate to OSI than the former version was.
        *
        */

        if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this) || !((from.Z - Z) > -3 && (from.Z - Z) < 3))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (house != null && house.HasSecureAccess(from, SecureLevel.Friends))
        {
            if (_resourceCount > 0)
            {
                Item res = null;

                switch ( Utility.Random(5) )
                {
                    case 0: res = new Wool(); break;
                    case 1: res = new Leather(); break;
                    case 2: res = new SpinedLeather(); break;
                    case 3: res = new HornedLeather(); break;
                    case 4: res = new BarbedLeather(); break;
                }

                int amount = Math.Min(10, _resourceCount);
                res.Amount = amount;

                if (!from.PlaceInBackpack(res))
                {
                    res.Delete();
                    from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
                }
                else
                {
                    ResourceCount -= amount;
                    PublicOverheadMessage(MessageType.Regular, 0, 1151834, _resourceCount.ToString()); // Resources: ~1_COUNT~
                }
            }
            else
                from.SendLocalizedMessage(1094725); // There are no more ResourceCounts available at this time.
        }
        else
            from.SendLocalizedMessage(1061637); // You are not allowed to access
    }

    [SerializationGenerator(0, false)]
    private partial class InternalAddonComponent : AddonComponent
    {
        public InternalAddonComponent(int id)
            :base(id)
        {
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            if (Addon is SheepStatue)
            {
                list.Add(1151834, ((SheepStatue)Addon).ResourceCount.ToString()); // Resources: ~1_val~
            }
        }
    }

    private void TryGiveResourceCount()
    {
        if (_nextResourceCount < Core.Now)
        {
            ResourceCount = Math.Min(100, _resourceCount + 10);
            _nextResourceCount = Core.Now + TimeSpan.FromDays(1);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class SheepStatueDeed : BaseAddonDeed, IRewardItem
{
    [SerializableField(0, fieldChanged: nameof(OnIsRewardItemChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [SerializableField(1, fieldChanged: nameof(OnResourceCountChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _resourceCount;

    private void OnIsRewardItemChanged(bool oldValue, bool newValue) => InvalidateProperties();

    private void OnResourceCountChanged(int oldValue, int newValue) => InvalidateProperties();

    [Constructible]
    public SheepStatueDeed()
        : base()
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1151835; // Sheep Statue Deed

    public override BaseAddon Addon
    {
        get
        {
            SheepStatue addon = new SheepStatue(_resourceCount > 0 ? 0x4A94 : 0x4A95);
            addon.IsRewardItem = _isRewardItem;
            addon.ResourceCount = _resourceCount;

            return addon;
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
            list.Add(1076223); // 7th Year Veteran Reward
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this, null))
            return;

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
        else
            base.OnDoubleClick(from);
    }
}
