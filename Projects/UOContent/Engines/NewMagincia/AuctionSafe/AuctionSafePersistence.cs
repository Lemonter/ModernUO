/// <summary>
///     Хранилище аукционов на сейфах. В ServUO аукцион был полем предмета <c>AuctionSafe</c> и писался внутри
///     него; здесь граф пишет отдельный файл — тот же приём, что в
///     <c>Engines/NewMagincia/Bazaar/Core/BazaarPersistence.cs</c>,
///     <c>Engines/NewMagincia/HousingLotto/Core/MaginciaLottoPersistence.cs</c> и
///     <c>Engines/CityLoyalty/CityLoyaltyPersistence.cs</c>. Раскладка полей описана в <see cref="AuctionStore" />.
/// </summary>
namespace Server.Engines.Auction;

public class AuctionSafePersistence : GenericPersistence
{
    public AuctionSafePersistence() : base("AuctionSafe", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => AuctionStore.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => AuctionStore.Deserialize(reader);
}
