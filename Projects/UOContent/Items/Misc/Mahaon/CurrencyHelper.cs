namespace Server.Items;

/// <summary>
///     Mahaon custom tri-metal currency: MahaonCopper / MahaonSilver / Gold.
///     Conversion rate: 10 MahaonCopper = 1 MahaonSilver, 100 MahaonSilver = 1 Gold (so 1000 MahaonCopper = 1 Gold).
///     All amounts internally normalize to a "total copper value" (long) for math, then
///     get broken back down into the three denominations for display/payout.
/// </summary>
public static class CurrencyHelper
{
    public const int CopperPerSilver = 10;
    public const int SilverPerGold = 100;
    public const int CopperPerGold = CopperPerSilver * SilverPerGold; // 1000

    /// <summary>
    ///     Converts a (gold, silver, copper) tuple into total copper value.
    /// </summary>
    public static long ToCopperValue(int gold, int silver, int copper) =>
        (long)gold * CopperPerGold + (long)silver * CopperPerSilver + copper;

    /// <summary>
    ///     Breaks a total copper value down into the fewest coins possible:
    ///     as much gold as possible, then silver, then copper remainder.
    /// </summary>
    public static (int gold, int silver, int copper) FromCopperValue(long totalCopper)
    {
        if (totalCopper < 0)
        {
            totalCopper = 0;
        }

        var gold = (int)(totalCopper / CopperPerGold);
        var remainder = totalCopper % CopperPerGold;
        var silver = (int)(remainder / CopperPerSilver);
        var copper = (int)(remainder % CopperPerSilver);

        return (gold, silver, copper);
    }

    /// <summary>
    ///     Scans a container for Gold/MahaonSilver/MahaonCopper stacks and returns their combined copper value.
    ///     Does not recurse into sub-containers by default to keep this cheap for frequent checks.
    /// </summary>
    public static long GetCopperValue(Container container, bool recurse = false)
    {
        if (container == null)
        {
            return 0;
        }

        long total = 0;

        var gold = container.FindItemByType<Gold>(recurse);
        if (gold != null)
        {
            total += (long)gold.Amount * CopperPerGold;
        }

        var silver = container.FindItemByType<MahaonSilver>(recurse);
        if (silver != null)
        {
            total += (long)silver.Amount * CopperPerSilver;
        }

        var copper = container.FindItemByType<MahaonCopper>(recurse);
        if (copper != null)
        {
            total += copper.Amount;
        }

        return total;
    }

    /// <summary>
    ///     Deposits a total copper value into a container as the fewest coins possible,
    ///     stacking onto existing Gold/MahaonSilver/MahaonCopper piles where present.
    /// </summary>
    public static void DepositCopperValue(Container container, long totalCopper)
    {
        if (container == null || totalCopper <= 0)
        {
            return;
        }

        var (gold, silver, copper) = FromCopperValue(totalCopper);

        if (gold > 0)
        {
            DepositStack<Gold>(container, gold, amount => new Gold(amount));
        }

        if (silver > 0)
        {
            DepositStack<MahaonSilver>(container, silver, amount => new MahaonSilver(amount));
        }

        if (copper > 0)
        {
            DepositStack<MahaonCopper>(container, copper, amount => new MahaonCopper(amount));
        }
    }

    private static void DepositStack<T>(Container container, int amount, System.Func<int, T> create) where T : Item
    {
        var existing = container.FindItemByType<T>(false);
        if (existing != null)
        {
            existing.Amount += amount;
        }
        else
        {
            container.DropItem(create(amount));
        }
    }

    /// <summary>
    ///     "Gp" (the vanilla Gold item) becomes real denominations from here on — the same
    ///     number, reinterpreted as copper and broken down. A vanilla drop of 412 becomes
    ///     41 silver + 2 copper; 5000 becomes a clean 5 gold. Removes any Gold sitting
    ///     directly in the container and replaces it with the proper breakdown.
    /// </summary>
    public static void ConvertVanillaGold(Container container)
    {
        var gold = container?.FindItemByType<Gold>(false);
        if (gold == null)
        {
            return;
        }

        var totalCopper = gold.Amount;
        gold.Delete();

        DepositCopperValue(container, totalCopper);
    }

    /// <summary>
    ///     Attempts to withdraw a total copper value from a container, consuming Gold/MahaonSilver/MahaonCopper
    ///     stacks as needed (largest denomination first) and making change by re-depositing the
    ///     leftover value in the fewest coins. Returns false if the container doesn't hold enough.
    /// </summary>
    public static bool TryWithdrawCopperValue(Container container, long totalCopper)
    {
        if (container == null || totalCopper <= 0)
        {
            return totalCopper <= 0;
        }

        var available = GetCopperValue(container);
        if (available < totalCopper)
        {
            return false;
        }

        // Consume everything, then redeposit the remainder as the fewest coins.
        container.FindItemByType<Gold>(false)?.Delete();
        container.FindItemByType<MahaonSilver>(false)?.Delete();
        container.FindItemByType<MahaonCopper>(false)?.Delete();

        DepositCopperValue(container, available - totalCopper);
        return true;
    }
}
