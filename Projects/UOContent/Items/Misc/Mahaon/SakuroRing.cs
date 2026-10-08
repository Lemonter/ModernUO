using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

public enum SakuroType
{
    Power, // +20 Str, 0 else
    Agility, // +20 Dex, 0 else
    Wisdom, // +20 Int, 0 else
    Balance, // +10 all three
    Fox, // +15 Dex, +15 Int
    Bear, // +15 Str, +15 Dex
    Owl, // +15 Str, +15 Int
    Titan // +10 Str, +5 Dex, +5 Int — the odd one out
}

/// <summary>
///     Mahaon "Sakuro" jewelry: crafted from a monster head, grants a fixed stat profile
///     depending on type. Which head yields which type wasn't something the player could
///     recall precisely, so crafting a Sakuro currently just rolls a random type — see
///     <see cref="SakuroCraftingTool" />. Tune to a real head→type table if/when that
///     detail surfaces.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SakuroRing : BaseRing
{
    [SerializableField(0)]
    private SakuroType _type;

    private static readonly Dictionary<SakuroType, (int str, int dex, int intel)> Profiles = new()
    {
        [SakuroType.Power]   = (20, 0, 0),
        [SakuroType.Agility] = (0, 20, 0),
        [SakuroType.Wisdom]  = (0, 0, 20),
        [SakuroType.Balance] = (10, 10, 10),
        [SakuroType.Fox]     = (0, 15, 15),
        [SakuroType.Bear]    = (15, 15, 0),
        [SakuroType.Owl]     = (15, 0, 15),
        [SakuroType.Titan]   = (10, 5, 5)
    };

    [Constructible]
    public SakuroRing(SakuroType type) : base(0x108A)
    {
        _type = type;
        Name = $"сакуро-перстень: {SakuroTypeRu(type)}";
    }

    private static string SakuroTypeRu(SakuroType type) => type switch
    {
        SakuroType.Power   => "Сила",
        SakuroType.Agility => "Ловкость",
        SakuroType.Wisdom  => "Мудрость",
        SakuroType.Balance => "Баланс",
        SakuroType.Fox     => "Лис",
        SakuroType.Bear    => "Медведь",
        SakuroType.Owl     => "Сова",
        SakuroType.Titan   => "Титан",
        _                  => type.ToString()
    };

    public override double DefaultWeight => 0.1;

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (parent is not Mobile owner)
        {
            return;
        }

        var (str, dex, intel) = Profiles[_type];

        AddModIfNonZero(owner, StatType.Str, str);
        AddModIfNonZero(owner, StatType.Dex, dex);
        AddModIfNonZero(owner, StatType.Int, intel);
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        // Deterministic names (not the tracked list — that doesn't survive a server
        // restart) so unequipping still cleans up correctly even after a reload.
        if (parent is Mobile owner)
        {
            owner.RemoveStatMod($"Sakuro-{Serial}-{StatType.Str}");
            owner.RemoveStatMod($"Sakuro-{Serial}-{StatType.Dex}");
            owner.RemoveStatMod($"Sakuro-{Serial}-{StatType.Int}");
        }
    }

    private void AddModIfNonZero(Mobile owner, StatType stat, int bonus)
    {
        if (bonus == 0)
        {
            return;
        }

        owner.AddStatMod(new StatMod(stat, $"Sakuro-{Serial}-{stat}", bonus, System.TimeSpan.Zero));
    }
}
