/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/Swab.cs).
///
///     Банник с ветошью: единственный способ вычистить ствол после выстрела. В ServUO каждая пушка
///     считает «грязные» выстрелы (<c>CheckDirty</c> — после десяти ствол требует чистки); чистая
///     пушка стрелять не обязана, но грязная не принимает новый заряд, пока её не вычистят.
///
///     Отличия: <c>Name</c> задан по-русски (в ServUO имени не было вовсе — только арт).
/// </summary>
using ModernUO.Serialization;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class Swab : Item
{
    [Constructible]
    public Swab() : base(Utility.RandomMinMax(16968, 16969)) => Name = "банник";
}
