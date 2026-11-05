namespace Server.Engines.NewMagincia;

/// <summary>
///     Хранилище лотереи участков. В ServUO участки, свободные зоны застройки и очередь сообщений писались
///     внутри предмета-контроллера <c>MaginciaLottoSystem</c>; здесь для этого используется
///     <see cref="GenericPersistence" /> — тот же приём, что в
///     <c>Engines/CleanUpBritannia/CleanUpBritanniaPersistence.cs</c> и
///     <c>Engines/CityLoyalty/CityLoyaltyPersistence.cs</c>.
/// </summary>
public class MaginciaLottoPersistence : GenericPersistence
{
    public MaginciaLottoPersistence() : base("NewMaginciaLotto", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => MaginciaLottoSystem.SerializeState(writer);

    public override void Deserialize(IGenericReader reader) => MaginciaLottoSystem.DeserializeState(reader);
}
