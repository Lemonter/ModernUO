/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/Ramrod.cs).
///
///     Банник-шомпол: нужен, чтобы забить снаряд в заряженный ствол (<c>TryLoad</c>/<c>TryPrep</c>
///     проверяют его наличие в рюкзаке). Не расходуется.
///
///     Отличия: <c>Name</c> по-русски, cliloc 1095838 не тронут.
/// </summary>
using ModernUO.Serialization;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class Ramrod : Item
{
    [Constructible]
    public Ramrod() : base(0x4246) => Name = "шомпол";

    public override int LabelNumber => 1095838; // ramrod
}
