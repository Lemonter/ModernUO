using System;
using Server.Items;

namespace Server.Engines.Craft;

/// <summary>
///     Ступка травника: из собранных трав — мази, шины, противоядия и отвары.
///
///     Отдельная система, а не раздел алхимии: навык другой (Травничество), сырьё своё, и
///     алхимику не должно доставаться это ремесло вместе с его зельями. Названия групп и
///     рецептов — строками, у шарда своих clilocs на это нет.
/// </summary>
public class DefHerbalism : CraftSystem
{
    private DefHerbalism() : base(1, 1, 1.25)
    {
    }

    public static CraftSystem CraftSystem { get; private set; }

    public override SkillName MainSkill => SkillName.Herbalism;

    public override TextDefinition GumpTitle { get; } = "<CENTER>СТУПКА ТРАВНИКА</CENTER>";

    public static void Initialize()
    {
        CraftSystem ??= new DefHerbalism();
    }

    public override double GetChanceAtMin(CraftItem item) => 0.0;

    public override int CanCraft(Mobile from, BaseTool tool, Type itemType)
    {
        if (tool?.Deleted != false || tool.UsesRemaining < 0)
        {
            return 1044038; // You have worn out your tool!
        }

        if (!BaseTool.CheckAccessible(tool, from))
        {
            return 1044263; // The tool must be on your person to use.
        }

        return 0;
    }

    public override void PlayCraftEffect(Mobile from)
    {
        from.PlaySound(0x242); // ступка — тот же звук, что у алхимика
    }

    public override int PlayEndingEffect(
        Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality,
        bool makersMark, CraftItem item
    )
    {
        if (toolBroken)
        {
            from.SendLocalizedMessage(1044038); // You have worn out your tool
        }

        if (failed)
        {
            return 1044043; // You failed to create the item, and some of your materials are lost.
        }

        return 1044154; // You create the item.
    }

    public override void InitCraftList()
    {
        const string healing = "Лечебные";
        const string brews = "Отвары";

        // Порог рецепта ниже порога самой редкой его травы: собрать траву — одно умение,
        // приготовить из неё — другое, и сварить можно и из купленного у собирателя.
        var index = AddCraft(
            typeof(MahaonBloodstopSalve), healing, "кровоостанавливающая мазь", 0.0, 50.0,
            typeof(MahaonPlantain), "подорожник", 2, "Тебе не хватает подорожника."
        );
        AddRes(index, typeof(MahaonYarrow), "тысячелистник", 1, "Тебе не хватает тысячелистника.");

        index = AddCraft(
            typeof(MahaonComfreySplint), healing, "шина с окопником", 25.0, 75.0,
            typeof(MahaonComfrey), "окопник", 2, "Тебе не хватает окопника."
        );
        AddRes(index, typeof(Bandage), "бинт", 1, "Тебе нужен бинт.");

        index = AddCraft(
            typeof(MahaonHerbalAntidote), healing, "противоядие травника", 30.0, 80.0,
            typeof(MahaonWormwood), "полынь", 2, "Тебе не хватает полыни."
        );
        AddRes(index, typeof(Garlic), "чеснок", 1, "Тебе нужен чеснок.");

        AddCraft(
            typeof(MahaonStJohnsTea), brews, "зверобойный чай", 20.0, 70.0,
            typeof(MahaonStJohnsWort), "зверобой", 3, "Тебе не хватает зверобоя."
        );

        index = AddCraft(
            typeof(MahaonMountainRootBrew), brews, "отвар горного корня", 45.0, 95.0,
            typeof(MahaonMountainRoot), "горный корень", 2, "Тебе не хватает горного корня."
        );
        AddRes(index, typeof(MahaonStJohnsWort), "зверобой", 1, "Тебе не хватает зверобоя.");

        index = AddCraft(
            typeof(MahaonWarmingBrew), brews, "согревающий отвар", 55.0, 105.0,
            typeof(MahaonSnowbloom), "снежноцвет", 2, "Тебе не хватает снежноцвета."
        );
        AddRes(index, typeof(MahaonMountainRoot), "горный корень", 1, "Тебе не хватает горного корня.");

        index = AddCraft(
            typeof(MahaonMoonwortInfusion), brews, "настой лунницы", 70.0, 120.0,
            typeof(MahaonMoonwort), "лунница", 2, "Тебе не хватает лунницы."
        );
        AddRes(index, typeof(MahaonStJohnsWort), "зверобой", 1, "Тебе не хватает зверобоя.");
    }
}
