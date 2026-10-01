namespace Server.Systems.MahaonSkills;

/// <summary>
///     Множитель скорости прокачки навыка — единственное место, где мы вмешиваемся в
///     ванильную кривую роста (SkillCheck.CheckSkill, строка с gc *= skill.Info.GainFactor).
///
///     Ремёсла качаются от одной вещи за раз: каждая попытка крафта — это одно испытание
///     навыка, тогда как боец или маг успевает сделать десятки за то же время. Из-за этого
///     ремесленные навыки при одинаковой формуле роста поднимаются несравнимо медленнее
///     всего остального, и именно на это была жалоба.
///
///     Множитель применяется к шансу прироста, а не к его величине: сам прирост остаётся
///     ванильной десятой долей, просто выпадает намного чаще. Когда шанс упирается в
///     единицу (на низком навыке он и так высокий), быстрее уже не станет — прирост идёт
///     с каждой удавшейся вещи, и это потолок для посделочной прокачки.
/// </summary>
public static class MahaonSkillGainRateSystem
{
    /// <summary>Во сколько раз чаще растут ремёсла.</summary>
    public const double CraftRate = 20.0;

    /// <summary>
    ///     Ремёсла в том смысле, в каком их понимает игрок: всё, что делает вещи по
    ///     рецепту. Добыча (горное дело, лесозаготовка, рыбалка) сюда не входит — она
    ///     идёт непрерывно и в ускорении не нуждается.
    /// </summary>
    public static bool IsCraftSkill(SkillName skill) => skill is
        SkillName.Blacksmith or
        SkillName.Tailoring or
        SkillName.Carpentry or
        SkillName.Tinkering or
        SkillName.Alchemy or
        SkillName.Inscribe or
        SkillName.Cooking or
        SkillName.Fletching or
        SkillName.Cartography;

    public static double GetRate(Mobile from, Skill skill) =>
        skill != null && IsCraftSkill(skill.SkillName) ? CraftRate : 1.0;
}
