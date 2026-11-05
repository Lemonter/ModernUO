using System;

namespace Server;

/// <summary>
///     Русские имена для случайно названных NPC. Серверный cliloc уже переведён, но списки имён
///     (<c>NameList.RandomName("male")</c> и подобные) остаются английскими — этот класс даёт
///     локализованные наборы для перенесённого контента.
/// </summary>
public static class RussianNames
{
    private static readonly string[] Male =
    {
        "Аскольд", "Богдан", "Борис", "Велизар", "Всеволод", "Гордей", "Григорий", "Добрыня",
        "Еремей", "Зоран", "Иван", "Игнат", "Казимир", "Кузьма", "Лаврентий", "Мирослав",
        "Мстислав", "Олег", "Прохор", "Радимир", "Ратибор", "Родион", "Святослав", "Тихон",
        "Фёдор", "Ярополк", "Ярослав"
    };

    private static readonly string[] Female =
    {
        "Агафья", "Алёна", "Анна", "Василиса", "Вера", "Горислава", "Дарья", "Елена",
        "Забава", "Злата", "Ирина", "Ксения", "Лада", "Любава", "Марина", "Марья",
        "Милана", "Мирослава", "Настасья", "Ольга", "Пелагея", "Рада", "Светлана", "Ульяна",
        "Ярина", "Ярослава"
    };

    private static readonly string[] Lizardman =
    {
        "Ксарр", "Ссарк", "Тхазз", "Шисс", "Врасс", "Кхарр", "Сзит", "Траск"
    };

    public static string RandomMale() => Male[Utility.Random(Male.Length)];

    public static string RandomFemale() => Female[Utility.Random(Female.Length)];

    /// <summary>Имя для NPC, у которого известен только пол.</summary>
    public static string RandomPerson(bool female) => female ? RandomFemale() : RandomMale();

    public static string RandomLizardman() => Lizardman[Utility.Random(Lizardman.Length)];
}
