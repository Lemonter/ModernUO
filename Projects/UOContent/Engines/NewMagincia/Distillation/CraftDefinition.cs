/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/CraftDefinition.cs).
///
///     Один рецепт дистилляции: группа, напиток, список ингредиентов с количествами и время выдержки.
///     Подписи ингредиентов (cliloc) вычисляются один раз при создании описания.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Поля ServUO заменены свойствами только для чтения; список подписей строится в конструкторе через
///             <see cref="Loot.Construct" />, как в оригинале.
///         </item>
///         <item>
///             Для типов, которых нет в таблице, подпись берётся у одноразового экземпляра предмета
///             (<c>Loot.Construct</c>) — тот же приём, что и в ServUO.
///         </item>
///     </list>
/// </summary>
using System;
using Server.Items;

namespace Server.Engines.Distillation;

public class CraftDefinition
{
    public CraftDefinition(
        Group group, Liquor liquor, Type[] ingredients, int[] amounts, TimeSpan maturationDuration
    )
    {
        Group = group;
        Liquor = liquor;
        Ingredients = ingredients;
        Amounts = amounts;
        MaturationDuration = maturationDuration;
        Labels = new int[ingredients.Length];

        for (var i = 0; i < ingredients.Length; i++)
        {
            Labels[i] = GetIngredientLabel(ingredients[i], liquor);
        }
    }

    public Group Group { get; }

    public Liquor Liquor { get; }

    public Type[] Ingredients { get; }

    public int[] Amounts { get; }

    public int[] Labels { get; }

    public TimeSpan MaturationDuration { get; }

    private static int GetIngredientLabel(Type type, Liquor liquor)
    {
        if (type == typeof(Yeast))
        {
            return 1150453;
        }

        if (type == typeof(WheatWort))
        {
            return 1150275;
        }

        if (type == typeof(PewterBowlOfCorn))
        {
            return 1025631;
        }

        if (type == typeof(PewterBowlOfPotatos))
        {
            return 1025634;
        }

        if (type == typeof(TribalBerry))
        {
            return 1040001;
        }

        if (type == typeof(HoneydewMelon))
        {
            return 1023189;
        }

        if (type == typeof(JarHoney))
        {
            return 1022540;
        }

        if (type == typeof(Pitcher))
        {
            // pitcher of wine / pitcher of water
            return liquor == Liquor.Brandy ? 1028091 : 1024088;
        }

        if (type == typeof(Dates))
        {
            return 1025927;
        }

        var item = Loot.Construct(type);

        if (item == null)
        {
            return 0;
        }

        var label = item.LabelNumber;
        item.Delete();

        return label;
    }
}
