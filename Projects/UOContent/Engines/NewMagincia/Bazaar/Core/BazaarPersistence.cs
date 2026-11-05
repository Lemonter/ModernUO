namespace Server.Engines.NewMagincia;

/// <summary>
///     Хранилище Новой Маджинции: прилавки, их аукционы, ставки «на первый свободный», ответные ставки и
///     склад невостребованных вещей. В ServUO всё это писалось внутри предмета-контроллера
///     <c>MaginciaBazaar</c>; здесь используется <see cref="GenericPersistence" /> — тот же приём, что в
///     <c>Engines/CleanUpBritannia/CleanUpBritanniaPersistence.cs</c>,
///     <c>Engines/CityLoyalty/CityLoyaltyPersistence.cs</c> и
///     <c>Engines/NewMagincia/HousingLotto/Core/MaginciaLottoPersistence.cs</c>.
/// </summary>
public class BazaarPersistence : GenericPersistence
{
    public BazaarPersistence() : base("NewMaginciaBazaar", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => MaginciaBazaar.SerializeState(writer);

    public override void Deserialize(IGenericReader reader) => MaginciaBazaar.DeserializeState(reader);
}
