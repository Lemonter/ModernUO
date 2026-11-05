/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/CityLoyaltyEntry.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             ServUO's <c>PointsEntry</c> base class does not exist here — this is a plain class owned by
///             <see cref="CityLoyaltyEntry" />'s player and saved through
///             <see cref="CityLoyaltyPersistence" /> instead of the points framework.
///         </item>
///         <item>
///             ServUO's player reward-title system (<c>PlayerMobile.AddRewardTitle</c>/<c>RemoveRewardTitle</c>)
///             has no equivalent in this codebase, so <see cref="Titles" /> and <see cref="CustomTitle" /> are
///             kept as data only; the localized "title bestowed" message is still sent.
///         </item>
///         <item><c>DateTime.UtcNow</c> became <c>Core.Now</c> for the trade-deal deadline.</item>
///         <item><c>BuffInfo.RemoveBuff</c> kept as-is (the buff icon exists here).</item>
///     </list>
/// </summary>
using System;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Engines.CityLoyalty;

[PropertyObject]
public class CityLoyaltyEntry
{
    private bool _utilizing;

    public CityLoyaltyEntry(PlayerMobile pm, City city)
    {
        Player = pm;
        City = city;
        ShowGainMessage = true;
    }

    public PlayerMobile Player { get; private set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Love { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Hate { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Neutrality { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public CityTitle Titles { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsCitizen { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public City City { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string CustomTitle { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool ShowGainMessage { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime TradeDealExpires { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool TradeDealExpired => TradeDealExpires < Core.Now;

    [CommandProperty(AccessLevel.GameMaster)]
    public LoyaltyRating LoyaltyRating => CityLoyaltySystem.GetCityInstance(City)?.GetLoyaltyRating(Player, this) ?? LoyaltyRating.Unknown;

    [CommandProperty(AccessLevel.GameMaster)]
    public bool UtilizingTradeDeal
    {
        get => _utilizing;
        set
        {
            if (Player == null)
            {
                return;
            }

            if (!_utilizing && value)
            {
                TradeDealExpires = Core.Now + TimeSpan.FromHours(CityLoyaltySystem.TradeDealUtilizationPeriod);
            }

            if (_utilizing && !value)
            {
                Player.RemoveBuff(BuffIcon.CityTradeDeal);

                CityLoyaltySystem.RemoveTradeDeal(Player);

                if (Player.NetState != null)
                {
                    // The benefit from your City's Trade Deal has expired! Visit the City Stone to reclaim it.
                    Player.SendLocalizedMessage(1154074);
                }

                Player.Delta(MobileDelta.WeaponDamage);
            }

            _utilizing = value;
        }
    }

    public void DeclareCitizenship()
    {
        IsCitizen = true;
        AddTitle(CityTitle.Citizen);
    }

    public void RenounceCitizenship()
    {
        foreach (var value in Enum.GetValues<CityTitle>())
        {
            Titles &= ~value;
        }

        CustomTitle = null;
        IsCitizen = false;
        Titles = CityTitle.None;
    }

    public virtual void AddTitle(CityTitle title)
    {
        if ((Titles & title) != 0)
        {
            return;
        }

        var loc = CityLoyaltySystem.GetTitleLocalization(Player, title, City);

        Titles |= title;

        // The title "~1_TITLE~" has been bestowed upon you.
        Player.SendLocalizedMessage(1073625, $"#{loc}");
    }

    public void CheckTradeDeal()
    {
        if (TradeDealExpired)
        {
            UtilizingTradeDeal = false;
        }
    }

    public override string ToString() => $"{City} {(IsCitizen ? "[Citizen]" : string.Empty)}";

    public void Persist(IGenericWriter writer)
    {
        writer.Write((int)City);
        writer.Write(Love);
        writer.Write(Hate);
        writer.Write(Neutrality);
        writer.Write((int)Titles);
        writer.Write(IsCitizen);
        writer.Write(CustomTitle);
        writer.Write(ShowGainMessage);
        writer.Write(TradeDealExpires);
        writer.Write(_utilizing);
    }

    public static CityLoyaltyEntry Load(IGenericReader reader)
    {
        var city = (City)reader.ReadInt();
        var love = reader.ReadInt();
        var hate = reader.ReadInt();
        var neutrality = reader.ReadInt();
        var titles = (CityTitle)reader.ReadInt();
        var isCitizen = reader.ReadBool();
        var customTitle = reader.ReadString();
        var showGainMessage = reader.ReadBool();
        var tradeDealExpires = reader.ReadDateTime();
        var utilizing = reader.ReadBool();

        return new CityLoyaltyEntry(null, city)
        {
            Love = love,
            Hate = hate,
            Neutrality = neutrality,
            Titles = titles,
            IsCitizen = isCitizen,
            CustomTitle = customTitle,
            ShowGainMessage = showGainMessage,
            TradeDealExpires = tradeDealExpires
        }.WithUtilizing(utilizing);
    }

    /// <summary>Restores the raw flag without re-running the side effects of the setter.</summary>
    public CityLoyaltyEntry WithUtilizing(bool utilizing)
    {
        _utilizing = utilizing;
        return this;
    }

    public void SetPlayer(PlayerMobile pm) => Player = pm;
}
