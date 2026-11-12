using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Systems.MahaonFarming;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;

namespace Server.Items;

/// <summary>
///     A seasonal crop plot — cycles through spring/summer/autumn graphics on its own as
///     SeasonSystem advances, is only actually harvestable while showing its autumn (ripe)
///     graphic, and goes fully invisible in winter (dormant) or once someone's picked it
///     this cycle. Invisibility is global — it's a shared plot, not a per-player one: first
///     person to click it in autumn gets the harvest, everyone else just sees it's gone
///     until next year. Resets itself back to harvestable the next time the calendar comes
///     back around to autumn — no GM involvement needed.
///
///     GMs always see it regardless of season/harvested state (AccessLevel bypasses
///     Item.Visible, same as MahaonDungeonMarker) — painted a permanent yellow-ish tint so
///     it's easy to spot even when it's just growing normally and blends in visually.
/// </summary>
[SerializationGenerator(1, false)]
public partial class MahaonCropTile : Item
{
    [SerializableField(0)]
    private MahaonCropType _cropType;

    [SerializableField(1)]
    private bool _harvested;

    // Посеянная грядка (семенами — игроком или ботом) зарастает, если за целый круг сезонов её
    // ни разу не собрали: иначе посадки копились бы вокруг городов без предела. Поля карты
    // (MahaonFieldSeeding), грядки GM и всё, что посажено до этого правила, не зарастают.
    [SerializableField(2)]
    private bool _planted;

    // Сколько осеней подряд урожай никто не снял.
    [SerializableField(3)]
    private sbyte _neglect;

    private void MigrateFrom(V0Content content)
    {
        _cropType = content.CropType;
        _harvested = content.Harvested;
    }

    // Раньше грядки красились в жёлтый, чтобы ГМ видел их среди травы. Теперь грядками
    // стали и все поля Британии — пять с половиной тысяч штук, — и подкрашивать их значило
    // бы перекрасить полмира. Ищутся они и так: зимой и после сбора они невидимы для
    // игроков, но не для ГМ (AccessLevel обходит Item.Visible).
    // Сколько снимается с грядки, решает навык Земледелия (MahaonFarmingSkill.Yield).

    /// <summary>Во сколько раз семян меньше, чем урожая.</summary>
    private const int SeedsPerHarvestDivisor = 3;

    private static readonly List<MahaonCropTile> AllTiles = new();

    /// <summary>Все грядки мира — для севооборота, который работает полями, а не тайлами.</summary>
    public static IReadOnlyList<MahaonCropTile> All => AllTiles;

    /// <summary>
    ///     Меняет культуру и сразу перерисовывает грядку.
    ///
    ///     Отдельным методом, а не просто сеттером: культура определяет и графику, и
    ///     название, так что смена без перерисовки оставила бы на поле пшеницу, которая
    ///     называется морковью.
    /// </summary>
    public void SetCrop(MahaonCropType crop)
    {
        CropType = crop;
        RefreshAppearance();
    }

    private MahaonCropDefinition Definition => MahaonCropTable.Data[(int)_cropType];

    [Constructible]
    public MahaonCropTile(MahaonCropType cropType = MahaonCropType.Onion)
        : base(MahaonCropTable.Data[(int)cropType].GraphicFor(SeasonSystem.CurrentSeason))
    {
        Movable = false;
        _cropType = cropType;

        AllTiles.Add(this);
        RefreshAppearance();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        AllTiles.Add(this);
        RefreshAppearance();
    }

    public override void OnDelete()
    {
        AllTiles.Remove(this);
        base.OnDelete();
    }

    private static bool _configured;

    public static void Configure()
    {
        // Повторный вызов безопасен: тестовый хост вызывает Configure выборочно и сам.
        if (_configured)
        {
            return;
        }

        _configured = true;
        SeasonSystem.OnSeasonChanged += OnSeasonChanged;
    }

    /// <summary>Сколько осеней подряд без сбора грядка терпит, прежде чем зарасти.</summary>
    private const int NeglectedAutumns = 1;

    /// <summary>Отмечает грядку посеянной: такая зарастает, если её бросить.</summary>
    public void MarkPlanted()
    {
        Planted = true;

        // Посеянное осенью или зимой своей осени ещё не видело — год отсчитывается со следующей.
        Neglect = (sbyte)(SeasonSystem.CurrentSeason is MahaonSeason.Autumn or MahaonSeason.Winter ? -1 : 0);
    }

    private static void OnSeasonChanged(MahaonSeason season)
    {
        List<MahaonCropTile> overgrown = null;

        foreach (var tile in AllTiles)
        {
            if (tile.Deleted)
            {
                continue;
            }

            if (season == MahaonSeason.Spring)
            {
                if (tile._planted)
                {
                    tile.Neglect = (sbyte)(tile._harvested ? 0 : tile._neglect + 1);
                    if (tile._neglect >= NeglectedAutumns)
                    {
                        (overgrown ??= []).Add(tile);
                        continue;
                    }
                }

                // New year — whatever got picked last autumn grows back.
                tile.Harvested = false;
            }

            tile.RefreshAppearance();
        }

        if (overgrown == null)
        {
            return;
        }

        foreach (var tile in overgrown)
        {
            // Зарастает и пашня под грядкой.
            MahaonTilledEarth.Find(tile.GetWorldLocation(), tile.Map)?.Delete();
            tile.Delete();
        }
    }

    /// <summary>
    ///     Семена той же культуры вместе с урожаем.
    ///
    ///     Без этого земледелие упиралось в лавку: посеять можно было только то, что продал
    ///     фермер, а собранное шло в еду и только. Теперь снятый лён даёт и лён, и семена
    ///     льна — поле воспроизводит само себя, и фермер нужен для начала, а не навсегда.
    ///
    ///     Семян меньше, чем урожая: поле должно расширяться, но не взрывообразно, иначе
    ///     первый же собранный участок покроет посевами всё вокруг.
    /// </summary>
    private void GiveSeeds(Mobile from, int harvested)
    {
        var count = Math.Max(1, harvested / SeedsPerHarvestDivisor);
        var seeds = new MahaonCropSeed(_cropType) { Amount = count };

        if (from.Backpack?.TryDropItem(from, seeds, false) != true)
        {
            seeds.MoveToWorld(from.Location, from.Map);
        }

        from.SendMessage(0x59, $"Собраны и семена: {Definition.NameRu} ({count}).");
    }

    private void RefreshAppearance()
    {
        Name = SeasonSystem.CurrentSeason switch
        {
            MahaonSeason.Spring => $"грядка: {Definition.NameRu} (росток)",
            MahaonSeason.Summer => $"грядка: {Definition.NameRu} (почти созрел)",
            MahaonSeason.Autumn => $"грядка: {Definition.NameRu} (можно собирать)",
            _                   => $"грядка: {Definition.NameRu}"
        };

        if (_harvested || SeasonSystem.CurrentSeason == MahaonSeason.Winter)
        {
            Visible = false; // gone for regular players — GM still sees it (AccessLevel bypass)
            return;
        }

        Visible = true;
        ItemID = Definition.GraphicFor(SeasonSystem.CurrentSeason);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // "That is too far away."
            return;
        }

        if (SeasonSystem.CurrentSeason == MahaonSeason.Winter)
        {
            from.SendMessage(0x22, "Зимой тут ничего не растёт.");
            return;
        }

        if (_harvested)
        {
            from.SendMessage(0x22, "Урожай уже собрали — жди следующей осени.");
            return;
        }

        if (SeasonSystem.CurrentSeason != MahaonSeason.Autumn)
        {
            // Стадий теперь три, и грядка это показывает графикой — пусть и на словах
            // говорит, на какой именно она стоит, а не отделывается общим «не созрел».
            from.SendMessage(
                0x59,
                SeasonSystem.CurrentSeason == MahaonSeason.Spring
                    ? $"{Definition.NameRu} только проклюнулся."
                    : $"{Definition.NameRu} почти созрел — дождись осени."
            );

            return;
        }

        if (!MahaonFarmingSkill.CanWork(from, _cropType))
        {
            MahaonFarmingSkill.SendTooHard(from, _cropType);
            return;
        }

        var amount = MahaonFarmingSkill.Yield(from);

        // Неудачный сбор портит половину, но не отнимает всё — грядку ведь уже вырастили.
        if (!MahaonFarmingSkill.CheckHarvest(from, _cropType))
        {
            amount = MahaonFarmingSkill.Spoiled(amount);
            from.SendMessage(0x22, "Ты неловко обрываешь плоды — часть урожая побита.");
        }

        // Вспаханная земля под грядкой удваивает урожай — и на этом заканчивается: пашня
        // уходит вместе со снятым урожаем, так что перед следующим посевом землю надо
        // готовить заново. Иначе один раз вспаханное поле кормило бы вдвойне вечно.
        var tilled = MahaonTilledEarth.Find(GetWorldLocation(), Map);

        if (tilled != null)
        {
            amount *= MahaonTilledEarth.YieldMultiplier;
            tilled.Delete();
            from.SendMessage(0x59, "Земля была вспахана — урожай вышел вдвое щедрее.");
        }

        var harvest = Definition.CreateHarvest();
        harvest.Amount = Systems.MahaonWorld.MahaonHouseFenceSystem.ApplyYieldBonus(amount, GetWorldLocation(), Map);

        if (!Systems.MahaonWorld.MahaonResourceBagSystem.TryGive(from, harvest, MahaonResourceCategory.Crop)
            && from.Backpack?.TryDropItem(from, harvest, false) != true)
        {
            harvest.MoveToWorld(from.Location, from.Map);
        }

        GiveSeeds(from, amount);

        from.SendMessage(0x59, $"Собрано: {Definition.NameRu}.");

        Harvested = true;
        RefreshAppearance(); // goes invisible for everyone now, GM keeps seeing it (yellow)
    }
}
