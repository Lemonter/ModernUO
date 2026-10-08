using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Systems.MahaonCombat;

namespace Server.Items;

/// <summary>
///     Свиток силы для мастерок — то же, что обычный PowerScroll, но поднимает потолок не
///     навыка, а мастерки (школы магии, воинского стиля, ремесленной или воровской
///     специализации).
///
///     Мастерки у нас упирались в общую константу MaxValue = 100 во всех двух десятках
///     систем сразу — персональный потолок и всё, что с ним связано, живёт в
///     MahaonMasteryCapSystem, а тут только предмет, который его поднимает.
///
///     Как и PowerScroll.CreateRandom, цель выбирается случайно — из тех мастерок, которые
///     игрок уже начал качать и у которых потолок ниже значения свитка. Список берём из
///     того же снимка дерева навыков, что уходит в клиентский гамп, чтобы не заводить
///     второй реестр мастерок рядом с существующим.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonMasteryScroll : Item
{
    [SerializableField(0)]
    private double _value;

    [Constructible]
    public MahaonMasteryScroll(double value = 105.0) : base(0x14F0)
    {
        _value = value;
        Hue = 0x48F;
        Weight = 1.0;
        LootType = LootType.Regular;
    }

    public override string DefaultName => $"свиток мастерства ({_value:F0})";

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Поднимает потолок случайной мастерки до {_value:F0}");
        list.Add("Работает только на мастерки твоей профессии");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage(0x22, "Свиток должен быть у тебя в рюкзаке.");
            return;
        }

        var candidates = FindCandidates(from, _value);

        if (candidates.Count == 0)
        {
            from.SendMessage(
                0x22,
                "Нет ни одной мастерки, потолок которой этот свиток мог бы поднять. Свиток берёт только мастерки твоего пути: до 120 — своей категории, до 110 — второй, чужие не берёт вовсе."
            );
            return;
        }

        var name = candidates.RandomElement();

        if (!MahaonMasteryCapSystem.RaiseCap(from, name, _value))
        {
            from.SendMessage(0x22, "Потолок этой мастерки уже не ниже.");
            return;
        }

        from.SendMessage(0x59, $"Потолок мастерки «{name}» поднят до {_value:F0}.");
        from.PlaySound(0x1F7);
        from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);

        Delete();
    }

    /// <summary>
    ///     Мастерки, которые этот свиток может поднять: уже начатые (значение больше нуля)
    ///     и с потолком ниже значения свитка. Подкатегории с IsUsableSkillList — это
    ///     обычные списки навыков в дереве, а не мастерки, их пропускаем.
    /// </summary>
    public static List<string> FindCandidates(Mobile from, double value) => FindCandidates(from, value, out _);

    // В пределах стольких очков от потолка мастерка считается упёршейся в него.
    private const double CappedMargin = 5.0;

    /// <param name="capped">Все найденные упёрлись в свой потолок — им свиток нужнее всего,
    /// поэтому при наличии таких возвращаются только они.</param>
    public static List<string> FindCandidates(Mobile from, double value, out bool capped)
    {
        var result = new List<string>();
        var pressing = new List<string>();

        foreach (var category in MahaonSkillTree.GetTreeSnapshot(from))
        {
            foreach (var sub in category.SubCategories)
            {
                if (sub.IsUsableSkillList)
                {
                    continue;
                }

                // Один раз на подкатегорию: GetCeiling по имени мастерки строит снимок
                // всего дерева заново, а мы уже внутри этого снимка.
                var ceiling = Systems.MahaonProfessions.ProfessionMasteryRules.CeilingFor(
                    from, Systems.MahaonProfessions.ProfessionMasteryRules.CategoryOfSubCategory(sub.Name)
                );

                if (ceiling < value)
                {
                    continue; // чужая мастерка — этот свиток её не возьмёт
                }

                foreach (var entry in sub.Entries)
                {
                    var cap = MahaonMasteryCapSystem.GetCap(from, entry.Name);
                    if (entry.Value > 0 && cap < value)
                    {
                        result.Add(entry.Name);
                        if (entry.Value >= cap - CappedMargin)
                        {
                            pressing.Add(entry.Name);
                        }
                    }
                }
            }
        }

        capped = pressing.Count > 0;
        return capped ? pressing : result;
    }
}
