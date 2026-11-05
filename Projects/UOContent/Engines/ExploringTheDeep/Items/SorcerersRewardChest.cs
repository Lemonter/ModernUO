using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Items/SorcerersRewardChest.cs). The locked
///     strongbox that holds the salvager suit plans, and the key the Master Instructor drops.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SorcerersRewardChest : Item
{
    [Constructible]
    public SorcerersRewardChest() : base(0x9AA)
    {
        Movable = false;
        Hue = 1912;
    }

    public override int LabelNumber => 1023712; // strong box

    public override void OnDoubleClick(Mobile from)
    {
        // *It's an unassuming strong box. You examine the lock more closely and determine there is no way
        // to pick it. You'll need to find a key.*
        from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154226);

        base.OnDoubleClick(from);
    }
}

[SerializationGenerator(0, false)]
public partial class StrongboxKey : BaseDecayingItem
{
    [Constructible]
    public StrongboxKey() : base(0x410A)
    {
        Stackable = false;
        Weight = 0.01;
        Hue = 2721;
        LootType = LootType.Blessed;
        QuestItem = true;
    }

    public override int LabelNumber => 1154227; // Strongbox Key

    public override int Lifespan => 3600;
    public override bool UseSeconds => false;

    public override void OnDoubleClick(Mobile from)
    {
        if (from is not PlayerMobile)
        {
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1054107); // This item must be in your backpack.
            return;
        }

        from.Target = new ChestTarget(this);
        from.SendLocalizedMessage(1010086); // What do you want to use this on?
    }

    private class ChestTarget : Target
    {
        private readonly StrongboxKey _key;

        public ChestTarget(StrongboxKey key) : base(2, false, TargetFlags.None) => _key = key;

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is not SorcerersRewardChest)
            {
                from.SendLocalizedMessage(501668); // This key doesn't seem to unlock that.
                return;
            }

            Item item = new SalvagerSuitPlans();

            if (from.Backpack == null || !from.Backpack.TryDropItem(from, item, false))
            {
                from.BankBox?.DropItem(item);
            }

            // *You insert the key into the mechanism and turn it. To your delight the lock opens with a
            // click and you remove the contents*
            from.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1154228);

            _key.Delete();
        }
    }
}
