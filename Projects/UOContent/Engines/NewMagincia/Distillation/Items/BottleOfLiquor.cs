/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/Items/BottleOfLiquor.cs).
///
///     Замены:
///     <list type="bullet">
///         <item><c>ObjectPropertyList</c> → <see cref="IPropertyList" />.</item>
///         <item>
///             Вместо <c>String.Format("#{0}", …)</c> используется интерполяция прямо в аргументе — так строка
///             попадает в обработчик без промежуточной аллокации.
///         </item>
///         <item>
///             Версия 1 ServUO существовала ради поля <c>IsStrong</c>, добавленного позже; здесь класс новый и
///             сразу пишет все четыре поля в версии 0.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.Distillation;

[SerializationGenerator(0, false)]
public partial class BottleOfLiquor : BeverageBottle
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Liquor _liquor;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private string _label;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _isStrong;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Mobile _distiller;

    [Constructible]
    public BottleOfLiquor() : this(Liquor.Whiskey, null, false, null)
    {
    }

    [Constructible]
    public BottleOfLiquor(Liquor liquor, string label, bool isStrong, Mobile distiller) : base(BeverageType.Liquor)
    {
        Quantity = MaxQuantity;
        _liquor = liquor;
        _label = label;
        _isStrong = isStrong;
        _distiller = distiller;
    }

    public override bool ShowQuantity => false;

    public override void AddNameProperty(IPropertyList list)
    {
        if (!string.IsNullOrEmpty(_label))
        {
            list.Add(1049519, _label); // a bottle of ~1_DRINK_NAME~
            return;
        }

        list.Add(1049519, $"#{DistillationSystem.GetLabel(_liquor, _isStrong)}"); // a bottle of ~1_DRINK_NAME~
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_liquor != Liquor.None)
        {
            // Liquor Type: ~1_TYPE~
            list.Add(1150454, $"#{DistillationSystem.GetLabel(_liquor, _isStrong)}");
        }

        if (_distiller != null)
        {
            list.Add(1150679, _distiller.Name); // Distiller: ~1_NAME~
        }

        list.Add(GetQuantityDescription());
    }
}
