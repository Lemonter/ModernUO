/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Items/CityStone.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             The <c>CityLoyaltySystem</c> reference is stored as the <see cref="City" /> enum and resolved through
///             <see cref="CityLoyaltySystem.GetCityInstance" /> — the system object is a singleton built in
///             <c>Configure</c> and must never be serialized directly.
///         </item>
///         <item>
///             <c>ObjectPropertyList</c> became <see cref="IPropertyList" />,
///             <c>GetContextMenuEntries(Mobile, List&lt;…&gt;)</c> became
///             <c>(Mobile, ref PooledRefList&lt;…&gt;)</c>, <c>SimpleContextMenuEntry</c> is provided locally.
///         </item>
///         <item>
///             <c>BaseGump.SendGump(...)</c> became <c>SendGump(...)</c>; the reward-title calls on
///             <c>PlayerMobile</c> have no equivalent here and were dropped (see
///             <see cref="CityLoyaltyEntry" />).
///         </item>
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
using Server.Targeting;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public partial class CityStone : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [SerializableField(1, setter: "private")]
    private List<BallotBox> _boxes;

    [Constructible]
    public CityStone() : this(null)
    {
    }

    public CityStone(CityLoyaltySystem city) : base(0xED4)
    {
        Movable = false;

        if (city != null)
        {
            _city = city.City;
            city.Stone = this;
        }
    }

    public CityLoyaltySystem CitySystem => CityLoyaltySystem.GetCityInstance(_city);

    public override void OnDoubleClick(Mobile from)
    {
        if (!CityLoyaltySystem.Enabled || !CityLoyaltySystem.IsSetup() || from is not PlayerMobile || !from.InRange(Location, 3))
        {
            return;
        }

        if (CitySystem != null && CitySystem.IsCitizen(from))
        {
            from.SendGump(new CityStoneGump(from as PlayerMobile, CitySystem));
        }
        else
        {
            // Only Citizens of this City may use the Election Stone.
            from.SendLocalizedMessage(1153888);
        }
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (CitySystem != null)
        {
            list.Add(1153887, $"#{CityLoyaltySystem.GetCityLocalization(_city)}");
            return;
        }

        base.AddNameProperty(list);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var city = CitySystem;

        if (!CityLoyaltySystem.Enabled || city == null)
        {
            return;
        }

        if (city.GovernorElect != null)
        {
            list.Add(1154066, city.GovernorElect.Name); // Governor-Elect: ~1_NAME~
        }
        else
        {
            // Governor: ~1_NAME~
            list.Add(1154067, city.PendingGovernor ? "#1154102" : city.Governor?.Name ?? "#1154072");
        }

        if (city.Election != null)
        {
            if (city.Election.CanNominate(out var nominationEnd))
            {
                list.Add(1155756, nominationEnd.ToShortDateString()); // Nomination period ends after: ~1_DATE~
            }

            if (city.Election.CanVote(out var voteEnd))
            {
                list.Add(1155757, voteEnd.ToShortDateString()); // Voting Period Ends After: ~1_DATE~
            }
        }

        // City Treasury Balance: ~1_AMT~
        list.Add(1154023, city.Treasury > 0 ? city.Treasury.ToString("N0") : city.Treasury.ToString());

        // Current Trade Deal: ~1_GUILD~
        list.Add(1154059, $"#{(city.ActiveTradeDeal == TradeDeal.None ? 1011051 : (int)city.ActiveTradeDeal - 12)}");

        // Trade Orders Delivered: ~1_val~
        list.Add(1154907, city.CompletedTrades.ToString());
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        var city = CitySystem;

        if (!CityLoyaltySystem.Enabled || city == null)
        {
            return;
        }

        if (!city.IsCitizen(from))
        {
            if (city.Herald != null)
            {
                // Only citizens of ~1_CITY~ may use the City Stone!
                city.Herald.SayTo(from, 1154061, city.Definition.Name);
            }
            else
            {
                from.SendLocalizedMessage(1154061, city.Definition.Name);
            }

            return;
        }

        var governor = city.IsGovernor(from);

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154018, // Grant Citizen Title
                m =>
                {
                    if (!city.IsGovernor(m))
                    {
                        return;
                    }

                    // Which Citizen do you wish to bestow a title?
                    m.SendLocalizedMessage(1154027);
                    m.BeginTarget(
                        10,
                        false,
                        TargetFlags.None,
                        (mob, targeted) =>
                        {
                            if (targeted is not PlayerMobile pm)
                            {
                                // You can only bestow a title on a player!
                                mob.SendLocalizedMessage(1154028);
                                return;
                            }

                            if (!city.IsCitizen(pm))
                            {
                                // You may only bestow a title on citizens of this city!
                                mob.SendLocalizedMessage(1154029);
                                return;
                            }

                            if (mob is PlayerMobile fromPm)
                            {
                                mob.SendGump(new PlayerTitleGump(fromPm, pm, city));
                            }
                        }
                    );
                },
                enabled: governor
            )
        );

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154031, // Open Trade Deal
                m =>
                {
                    if (m is PlayerMobile pm && city.IsGovernor(m))
                    {
                        m.SendGump(new ChooseTradeDealGump(pm, city));
                    }
                },
                enabled: governor
            )
        );

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154277, // Open Inventory
                m =>
                {
                    if (m is PlayerMobile pm && city.IsGovernor(m))
                    {
                        m.SendGump(new OpenInventoryGump(pm, city));
                    }
                },
                enabled: governor
            )
        );

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154278, // Place Ballot Box
                m =>
                {
                    if (!city.IsGovernor(m))
                    {
                        return;
                    }

                    if (_boxes != null && _boxes.Count >= CityLoyaltySystem.MaxBallotBoxes)
                    {
                        m.SendMessage("Вы достигли предельного числа баллотировочных урн в городе.");
                        return;
                    }

                    m.SendMessage("Где вы хотите поставить баллотировочную урну?");
                    m.BeginTarget(
                        3,
                        true,
                        TargetFlags.None,
                        (mob, targeted) =>
                        {
                            if (targeted is not IPoint3D point)
                            {
                                return;
                            }

                            Server.Spells.SpellHelper.GetSurfaceTop(ref point);

                            var box = new BallotBox();

                            if (!CheckLocation(mob, box, point))
                            {
                                box.Delete();
                                return;
                            }

                            box.Movable = false;

                            _boxes ??= new List<BallotBox>();
                            _boxes.Add(box);
                            box.MoveToWorld(new Point3D(point), Map);

                            mob.SendMessage(
                                $"Размещено урн: {_boxes.Count} из {CityLoyaltySystem.MaxBallotBoxes}."
                            );
                        }
                    );
                },
                enabled: governor
            )
        );

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154060, // Utilize Trade Deal
                _ => city.TryUtilizeTradeDeal(from),
                enabled: city.ActiveTradeDeal != TradeDeal.None
            )
        );

        var entry = city.GetPlayerEntry(from);

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154019, // Remove City Title
                m =>
                {
                    if (entry?.CustomTitle == null)
                    {
                        return;
                    }

                    entry.CustomTitle = null;
                    m.SendMessage("Городской титул снят.");
                },
                enabled: entry?.CustomTitle != null
            )
        );

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154068, // Accept Office
                m =>
                {
                    if (m is PlayerMobile pm && m == city.GovernorElect && city.Governor == null)
                    {
                        m.SendGump(new AcceptOfficeGump(pm, city));
                    }
                },
                enabled: city.GovernorElect == from && city.Governor == null &&
                         city.GetLoyaltyRating(from) >= LoyaltyRating.Unknown
            )
        );
    }

    public bool CheckLocation(Mobile m, BallotBox box, IPoint3D p)
    {
        var region = Region.Find(new Point3D(p), Map);

        if (CitySystem?.Definition?.Region == null || !region.IsPartOf(CitySystem.Definition.Region))
        {
            m.SendMessage($"Ставить баллотировочные урны можно только в пределах города {CitySystem?.Definition?.Name}!");
            return false;
        }

        if (box.DropToWorld(m, new Point3D(p)))
        {
            return true;
        }

        m.SendMessage("Здесь нельзя поставить баллотировочную урну!");
        return false;
    }

    private void AfterDeserialization()
    {
        if (CitySystem != null)
        {
            CitySystem.Stone = this;
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        if (CitySystem?.Stone == this)
        {
            CitySystem.Stone = null;
        }
    }
}
