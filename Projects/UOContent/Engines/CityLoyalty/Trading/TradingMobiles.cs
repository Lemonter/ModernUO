/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Trading/Mobiles/TradeMinister.cs and Slim.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item><c>SetWearable</c>/<c>EquipItem</c> became <c>AddItem(new X { Hue = hue })</c>.</item>
///         <item><c>NameList.RandomName</c> became <see cref="RussianNames" />.</item>
///         <item>
///             <c>SkillHandlers.Imbuing.IngredTypes</c> and <c>Skeletonkey</c> do not exist here; the minister's
///             resource reward is limited to ingots, leather and boards, and Slim's mundane reward is a random
///             armour/weapon/jewellery piece or a scroll of transcendence.
///         </item>
///         <item><c>RunicReforging.GenerateRandomItem</c> and <c>TreasureMapChest.GetRandomItemStat</c> do not exist.</item>
///         <item><c>Titles.AwardKarma</c> kept as-is (it exists in this codebase).</item>
///         <item><c>BaseGump.SendGump</c> became <c>SendGump</c>.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public partial class TradeMinister : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CityItemDonation _donationCrate;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private CityPetDonation _donationPost;

    [Constructible]
    public TradeMinister() : this(City.Britain)
    {
    }

    [Constructible]
    public TradeMinister(City city) : base(AIType.AI_Vendor, FightMode.None)
    {
        _city = city;
        SpeechHue = 0x3B2;
        Female = Utility.RandomDouble() > 0.75;
        Blessed = true;

        Name = RussianNames.RandomPerson(Female);
        Title = "министр торговли";

        Body = Female ? 0x191 : 0x190;
        HairItemID = Race.Human.RandomHair(Female);
        FacialHairItemID = Race.Human.RandomFacialHair(Female);
        HairHue = Race.Human.RandomHairHue();
        Hue = Race.Human.RandomSkinHue();

        SetStr(150);
        SetInt(50);
        SetDex(150);

        AddItem(new Cloak { Hue = 1157 });
        AddItem(new BodySash { Hue = 1326 });
        AddItem(new ChainChest { Hue = 1908 });
        AddItem(new ThighBoots { Hue = 1908 });
        AddItem(new GoldRing());

        Ministers.Add(this);

        CantWalk = true;
    }

    public override bool IsInvulnerable => true;

    public static List<TradeMinister> Ministers { get; } = new();

    public CityLoyaltySystem CitySystem => CityLoyaltySystem.GetCityInstance(_city);

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(Location, 4))
        {
            from.SendGump(new MinisterInfoGump());
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new TradeOrderEntry(from, this));
        list.Add(new TurnInEntry(from, this));
    }

    public Item GiveReward(TradeEntry entry)
    {
        if (0.01 > Utility.RandomDouble())
        {
            return Utility.RandomBool() ? CityBook.GetRandom() : new RewardSign();
        }

        return Utility.RandomBool() ? RandomResource(entry) : ScrollofTranscendence.CreateRandom(1, 10);
    }

    private Item RandomResource(TradeEntry entry)
    {
        var amount = 40;

        if (entry.Details.Count > 1)
        {
            amount = 40 + Utility.RandomMinMax(10, entry.Details.Count * 20);
        }

        return Utility.Random(3) switch
        {
            0 => Utility.Random(9) switch
            {
                0 => new IronIngot(amount),
                1 => new DullCopperIngot(amount),
                2 => new ShadowIronIngot(amount),
                3 => new CopperIngot(amount),
                4 => new BronzeIngot(amount),
                5 => new GoldIngot(amount),
                6 => new AgapiteIngot(amount),
                7 => new VeriteIngot(amount),
                _ => new ValoriteIngot(amount)
            },
            1 => Utility.Random(4) switch
            {
                0 => new Leather(amount),
                1 => new SpinedLeather(amount),
                2 => new HornedLeather(amount),
                _ => new BarbedLeather(amount)
            },
            _ => Utility.Random(7) switch
            {
                0 => new Board(amount),
                1 => new OakBoard(amount),
                2 => new AshBoard(amount),
                3 => new YewBoard(amount),
                4 => new BloodwoodBoard(amount),
                5 => new HeartwoodBoard(amount),
                _ => new FrostwoodBoard(amount)
            }
        };
    }

    private void AfterDeserialization()
    {
        if (CitySystem != null)
        {
            CitySystem.Minister = this;
        }

        if (!Ministers.Contains(this))
        {
            Ministers.Add(this);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        Ministers.Remove(this);
    }

    private class TradeOrderEntry : ContextMenuEntry
    {
        private readonly TradeMinister _minister;
        private readonly Mobile _player;

        public TradeOrderEntry(Mobile player, TradeMinister minister) : base(1114453, 5) // Get Trade Order
        {
            _player = player;
            _minister = minister;
            Enabled = player != null && !CityTradeSystem.HasTrade(player);
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from is PlayerMobile pm && !CityTradeSystem.HasTrade(from))
            {
                from.SendGump(new InternalTradeOrderGump(pm, _minister));
            }
        }
    }

    private class TurnInEntry : ContextMenuEntry
    {
        private readonly TradeMinister _minister;

        public TurnInEntry(Mobile player, TradeMinister minister) : base(1151729, 3) // Turn in a trade order
        {
            _minister = minister;
            Enabled = player != null && CityTradeSystem.HasTrade(player);
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!CityTradeSystem.HasTrade(from))
            {
                return;
            }

            from.Target = new InternalTarget(_minister);
            // Target the trade order you wish to turn in.
            from.SendLocalizedMessage(1151730);
        }

        private class InternalTarget : Target
        {
            private readonly TradeMinister _minister;

            public InternalTarget(TradeMinister minister) : base(-1, false, TargetFlags.None) => _minister = minister;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not TradeOrderCrate order)
                {
                    // That is not a valid trade order. Please try again.
                    from.SendLocalizedMessage(1151731);
                    from.Target = new InternalTarget(_minister);
                    return;
                }

                if (!CityLoyaltySystem.CityTrading.TryTurnIn(from, order, _minister))
                {
                    return;
                }

                if (order.Entry != null && order.Entry.Distance > 0)
                {
                    from.AddToBackpack(_minister.GiveReward(order.Entry));
                    // Your reward has been placed in your backpack.
                    from.SendLocalizedMessage(1073621);
                }

                Titles.AwardKarma(from, 100, true);
            }
        }
    }
}

public class MinisterInfoGump : Gump
{
    public MinisterInfoGump() : base(40, 40)
    {
        AddBackground(0, 0, 500, 400, 9380);

        AddHtmlLocalized(30, 50, 400, 16, 1152962, 0x4800);  // City Trade Minister
        AddHtmlLocalized(30, 80, 440, 320, 1152963, 0x001F); // body
    }
}

public class InternalTradeOrderGump : Gump
{
    private readonly TradeMinister _minister;

    public InternalTradeOrderGump(PlayerMobile user, TradeMinister minister) : base(100, 100)
    {
        _minister = minister;
        User = user;

        AddBackground(0, 0, 370, 420, 9300);

        AddHtmlLocalized(10, 30, 350, 18, 1114513, "#1114454", 0x3442, false, false);

        AddHtmlLocalized(10, 61, 350, 162, 1114455, 0x10, false, false);

        AddHtmlLocalized(10, 225, 350, 90, 1151721, 0x3442, false, false);

        AddButton(10, 390, 4005, 4007, 1);
        AddHtmlLocalized(50, 390, 100, 20, 1049011, 0x10); // I Accept!

        AddButton(330, 390, 4017, 4019, 0);
        AddHtmlLocalized(220, 390, 100, 20, 1114514, "#1006045", 0x4000); // Cancel
    }

    public PlayerMobile User { get; }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1)
        {
            return;
        }

        if (_minister == null || _minister.Deleted || !_minister.InRange(User, 5))
        {
            // You are too far away to do that.
            User.SendLocalizedMessage(500295);
            return;
        }

        CityLoyaltySystem.CityTrading?.TryOfferTrade(User, _minister);
    }
}

[SerializationGenerator(0, false)]
public partial class SlimTheFence : BaseCreature
{
    [Constructible]
    public SlimTheFence() : base(AIType.AI_Vendor, FightMode.None)
    {
        Body = 0x190;
        SpeechHue = 0x3B2;
        Blessed = true;

        Name = "Слим";
        Title = "Скупщик";

        HairItemID = 8264;
        FacialHairItemID = 8269;

        HairHue = 1932;
        FacialHairHue = 1932;
        Hue = Race.Human.RandomSkinHue();

        SetStr(150);
        SetInt(50);
        SetDex(150);

        AddItem(new JinBaori { Hue = 1932 });
        AddItem(new FancyShirt { Hue = 1932 });
        AddItem(new LongPants { Hue = 1 });
        AddItem(new Boots { Hue = 1 });

        CantWalk = true;
    }

    public override bool IsInvulnerable => true;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new TurnInEntry(from, this));
    }

    private class TurnInEntry : ContextMenuEntry
    {
        private readonly SlimTheFence _slim;

        public TurnInEntry(Mobile player, SlimTheFence slim) : base(1151729, 3) // Turn in a trade order
        {
            _slim = slim;
            Enabled = player != null && CityTradeSystem.HasTrade(player);
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (!CityTradeSystem.HasTrade(from))
            {
                return;
            }

            from.Target = new InternalTarget(_slim);
            // Target the trade order you wish to turn in.
            from.SendLocalizedMessage(1151730);
        }

        private class InternalTarget : Target
        {
            private readonly SlimTheFence _slim;

            public InternalTarget(SlimTheFence slim) : base(-1, false, TargetFlags.None) => _slim = slim;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not TradeOrderCrate order)
                {
                    // That is not a valid trade order. Please try again.
                    from.SendLocalizedMessage(1151731);
                    from.Target = new InternalTarget(_slim);
                    return;
                }

                if (!CityLoyaltySystem.CityTrading.TryTurnInToSlim(from, order, _slim))
                {
                    return;
                }

                if (order.Entry != null && order.Entry.Distance > 0)
                {
                    from.AddToBackpack(GiveAward());
                    // Your reward has been placed in your backpack.
                    from.SendLocalizedMessage(1073621);
                }

                Titles.AwardKarma(from, -100, true);
            }
        }
    }

    private static Item GiveAward()
    {
        if (0.05 > Utility.RandomDouble())
        {
            return Utility.Random(4) switch
            {
                0 => new ExpiredVoucher(),
                1 => new ForgedArtwork(),
                2 => new LittleBlackBook(),
                _ => new SlimsShadowVeil()
            };
        }

        return Utility.Random(2) switch
        {
            0 => Loot.RandomArmorOrShieldOrWeaponOrJewelry(),
            _ => ScrollofTranscendence.CreateRandom(1, 10)
        };
    }
}
