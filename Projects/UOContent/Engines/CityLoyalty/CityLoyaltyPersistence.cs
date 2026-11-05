/// <summary>
///     Serializer for the City Loyalty system. ServUO persisted all of this through
///     <c>Server.Engines.Points.PointsSystem</c>; that framework does not exist here, so this
///     <see cref="GenericPersistence" /> — the same pattern as
///     <c>Engines/CleanUpBritannia/CleanUpBritanniaPersistence.cs</c> and
///     <c>Engines/VvV/VvVPersistence.cs</c> — owns the file instead.
///
///     Per-city state (governor, treasury, trade deal, election, citizen wait list) and every player's loyalty
///     entry are written here. Per-mobile loyalty is deliberately *not* a field on <c>PlayerMobile</c>: a player
///     accumulates an entry for every one of the nine cities, so a single global file keyed by city is simpler
///     than nine parallel arrays on each mobile and keeps the whole system in one place.
/// </summary>
namespace Server.Engines.CityLoyalty;

public class CityLoyaltyPersistence : GenericPersistence
{
    public CityLoyaltyPersistence() : base("CityLoyalty", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => CityLoyaltySystem.SerializeState(writer);

    public override void Deserialize(IGenericReader reader) => CityLoyaltySystem.DeserializeState(reader);
}
