/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/DistillationContext.cs).
///
///     Состояние окна дистилляции одного игрока. Сам по себе класс не сериализуемый сущностью: в ServUO он
///     писался вручную в <c>Saves/CraftContext/DistillationContexts.bin</c>, здесь — в
///     <see cref="DistillationPersistence" /> (см. <see cref="DistillationSystem.SerializeState" />).
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Выбранные дрожжи — это ссылки на живые предметы, они намеренно не сохраняются (как и в ServUO:
///             выбор существует только на время работы с окном).
///         </item>
///         <item>Поля заменены свойствами; <c>m_Label</c> инициализируется <c>null</c> по умолчанию.</item>
///     </list>
/// </summary>
using System;
using Server.Items;

namespace Server.Engines.Distillation;

public class DistillationContext
{
    private readonly Yeast[] _selectedYeast = new Yeast[4];

    public DistillationContext()
    {
        LastGroup = Group.WheatBased;
        LastLiquor = Liquor.None;
        Mark = true;
    }

    public Group LastGroup { get; set; } = Group.WheatBased;

    public Liquor LastLiquor { get; set; } = Liquor.None;

    public Yeast[] SelectedYeast => _selectedYeast;

    public bool MakeStrong { get; set; }

    public bool Mark { get; set; }

    public string Label { get; set; }

    public bool YeastInUse(Yeast yeast)
    {
        foreach (var selected in _selectedYeast)
        {
            if (selected != null && selected == yeast)
            {
                return true;
            }
        }

        return false;
    }

    public void ClearYeasts()
    {
        for (var i = 0; i < _selectedYeast.Length; i++)
        {
            _selectedYeast[i] = null;
        }
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write((int)LastGroup);
        writer.Write((int)LastLiquor);
        writer.Write(MakeStrong);
        writer.Write(Mark);
        writer.Write(Label);
    }

    public static DistillationContext Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        return new DistillationContext
        {
            LastGroup = (Group)reader.ReadInt(),
            LastLiquor = (Liquor)reader.ReadInt(),
            MakeStrong = reader.ReadBool(),
            Mark = reader.ReadBool(),
            Label = reader.ReadString()
        };
    }
}
