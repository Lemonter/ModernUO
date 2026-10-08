using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Mobiles/SilverTrader.cs).
// Simplified: dropped the gargoyle-artifact-conversion drag-drop mechanic (swaps human
// versions of specific artifacts for Gargish ones) — 3 of its 4 target types
// (GargishCrimsonCincture, GargishMaceAndShieldGlasses, GargishWizardsCrystalGlasses,
// GargishFoldedSteelGlasses) don't exist in this codebase. `RunicReforging`/
// `NegativeAttributes` antiquing dropped too — see ViceVsVirtueSystem.cs header.
[SerializationGenerator(0, false)]
public partial class SilverTrader : BaseVendor
{
    public override bool IsActiveVendor => false;
    public override bool DisallowAllMoves => true;
    public override bool ClickTitle => true;
    public override bool CanTeach => false;

    protected override List<SBInfo> SBInfos { get; } = new();
    public override void InitSBInfo()
    {
    }

    [Constructible]
    public SilverTrader() : base("the Silver Trader")
    {
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        var robe = new Robe { ItemID = 0x2684, Name = "a robe", Hue = 1109 };
        EquipItem(robe);

        Timer.DelayCall(TimeSpan.FromSeconds(10), StockInventory);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add(1155513); // Vice vs Virtue Reward Vendor
    }

    private DateTime _nextSpeak;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (_nextSpeak < Core.Now && ViceVsVirtueSystem.IsVvV(m) && InRange(m.Location, 6) && m.Race == Race.Gargoyle)
        {
            SayTo(m, 1155534); // I will convert your human artifacts to gargoyle versions if you hand them to me.
            _nextSpeak = Core.Now + TimeSpan.FromSeconds(25);
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (ViceVsVirtueSystem.Enabled && m is PlayerMobile pm && InRange(m.Location, 3))
        {
            if (ViceVsVirtueSystem.IsVvV(m))
            {
                m.SendGump(new VvVRewardGump(this, pm));
            }
            else
            {
                SayTo(m, 1155585); // You have no silver to trade with. Join Vice vs Virtue and return to me.
            }
        }
    }

    public void StockInventory()
    {
        if (Backpack == null)
        {
            EquipItem(new Backpack());
        }

        foreach (var item in VvVRewards.Rewards)
        {
            if (item.Tooltip != 0)
            {
                continue;
            }

            if (Backpack.GetAmount(item.Type) > 0)
            {
                if (Backpack.FindItemByType(item.Type) is IVvVItem existing)
                {
                    existing.IsVvVItem = true;
                }

                continue;
            }

            if (Activator.CreateInstance(item.Type) is not Item i)
            {
                continue;
            }

            if (i is IOwnerRestricted owned)
            {
                owned.OwnerName = "Your Player Name";
            }

            if (i is IVvVItem vvvItem)
            {
                vvvItem.IsVvVItem = true;
            }

            ViceVsVirtueSystem.Instance.AddVvVItem(i, true);

            Backpack.DropItem(i);
        }
    }
}
