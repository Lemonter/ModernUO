/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Mobiles/CityHerald.cs and GuardCaptain.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item><c>SetWearable</c> became <c>AddItem(new X { Hue = hue })</c>, per this codebase's conventions.</item>
///         <item><c>NameList.RandomName("female")</c> became <see cref="RussianNames" /> so the NPCs are Russian.</item>
///         <item>
///             <c>Race.RandomHair/RandomFacialHair/RandomSkinHue</c> are instance members here, so the static
///             <c>Race.Human.*</c> form is used.
///         </item>
///         <item>
///             The herald's repeating timer is a <see cref="TimerExecutionToken" /> cancelled in
///             <c>OnAfterDelete</c> instead of a raw <c>Timer</c> field.
///         </item>
///         <item>
///             The captain's per-player banner cooldown is a static table, as in ServUO, and is not serialized
///             (ServUO never saved it either).
///         </item>
///         <item><c>ConfirmCallbackGump</c> became <see cref="CityConfirmGump" />.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public partial class CityHerald : BaseCreature
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [SerializableField(1, setter: "private")]
    private string _text;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _announcementExpires;

    private TimerExecutionToken _timerToken;

    [Constructible]
    public CityHerald() : this(City.Britain)
    {
    }

    [Constructible]
    public CityHerald(City city) : base(AIType.AI_Vendor, FightMode.None)
    {
        _city = city;
        SpeechHue = 0x3B2;
        Female = Utility.RandomDouble() > 0.75;
        Blessed = true;

        Name = RussianNames.RandomPerson(Female);
        Title = "городской глашатай";

        Body = Female ? 0x191 : 0x190;
        HairItemID = Race.Human.RandomHair(Female);
        FacialHairItemID = Race.Human.RandomFacialHair(Female);
        HairHue = Race.Human.RandomHairHue();
        Hue = Race.Human.RandomSkinHue();

        SetStr(150);
        SetInt(50);
        SetDex(150);

        AddItem(new Doublet { Hue = 1157 });
        AddItem(new FancyShirt { Hue = 1908 });
        AddItem(new FeatheredHat { Hue = 1157 });
        AddItem(new LongPants { Hue = 1908 });
        AddItem(new Boots { Hue = 2012 });
        AddItem(new GoldRing());

        CantWalk = true;
    }

    public override bool IsInvulnerable => true;

    public CityLoyaltySystem CitySystem => CityLoyaltySystem.GetCityInstance(_city);

    [CommandProperty(AccessLevel.GameMaster)]
    public string Announcement
    {
        get => _text;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _announcementExpires = DateTime.MinValue;
                _timerToken.Cancel();
            }

            if (value != _text)
            {
                _announcementExpires = Core.Now + TimeSpan.FromHours(CityLoyaltySystem.AnnouncementPeriod);

                _timerToken.Cancel();
                Timer.StartTimer(
                    TimeSpan.FromMinutes(3),
                    TimeSpan.FromMinutes(3),
                    DoAnnouncement,
                    out _timerToken
                );
            }

            _text = value;

            if (_text != null)
            {
                DoAnnouncement();
            }
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new DonateGoldEntry(from, this));
    }

    public void DoAnnouncement()
    {
        if (!string.IsNullOrEmpty(_text))
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, 502976); // Hear ye! Hear ye!

            Timer.StartTimer(
                TimeSpan.FromSeconds(3),
                () =>
                {
                    if (!Deleted)
                    {
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, false, _text);
                    }
                }
            );
        }

        if (Core.Now <= _announcementExpires)
        {
            return;
        }

        _announcementExpires = DateTime.MinValue;
        _timerToken.Cancel();
    }

    private void AfterDeserialization()
    {
        if (CitySystem != null)
        {
            CitySystem.Herald = this;
        }

        if (Core.Now < _announcementExpires)
        {
            Timer.StartTimer(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3), DoAnnouncement, out _timerToken);
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _timerToken.Cancel();
    }

    private class DonateGoldEntry : ContextMenuEntry
    {
        private readonly CityHerald _herald;

        public DonateGoldEntry(Mobile player, CityHerald herald) : base(1156237, 3) // Donate Gold
        {
            _herald = herald;

            if (player != null)
            {
                Enabled = player.InRange(herald.Location, 5);
            }
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from.Prompt != null)
            {
                // You already have a text entry request pending.
                from.SendLocalizedMessage(1079166);
                return;
            }

            // Enter amount to deposit:
            from.SendLocalizedMessage(1155865);

            from.BeginPrompt(
                (mob, text) =>
                {
                    var amount = Utility.ToInt32(text);

                    if (amount <= 0 || !Banker.Withdraw(mob, amount))
                    {
                        // The amount entered is invalid. Verify that there are sufficient funds to complete this transaction.
                        mob.SendLocalizedMessage(1155867);
                        return;
                    }

                    CityLoyaltySystem.GetCityInstance(_herald._city)?.AddToTreasury(mob, amount, true);

                    _herald.SayTo(mob, 1152926); // The City thanks you for your generosity!
                },
                (mob, _) => mob.SendLocalizedMessage(1155867)
            );
        }
    }
}

[SerializationGenerator(0, false)]
public partial class GuardCaptain : BaseCreature
{
    private static readonly Dictionary<PlayerMobile, DateTime> _bannerCooldown = new();

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextShout;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BoxOfRopes _box;

    [Constructible]
    public GuardCaptain() : this(City.Britain)
    {
    }

    [Constructible]
    public GuardCaptain(City city) : base(AIType.AI_Vendor, FightMode.None)
    {
        _city = city;
        Female = Utility.RandomDouble() > 0.75;
        Blessed = true;

        Name = RussianNames.RandomPerson(Female);
        Title = "капитан стражи";

        Body = Female ? 0x191 : 0x190;
        HairItemID = Race.Human.RandomHair(Female);
        FacialHairItemID = Race.Human.RandomFacialHair(Female);
        HairHue = Race.Human.RandomHairHue();
        Hue = Race.Human.RandomSkinHue();

        SetStr(150);
        SetInt(50);
        SetDex(150);

        AddItem(new ShortPants { Hue = 1508 });
        AddItem(Female ? new FemaleStuddedChest() : new PlateChest());
        AddItem(new BodySash { Hue = 1326 });
        AddItem(new Halberd());

        CantWalk = true;
    }

    public override bool IsInvulnerable => true;

    public CityLoyaltySystem CitySystem => CityLoyaltySystem.GetCityInstance(_city);

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        var sys = CitySystem;

        if (sys != null && m is PlayerMobile pm && InRange(m.Location, 2))
        {
            var raider = FindRaiderNear(pm);

            if (raider != null)
            {
                sys.AwardLove(m, 1000);

                // Thank you, ~1_name~, for your assistance during these difficult times.
                SayTo(m, 1152250, m.Name);
                raider.Delete();
            }
        }

        base.OnMovement(m, oldLocation);
    }

    private Raider FindRaiderNear(PlayerMobile pm)
    {
        foreach (var follower in pm.AllFollowers)
        {
            if (follower is Raider raider && raider.InRange(Location, 2))
            {
                return raider;
            }
        }

        return null;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        if (from is PlayerMobile pm)
        {
            list.Add(new InternalEntry(pm, this));
        }
    }

    public override void OnThink()
    {
        base.OnThink();

        if (_nextShout >= Core.Now)
        {
            return;
        }

        Say(Utility.Random(1152252, 6));
        _nextShout = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(45, 65));
    }

    public void AddToCooldown(PlayerMobile pm) =>
        _bannerCooldown[pm] = Core.Now + TimeSpan.FromHours(CityLoyaltySystem.BannerCooldownDuration);

    public bool IsInBannerCooldown(PlayerMobile m) =>
        _bannerCooldown.TryGetValue(m, out var next) && next > Core.Now;

    public void CheckBannerCooldown()
    {
        if (_bannerCooldown.Count == 0)
        {
            return;
        }

        var stale = new List<PlayerMobile>();

        foreach (var (pm, next) in _bannerCooldown)
        {
            if (next < Core.Now)
            {
                stale.Add(pm);
            }
        }

        foreach (var pm in stale)
        {
            _bannerCooldown.Remove(pm);
        }
    }

    public void PurchaseBanner(Mobile m)
    {
        if (m is not PlayerMobile pm)
        {
            return;
        }

        if (!Banker.Withdraw(pm, CityLoyaltySystem.BannerCost))
        {
            // I am afraid your bank box does not contain the funds needed to complete this transaction.
            SayTo(pm, 1152302);
            return;
        }

        var deed = new CityBannerDeed(_city);
        CitySystem?.AddToTreasury(m, CityLoyaltySystem.BannerCost);

        if (pm.Backpack == null || !pm.Backpack.TryDropItem(pm, deed, false))
        {
            pm.BankBox?.DropItem(deed);
            pm.SendMessage("Документ положен в ваш банковский ящик.");
        }
        else
        {
            pm.SendMessage("Документ положен в ваш рюкзак.");
        }

        AddToCooldown(pm);
    }

    private void AfterDeserialization()
    {
        if (CitySystem != null)
        {
            CitySystem.Captain = this;
        }
    }

    public class InternalEntry : ContextMenuEntry
    {
        public InternalEntry(PlayerMobile from, GuardCaptain guard) : base(1152366, 3) // City Banner
        {
            Player = from;
            Guard = guard;

            Enabled = CityLoyaltySystem.HasCitizenship(from, guard._city);
        }

        public PlayerMobile Player { get; }
        public GuardCaptain Guard { get; }

        public override void OnClick(Mobile from, IEntity target)
        {
            var thisSystem = CityLoyaltySystem.GetCityInstance(Guard._city);
            var theirSystem = CityLoyaltySystem.GetCitizenship(Player);

            Guard.CheckBannerCooldown();

            if (theirSystem == null || thisSystem == null || !CityLoyaltySystem.HasCitizenship(Player, Guard._city))
            {
                return;
            }

            if (Guard.IsInBannerCooldown(Player))
            {
                // I have quite a backlog of orders and I cannot satisfy your request for a ~1_ITEM~ right now.
                Guard.SayTo(Player, 1152364, $"#{CityLoyaltySystem.BannerLocalization(thisSystem.City)}");
                return;
            }

            if (theirSystem.GetLoyaltyRating(Player) < LoyaltyRating.Adored)
            {
                // I apologize, but you are not well-enough renowned in the city of ~1_CITY~ to make this purchase.
                Guard.SayTo(Player, 1152363, $"#{CityLoyaltySystem.GetCityLocalization(thisSystem.City)}");
                return;
            }

            var args =
                $"#{CityLoyaltySystem.BannerLocalization(thisSystem.City)}\t{CityLoyaltySystem.BannerCost:N0}";

            Player.SendGump(new BannerPurchaseGump(Player, Guard, args));
        }
    }
}

/// <summary>Confirmation for buying a city banner; runs the captain's purchase on accept.</summary>
public class BannerPurchaseGump : Gump
{
    private readonly GuardCaptain _guard;

    public BannerPurchaseGump(Mobile from, GuardCaptain guard, string args) : base(100, 100)
    {
        _guard = guard;

        AddBackground(0, 0, 420, 160, 5054);

        // I apologize, but you are not well-enough renowned... / cost table
        AddHtmlLocalized(20, 20, 380, 90, 1152365, args, 0xFFFF, false, false);

        AddButton(150, 120, 4005, 4007, 1);
        AddHtmlLocalized(185, 122, 120, 20, 1049011, 0xFFFF); // I Accept!

        AddButton(20, 120, 4017, 4019, 0);
        AddHtmlLocalized(55, 122, 120, 20, 1006045, 0xFFFF); // Cancel
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1)
        {
            return;
        }

        var from = sender.Mobile;

        if (_guard == null || _guard.Deleted || !_guard.InRange(from, 5))
        {
            // You are too far away to do that.
            from.SendLocalizedMessage(500295);
            return;
        }

        _guard.PurchaseBanner(from);
    }
}
