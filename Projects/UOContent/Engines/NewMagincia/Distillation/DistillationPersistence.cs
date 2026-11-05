namespace Server.Engines.Distillation;

/// <summary>
///     Хранилище контекстов дистилляции. В ServUO они лежали в
///     <c>Saves/CraftContext/DistillationContexts.bin</c> и писались вручную; здесь используется
///     <see cref="GenericPersistence" /> — тот же приём, что в
///     <c>Engines/CleanUpBritannia/CleanUpBritanniaPersistence.cs</c> и
///     <c>Engines/CityLoyalty/CityLoyaltyPersistence.cs</c>.
/// </summary>
public class DistillationPersistence : GenericPersistence
{
    public DistillationPersistence() : base("Distillation", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => DistillationSystem.SerializeState(writer);

    public override void Deserialize(IGenericReader reader) => DistillationSystem.DeserializeState(reader);
}
