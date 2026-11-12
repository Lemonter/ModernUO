using System;
using Server.Systems.MahaonWorld;

namespace Server.Systems.MahaonFarming;

/// <summary>
///     Навык Земледелия (SkillName.Farming) — всё, что он решает на грядках, в одном месте.
///
///     До навыка урожай был одинаковым у всех: новичок и человек, проживший в поле год,
///     снимали с грядки ровно шесть штук, и мандрагору мог вырастить кто угодно. Поэтому
///     реагенты с полей ничего не стоили, а у фермера не было причины быть фермером.
///
///     Навык делает три вещи:
///     — порог культуры: сеять и собирать редкое можно только с нужным навыком, иначе
///       женьшень и мандрагора перестают быть делом специалиста;
///     — объём: сколько снимается с грядки (пашня по-прежнему удваивает это сверху);
///     — удача работы: посев может не взойти, вспашка — не получиться, сбор — выйти
///       побитым. Это же и источник прокачки: каждое действие — проверка навыка.
///
///     Середина шкалы (50) даёт ровно прежние шесть штук, чтобы мир не сломался для тех,
///     кто фермерствовал до навыка.
/// </summary>
public static class MahaonFarmingSkill
{
    /// <summary>Урожай на нулевом навыке.</summary>
    private const int BaseYield = 3;

    /// <summary>Сколько урожая добавляют 100 очков навыка сверх базы.</summary>
    private const int YieldPerHundred = 6;

    /// <summary>
    ///     Какой навык нужен, чтобы сеять и собирать культуру.
    ///
    ///     Привычная еда и волокно — всем. Тыквы и чеснок — тем, кто уже чем-то занимался в
    ///     поле. Дыни и виноград капризнее. Реагенты (женьшень, мандрагора, паслён) —
    ///     только опытному: ради них навык и затевался.
    /// </summary>
    public static double RequiredSkill(MahaonCropType crop) => crop switch
    {
        MahaonCropType.Pumpkin or MahaonCropType.Squash or MahaonCropType.YellowGourd
            or MahaonCropType.GreenGourd or MahaonCropType.Garlic => 20.0,
        MahaonCropType.Watermelon or MahaonCropType.HoneydewMelon or MahaonCropType.Cantaloupe => 30.0,
        MahaonCropType.Grapes                                                                 => 45.0,
        MahaonCropType.Ginseng                                                                => 55.0,
        MahaonCropType.Mandrake or MahaonCropType.Nightshade                                  => 70.0,
        _                                                                                     => 0.0
    };

    public static double Value(Mobile m) => m.Skills[SkillName.Farming].Value;

    public static bool CanWork(Mobile m, MahaonCropType crop) => Value(m) >= RequiredSkill(crop);

    /// <summary>Сообщение для того, кто не дотягивает до культуры. Общее для посева и сбора.</summary>
    public static void SendTooHard(Mobile m, MahaonCropType crop)
    {
        var name = MahaonCropTable.Data[(int)crop].NameRu;
        m.SendMessage(0x22, $"Ты не знаешь, как обращаться с этим ({name}): нужно земледелие {RequiredSkill(crop):0}.");
    }

    /// <summary>
    ///     Сколько снимется с грядки при удачном сборе, без пашни.
    ///     0 → 3, 50 → 6 (как было до навыка), 100 → 9, 120 → 10.
    /// </summary>
    public static int Yield(Mobile m) => BaseYield + (int)(Value(m) * YieldPerHundred / 100.0);

    /// <summary>
    ///     Проверка при сборе. Неудача не отнимает урожай целиком, а портит половину:
    ///     грядку ты уже вырастил, и уйти с пустыми руками было бы слишком жестоко.
    ///     Диапазон ±50 от порога: ровно на пороге — пятьдесят на пятьдесят.
    /// </summary>
    public static bool CheckHarvest(Mobile m, MahaonCropType crop)
    {
        var req = RequiredSkill(crop);
        return m.CheckSkill(SkillName.Farming, req - 50.0, req + 50.0);
    }

    /// <summary>
    ///     Проверка при посеве. Неудача — семя пропало. Диапазон смещён вверх меньше, чем
    ///     вниз: на пороге шанс около 60%, а через 30 очков посев уже не срывается.
    /// </summary>
    public static bool CheckSow(Mobile m, MahaonCropType crop)
    {
        var req = RequiredSkill(crop);
        return m.CheckSkill(SkillName.Farming, req - 50.0, req + 30.0);
    }

    /// <summary>
    ///     Проверка при вспашке. Пахать умеет почти всякий, поэтому шанс высокий и с нуля
    ///     (около 55%), а к 40 навыка вспашка не срывается вовсе.
    /// </summary>
    public static bool CheckPlough(Mobile m) => m.CheckSkill(SkillName.Farming, -50.0, 40.0);

    /// <summary>Урожай после неудачной проверки: половина, но хоть что-то.</summary>
    public static int Spoiled(int amount) => Math.Max(1, amount / 2);
}
