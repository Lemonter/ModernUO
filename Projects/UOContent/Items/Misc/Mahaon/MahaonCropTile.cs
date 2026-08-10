using System.Collections.Generic;
using ModernUO.Serialization;
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
[SerializationGenerator(0, false)]
public partial class MahaonCropTile : Item
{
    [SerializableField(0)]
    private MahaonCropType _cropType;

    [SerializableField(1)]
    private bool _harvested;

    private const int GmMarkerHue = 53; // "yellow" per the request — tweak freely, purely cosmetic
    private const int HarvestAmount = 6;

    private static readonly List<MahaonCropTile> AllTiles = new();

    private MahaonCropDefinition Definition => MahaonCropTable.Data[(int)_cropType];

    [Constructible]
    public MahaonCropTile(MahaonCropType cropType = MahaonCropType.Onion)
        : base(MahaonCropTable.Data[(int)cropType].GraphicFor(SeasonSystem.CurrentSeason))
    {
        Movable = false;
        Hue = GmMarkerHue;
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

    public static void Configure()
    {
        SeasonSystem.OnSeasonChanged += OnSeasonChanged;
    }

    private static void OnSeasonChanged(MahaonSeason season)
    {
        foreach (var tile in AllTiles)
        {
            if (tile.Deleted)
            {
                continue;
            }

            if (season == MahaonSeason.Spring)
            {
                // New year — whatever got picked last autumn grows back.
                tile._harvested = false;
            }

            tile.RefreshAppearance();
        }
    }

    private void RefreshAppearance()
    {
        Name = $"грядка: {Definition.NameRu}";

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
            from.SendMessage(0x59, $"{Definition.NameRu} ещё не созрел.");
            return;
        }

        var harvest = Definition.CreateHarvest();
        harvest.Amount = HarvestAmount;

        if (from.Backpack?.TryDropItem(from, harvest, false) != true)
        {
            harvest.MoveToWorld(from.Location, from.Map);
        }

        from.SendMessage(0x59, $"Собрано: {Definition.NameRu}.");

        _harvested = true;
        RefreshAppearance(); // goes invisible for everyone now, GM keeps seeing it (yellow)
    }
}
