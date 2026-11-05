/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Items/Containers.cs).
///
///     Складской ящик с пометкой владельца — служебный контейнер, которым администратор смотрит, что лежит
///     на складе у конкретного игрока.
///
///     Замены:
///     <list type="bullet">
///         <item>Ручной <c>Serialize/Deserialize</c> → <c>[SerializationGenerator]</c>; <c>[Constructible]</c>.</item>
///         <item>
///             Второй класс из этого файла ServUO — <c>Warehouse : LargeCrate</c> — не портирован: он ничего не
///             делал (невидимый ящик, игнорирующий переданного владельца), нигде не использовался, а его
///             <c>Serialize</c> писал только версию.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class WarehouseContainer : MediumCrate
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [Constructible]
    public WarehouseContainer()
    {
    }

    public WarehouseContainer(Mobile owner) => _owner = owner;
}
