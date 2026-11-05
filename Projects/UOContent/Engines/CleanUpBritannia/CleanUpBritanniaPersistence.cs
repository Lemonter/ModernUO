namespace Server.Engines.CleanUpBritannia;

/// <summary>
///     Хранилище очков Clean Up Britannia. В ServUO их сохранял общий
///     <c>Server.Engines.Points.PointsSystem</c>; здесь для этого используется
///     <see cref="GenericPersistence" /> с собственным файлом сохранения.
/// </summary>
public class CleanUpBritanniaPersistence : GenericPersistence
{
    public CleanUpBritanniaPersistence() : base("CleanUpBritannia", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => CleanUpBritanniaData.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => CleanUpBritanniaData.Deserialize(reader);
}
