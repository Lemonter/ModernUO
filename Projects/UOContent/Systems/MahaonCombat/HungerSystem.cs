using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Each player has a "desired food" that rotates every 2 real hours. Eating exactly
///     that food (while it's still the current desire) grants +10 Str/Dex/Int for 12 real
///     hours. Shown as an item icon in MahaonCombatMenuGump. In-memory only, same
///     convention as BotGuilds/GuildBank/etc — resets on server restart.
/// </summary>
public static class HungerSystem
{
    private static readonly TimeSpan RotateInterval = TimeSpan.FromHours(2);
    private static readonly TimeSpan BonusDuration = TimeSpan.FromHours(12);
    private const int StatBonus = 10;

    // A representative spread across the Food.cs roster — different enough graphics to be
    // visually distinct as an icon, common enough that a player can realistically find or
    // buy each one.
    private static readonly Type[] FoodPool =
    {
        typeof(BreadLoaf),
        typeof(Bacon),
        typeof(FishSteak),
        typeof(CheeseWheel),
        typeof(FriedEggs),
        typeof(CookedBird),
        typeof(RoastPig),
        typeof(Sausage),
        typeof(Ham),
        typeof(Cake),
        typeof(Ribs),
        typeof(Cookies),
        typeof(ApplePie),
        typeof(LambLeg),
        typeof(ChickenLeg)
    };

    private class HungerState
    {
        public Type DesiredFood;
        public DateTime RotatesAt;
        public DateTime BonusExpiresAt; // MinValue = no active bonus
    }

    private static readonly Dictionary<Mobile, HungerState> States = new();

    private static readonly Dictionary<Type, int> FoodGraphics = new()
    {
        [typeof(BreadLoaf)] = 0x103B,
        [typeof(Bacon)] = 0x979,
        [typeof(FishSteak)] = 0x97B,
        [typeof(CheeseWheel)] = 0x97E,
        [typeof(FriedEggs)] = 0x9B6,
        [typeof(CookedBird)] = 0x9B7,
        [typeof(RoastPig)] = 0x9BB,
        [typeof(Sausage)] = 0x9C0,
        [typeof(Ham)] = 0x9C9,
        [typeof(Cake)] = 0x9E9,
        [typeof(Ribs)] = 0x9F2,
        [typeof(Cookies)] = 0x160b,
        [typeof(ApplePie)] = 0x1041,
        [typeof(LambLeg)] = 0x160a,
        [typeof(ChickenLeg)] = 0x1608
    };

    public static int GetGraphic(Type foodType) => FoodGraphics.GetValueOrDefault(foodType, 0x103B);

    public static string GetFoodNameRu(Type foodType) => foodType.Name switch
    {
        nameof(BreadLoaf) => "хлеб",
        nameof(Bacon) => "бекон",
        nameof(FishSteak) => "рыбный стейк",
        nameof(CheeseWheel) => "головку сыра",
        nameof(FriedEggs) => "яичницу",
        nameof(CookedBird) => "жареную птицу",
        nameof(RoastPig) => "жареного поросёнка",
        nameof(Sausage) => "колбасу",
        nameof(Ham) => "ветчину",
        nameof(Cake) => "торт",
        nameof(Ribs) => "рёбрышки",
        nameof(Cookies) => "печенье",
        nameof(ApplePie) => "яблочный пирог",
        nameof(LambLeg) => "баранью ногу",
        nameof(ChickenLeg) => "куриную ножку",
        _ => foodType.Name
    };

    private static HungerState GetState(Mobile m)
    {
        if (States.TryGetValue(m, out var state))
        {
            if (Core.Now >= state.RotatesAt)
            {
                RollNewDesire(state);
            }

            return state;
        }

        state = new HungerState();
        RollNewDesire(state);
        States[m] = state;
        return state;
    }

    private static void RollNewDesire(HungerState state)
    {
        state.DesiredFood = FoodPool[Utility.Random(FoodPool.Length)];
        state.RotatesAt = Core.Now + RotateInterval;
    }

    public static Type GetDesiredFood(Mobile m) => GetState(m).DesiredFood;

    public static bool HasActiveBonus(Mobile m) => GetState(m).BonusExpiresAt > Core.Now;

    public static TimeSpan GetBonusTimeLeft(Mobile m)
    {
        var left = GetState(m).BonusExpiresAt - Core.Now;
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    /// <summary>Called from Food.Eat() right after a successful bite — checks whether what
    /// was just eaten matches the current desire and, if so, grants the bonus and rolls a
    /// new desire immediately (so eating the right food doesn't just sit there satisfied
    /// until the timer would've rotated anyway).</summary>
    public static void OnAte(Mobile from, Item food)
    {
        var state = GetState(from);

        if (food.GetType() != state.DesiredFood)
        {
            return;
        }

        GrantBonus(from, state);
        RollNewDesire(state);
        from.SendMessage(0x59, "Это была именно та еда, которую тебе хотелось! Ты чувствуешь прилив сил.");
    }

    private static void GrantBonus(Mobile from, HungerState state)
    {
        from.RemoveStatMod("MahaonHungerStr");
        from.RemoveStatMod("MahaonHungerDex");
        from.RemoveStatMod("MahaonHungerInt");

        from.AddStatMod(new StatMod(StatType.Str, "MahaonHungerStr", StatBonus, BonusDuration));
        from.AddStatMod(new StatMod(StatType.Dex, "MahaonHungerDex", StatBonus, BonusDuration));
        from.AddStatMod(new StatMod(StatType.Int, "MahaonHungerInt", StatBonus, BonusDuration));

        state.BonusExpiresAt = Core.Now + BonusDuration;
    }
}
