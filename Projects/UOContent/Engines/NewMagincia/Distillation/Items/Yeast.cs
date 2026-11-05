/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/Items/Yeast.cs).
///
///     Замены:
///     <list type="bullet">
///         <item>Сеттер сопротивления ограничивает значение 1..5 через <c>allowFieldChange</c>.</item>
///         <item><c>ObjectPropertyList</c> → <see cref="IPropertyList" />.</item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Yeast : Item
{
    [SerializableField(0, allowFieldChange: nameof(AllowBacterialResistanceChange))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private int _bacterialResistance;

    [Constructible]
    public Yeast() : base(3624)
    {
        Hue = 2418;

        var roll = Utility.Random(100);

        if (roll <= 5)
        {
            _bacterialResistance = 5;
        }
        else if (roll <= 10)
        {
            _bacterialResistance = 4;
        }
        else if (roll <= 20)
        {
            _bacterialResistance = 3;
        }
        else if (roll <= 40)
        {
            _bacterialResistance = 2;
        }
        else
        {
            _bacterialResistance = 1;
        }
    }

    [Constructible]
    public Yeast(int resistance) : base(3624)
    {
        BacterialResistance = resistance;
        Hue = 2418;
    }

    public override int LabelNumber => 1150453; // yeast

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1150455, GetResistanceLabel()); // Bacterial Resistance: ~1_VAL~
    }

    private bool AllowBacterialResistanceChange(ref int value)
    {
        value = Math.Clamp(value, 1, 5);
        return true;
    }

    private string GetResistanceLabel() => _bacterialResistance switch
    {
        4 => "+",
        3 => "+-",
        2 => "-",
        1 => "--",
        _ => "++"
    };
}
