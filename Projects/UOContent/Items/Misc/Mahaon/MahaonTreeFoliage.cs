using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonTreeFoliage : Item
{
    [SerializableField(0)]
    private MahaonTreeSpecies _species;

    [SerializableField(1)]
    private int _fruitRemaining;

    private static readonly HashSet<MahaonTreeFoliage> _all = [];

    /// <summary>Every tree crown in the world — bots look here for fruit to pick.</summary>
    public static IReadOnlyCollection<MahaonTreeFoliage> All => _all;

    public bool BearsFruit => MahaonTreeSpeciesTable.Get(_species).BearsFruit;

    [Constructible]
    public MahaonTreeFoliage(MahaonTreeSpecies species = MahaonTreeSpecies.Apple) : base(0x0000)
    {
        Movable = false;
        _species = species;
        _all.Add(this);

        SeasonSystem.OnSeasonChanged += OnSeasonChanged;
        RefreshGraphic(resetFruit: true);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _all.Add(this);
        SeasonSystem.OnSeasonChanged += OnSeasonChanged;
    }

    public override void OnDelete()
    {
        SeasonSystem.OnSeasonChanged -= OnSeasonChanged;
        _all.Remove(this);
        base.OnDelete();
    }

    private void OnSeasonChanged(MahaonSeason season)
    {
        if (!Deleted)
        {
            RefreshGraphic(resetFruit: true);
        }
    }

    /// <summary>Fruiting species show the fruiting graphic in spring/summer (with fruit
    /// left to give), the bare graphic in autumn, and the winter graphic in winter. Non-
    /// fruiting species just alternate between their two given foliage graphics with the
    /// season, plus the dedicated winter one.</summary>
    private void RefreshGraphic(bool resetFruit)
    {
        var data = MahaonTreeSpeciesTable.Get(_species);
        var season = SeasonSystem.CurrentSeason;

        if (season == MahaonSeason.Winter)
        {
            ItemID = data.FoliageWinterGraphic;
            Name = $"{data.NameRu} — зимняя листва";
            return;
        }

        if (!data.BearsFruit)
        {
            // Simple alternation for non-fruit trees — spring/summer look one way,
            // autumn looks the other, matching "листва меняется" without fruit involved.
            ItemID = season == MahaonSeason.Autumn ? data.FoliageWinterGraphic : data.FoliageBareGraphic;
            Name = data.NameRu;
            return;
        }

        if (resetFruit && season is MahaonSeason.Spring or MahaonSeason.Summer)
        {
            _fruitRemaining = Utility.RandomMinMax(data.FruitMin, data.FruitMax);
        }

        if (season is MahaonSeason.Spring or MahaonSeason.Summer && _fruitRemaining > 0)
        {
            ItemID = data.FoliageFruitGraphic;
            Name = $"{data.NameRu} с плодами";
        }
        else
        {
            ItemID = data.FoliageBareGraphic;
            Name = data.NameRu;
        }
    }

    /// <summary>Axing the foliage of a fruit tree gets everything at once — whatever fruit
    /// is left, plus the wood from felling the trunk — unlike a plain double-click, which
    /// just picks a little fruit without destroying the tree.</summary>
    public void ChopWithAxe(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendMessage("Слишком далеко.");
            return;
        }

        var data = MahaonTreeSpeciesTable.Get(_species);

        if (data.BearsFruit && _fruitRemaining > 0 && SeasonSystem.CurrentSeason is MahaonSeason.Spring or MahaonSeason.Summer)
        {
            var fruit = data.CreateFruit(_fruitRemaining);
            fruit.Amount = MahaonHouseFenceSystem.ApplyYieldBonus(fruit.Amount, GetWorldLocation(), Map);

            if (from.Backpack?.TryDropItem(from, fruit, false) != true)
            {
                fruit.MoveToWorld(from.Location, from.Map);
            }

            from.SendMessage(0x59, $"Ты собираешь оставшиеся плоды: {fruit.Amount} шт.");
        }

        foreach (var item in Map.GetItemsInRange<MahaonTree>(Location, 0))
        {
            if (item.Location == Location && !item.Deleted)
            {
                item.OnDoubleClick(from); // fells the trunk, drops the wood, deletes both halves
                return;
            }
        }

        from.SendMessage("Не могу найти ствол этого дерева.");
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendMessage("Слишком далеко.");
            return;
        }

        var data = MahaonTreeSpeciesTable.Get(_species);

        if (!data.BearsFruit)
        {
            // No fruit to give — clicking the leaves fells the tree instead, same as
            // clicking the trunk directly.
            foreach (var item in Map.GetItemsInRange<MahaonTree>(Location, 0))
            {
                if (item.Location == Location && !item.Deleted)
                {
                    item.OnDoubleClick(from);
                    return;
                }
            }

            from.SendMessage("Не могу найти ствол этого дерева.");
            return;
        }

        if (_fruitRemaining <= 0 || SeasonSystem.CurrentSeason is not (MahaonSeason.Spring or MahaonSeason.Summer))
        {
            from.SendMessage("На этом дереве сейчас нет плодов.");
            return;
        }

        var amount = Utility.RandomMinMax(1, 3);
        amount = System.Math.Min(amount, _fruitRemaining);

        var fruit = data.CreateFruit(amount);
        fruit.Amount = MahaonHouseFenceSystem.ApplyYieldBonus(fruit.Amount, GetWorldLocation(), Map);

        if (from.Backpack?.TryDropItem(from, fruit, false) != true)
        {
            fruit.MoveToWorld(from.Location, from.Map);
        }

        _fruitRemaining -= amount;
        from.SendMessage(0x59, "Вы собрали немного фруктов.");

        if (_fruitRemaining <= 0)
        {
            RefreshGraphic(resetFruit: false);
        }
    }
}
