namespace Server.Items;

public static class BankCheckExtensions
{
    public static int GetChecksWorth(this Container cont, bool recurse)
    {
        var count = 0;

        foreach (var check in cont.FindItemsByType<BankCheck>(recurse))
        {
            count += check.Worth;
        }

        return count;
    }

    public static int TakeFromChecks(this Container cont, int amount, bool recurse)
    {
        var left = amount;

        foreach (var check in cont.FindItemsByType<BankCheck>(recurse))
        {
            if (check.Worth <= left)
            {
                left -= check.Worth;
                check.Delete();
            }
            else
            {
                check.Worth -= left;
                check.InvalidateProperties();
                left = 0;
                break;
            }
        }

        return amount - left;
    }
}
