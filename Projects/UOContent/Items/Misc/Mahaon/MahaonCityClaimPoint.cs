using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Guilds;
using Server.Mobiles;
using Server.Systems.MahaonCities;

namespace Server.Items;

/// <summary>
/// The banner a city is claimed at, placed by a GM. A guild leader standing by it claims the city
/// with [ClaimCity; the city's guards and the holding guild then come to the banner, and if the
/// claimants still stand by it after ten minutes the city changes hands. Should every claimant by
/// the banner be killed or leave, the claim fails.
/// </summary>
[SerializationGenerator(0)]
public partial class MahaonCityClaimPoint : Item
{
    public static readonly TimeSpan HoldTime = TimeSpan.FromMinutes(10);

    /// <summary>How close to the banner a claimant must stand to hold it.</summary>
    public const int HoldRange = 12;

    /// <summary>How close the leader must be to lay the claim.</summary>
    public const int ClaimRange = 5;

    private static readonly TimeSpan WatchInterval = TimeSpan.FromSeconds(5);

    private static readonly Dictionary<string, MahaonCityClaimPoint> _byCity = new(StringComparer.OrdinalIgnoreCase);

    [SerializableField(0, allowFieldChange: nameof(AllowCityChange), fieldChanged: nameof(OnCityChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _city;

    [SerializableField(1, setter: "private")]
    private Guild _contender;

    [SerializableField(2, setter: "private")]
    [DeserializeTimer(nameof(DeserializeHoldTimer))]
    private Timer _holdTimer;

    private Timer _watchTimer;

    [Constructible]
    public MahaonCityClaimPoint(string city = null) : base(0x15AE)
    {
        Movable = false;
        _city = city;
        Name = "знамя города";
        Register();
    }

    /// <summary>The banner of a city, if a GM placed one.</summary>
    public static MahaonCityClaimPoint Of(string city) =>
        city != null && _byCity.TryGetValue(city, out var point) && !point.Deleted ? point : null;

    public bool Contested => _contender != null && _holdTimer?.Running == true;

    // A banner moved to another city while a claim is on would leave that claim nowhere.
    private bool AllowCityChange(ref string value) => !Contested && (value == null || CityControlSystem.Cities.ContainsKey(value));

    private void OnCityChanged(string oldValue, string newValue)
    {
        if (oldValue != null && _byCity.TryGetValue(oldValue, out var point) && point == this)
        {
            _byCity.Remove(oldValue);
        }

        Register();
    }

    private void Register()
    {
        if (_city != null)
        {
            _byCity[_city] = this;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Register();

        if (Contested)
        {
            RallyGuards();
            _watchTimer = Timer.DelayCall(WatchInterval, WatchInterval, Watch);
        }
    }

    private void DeserializeHoldTimer(TimeSpan delay) => _holdTimer = Timer.DelayCall(delay, Finish);

    /// <summary>Whether this guild's members still stand by the banner, alive.</summary>
    public int ClaimantsNear()
    {
        if (_contender == null || Map == null || Map == Map.Internal)
        {
            return 0;
        }

        var count = 0;
        foreach (var m in Map.GetMobilesInRange<PlayerMobile>(Location, HoldRange))
        {
            if (m.Alive && m.Guild == _contender)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Lays a guild's claim. The caller has checked who may claim; this starts the hold, calls the
    /// guards and tells the world.
    /// </summary>
    public bool StartContest(Guild guild)
    {
        if (Contested || guild == null || _city == null)
        {
            return false;
        }

        Contender = guild;
        HoldTimer = Timer.DelayCall(HoldTime, Finish);
        _watchTimer = Timer.DelayCall(WatchInterval, WatchInterval, Watch);

        RallyGuards();

        if (CityControlSystem.GetController(_city) is { } holder)
        {
            World.Broadcast(0x22, false, $"Гильдия {guild.Name} оспаривает {_city} у гильдии {holder.Name}! Защитники, к знамени!");
        }
        else
        {
            World.Broadcast(0x22, false, $"Гильдия {guild.Name} заявила права на {_city}! Десять минут у знамени — и город её.");
        }

        Systems.MahaonBots.BotRumors.Spread($"Гильдия {guild.Name} пытается взять {_city}.");
        return true;
    }

    private void RallyGuards()
    {
        foreach (var guard in CityGuard.Of(_city))
        {
            guard.RallyTo(Location);
        }
    }

    private void Watch()
    {
        if (!Contested || ClaimantsNear() == 0)
        {
            Fail();
        }
    }

    private void Finish()
    {
        var guild = _contender;
        if (guild == null || ClaimantsNear() == 0)
        {
            Fail();
            return;
        }

        EndContest();
        CityControlSystem.Capture(_city, guild);
        World.Broadcast(0x59, false, $"{_city} переходит под руку гильдии {guild.Name}!");
        Systems.MahaonBots.BotRumors.Spread($"Гильдия {guild.Name} взяла {_city}.");
    }

    private void Fail()
    {
        var guild = _contender;
        EndContest();

        if (guild != null)
        {
            World.Broadcast(0x59, false, $"Попытка гильдии {guild.Name} взять {_city} провалилась.");
        }
    }

    private void EndContest()
    {
        _holdTimer?.Stop();
        HoldTimer = null;
        _watchTimer?.Stop();
        _watchTimer = null;
        Contender = null;

        foreach (var guard in CityGuard.Of(_city))
        {
            guard.ReturnToPost(CityControlSystem.GuardPatrolRadius);
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        if (Contested)
        {
            LabelTo(from, $"{_city}: город оспаривает {_contender.Name}");
        }
        else if (_city != null)
        {
            LabelTo(from, _city);
        }
    }

    public override void OnDelete()
    {
        if (Contested)
        {
            EndContest();
        }

        if (_city != null && _byCity.TryGetValue(_city, out var point) && point == this)
        {
            _byCity.Remove(_city);
        }

        base.OnDelete();
    }
}
