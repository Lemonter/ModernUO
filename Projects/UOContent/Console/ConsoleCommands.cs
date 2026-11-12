using System;
using System.Linq;
using Server.Accounting;
using Server.Saves;

namespace Server.Misc;

public static class ConsoleCommands
{
    public static void Configure()
    {
        ConsoleInputHandler.RegisterCommand(
            ["save", "s"],
            "Saves the world",
            _ => Core.LoopContext.Post(AutoSave.Save)
        );

        ConsoleInputHandler.RegisterCommand(
            ["shutdown", "sh"],
            "Shuts down the server.",
            _ => Core.Kill()
        );

        ConsoleInputHandler.RegisterCommand(
            ["restart", "r"],
            "Restarts the server.",
            _ => Core.Kill(true)
        );

        ConsoleInputHandler.RegisterCommand(
            ["access"],
            "access <аккаунт> <Player|Counselor|GameMaster|Seer|Administrator> — уровень доступа аккаунта.",
            args => Core.LoopContext.Post(() => SetAccess(args))
        );

        ConsoleInputHandler.RegisterCommand(
            ["testaccounts"],
            "testaccounts — одноразовые аккаунты тестового клиента (cj…): когда созданы, персонажи, дома.",
            _ => Core.LoopContext.Post(ListTestAccounts)
        );

        ConsoleInputHandler.RegisterCommand(
            ["droptest"],
            "droptest <аккаунт|all> — удалить одноразовый тестовый аккаунт (cj…) с персонажами и их домами.",
            args => Core.LoopContext.Post(() => DropTest(args.Trim()))
        );
    }

    // Одноразовые аккаунты тестового клиента (Tools/LiveTest) называются cj + 8 hex-цифр.
    private static bool IsTestAccount(Account a) =>
        a.Username.Length == 10 && a.Username.StartsWith("cj", StringComparison.Ordinal) &&
        Uri.IsHexDigit(a.Username[2]) && a.Username[2..].All(Uri.IsHexDigit);

    private static void ListTestAccounts()
    {
        var n = 0;
        foreach (var ia in Accounts.GetAccounts())
        {
            if (ia is not Account a || !IsTestAccount(a))
            {
                continue;
            }

            n++;
            var houses = 0;
            var chars = 0;
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] is { } m)
                {
                    chars++;
                    houses += Multis.BaseHouse.GetHouses(m).Count;
                }
            }

            Console.WriteLine($"  {a.Username}  создан {a.Created:yyyy-MM-dd HH:mm}  персонажей {chars}  домов {houses}");
        }

        Console.WriteLine($"Тестовых аккаунтов: {n}.");
    }

    /// <summary>
    ///     Убрать за тестовым клиентом: аккаунт, его персонажей, их дома (с оградой, подвалом и
    ///     деревьями — всё уходит вместе с домом) и купленные ими городские дома — те
    ///     возвращаются городу. Только для аккаунтов вида cj + hex: настоящий так не удалить.
    /// </summary>
    private static void DropTest(string name)
    {
        var targets = new System.Collections.Generic.List<Account>();
        foreach (var ia in Accounts.GetAccounts())
        {
            if (ia is Account a && IsTestAccount(a) && (name == "all" || a.Username == name))
            {
                targets.Add(a);
            }
        }

        if (targets.Count == 0)
        {
            Console.WriteLine($"Тестового аккаунта «{name}» нет (удаляются только cj + 8 hex-цифр).");
            return;
        }

        foreach (var a in targets)
        {
            var houses = 0;
            var cityHouses = 0;

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] is not { } m)
                {
                    continue;
                }

                foreach (var house in Multis.BaseHouse.GetHouses(m))
                {
                    house.Delete();
                    houses++;
                }

                foreach (var city in Systems.MahaonWorld.MahaonCityHouseSystem.All())
                {
                    if (city.Owner == m)
                    {
                        Systems.MahaonWorld.MahaonCityHouseSystem.SellBack(city, FindSign(city));
                        cityHouses++;
                    }
                }
            }

            var username = a.Username;
            a.Delete();
            Console.WriteLine($"Удалён {username}: домов {houses}, городских домов возвращено {cityHouses}.");
        }
    }

    private static Items.MahaonCityHouseSign FindSign(Items.MahaonCityHouse house)
    {
        foreach (var item in World.Items.Values)
        {
            if (item is Items.MahaonCityHouseSign sign && sign.House == house)
            {
                return sign;
            }
        }

        return null;
    }

    /// <summary>
    ///     Выдать аккаунту уровень доступа из консоли сервера.
    ///
    ///     Зачем: тестовому клиенту, который гоняет бой на отключённом фасете, нужны права ГМ —
    ///     создать существо, перенестись. Раньше выдать их можно было только из игры, рукой
    ///     того, у кого права уже есть. Консоль — у того, кто запустил сервер, то есть у
    ///     владельца; лазейкой это не становится.
    ///
    ///     Защищённые аккаунты (server-access.json) не трогаются: опечатка в консоли не должна
    ///     разжаловать владельца.
    /// </summary>
    private static void SetAccess(string args)
    {
        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2 || !Enum.TryParse<AccessLevel>(parts[1], true, out var level))
        {
            Console.WriteLine("access <аккаунт> <Player|Counselor|GameMaster|Seer|Administrator>");
            return;
        }

        if (Accounts.GetAccount(parts[0]) is not Account account)
        {
            Console.WriteLine($"Аккаунта «{parts[0]}» нет.");
            return;
        }

        if (ServerAccess.ServerAccessConfiguration?.ProtectedAccounts.Contains(account.Username) == true)
        {
            Console.WriteLine($"Аккаунт «{account.Username}» защищён — его уровень из консоли не меняется.");
            return;
        }

        if (level >= AccessLevel.Owner)
        {
            Console.WriteLine("Владельца из консоли не назначают.");
            return;
        }

        account.AccessLevel = level;

        // Уровень персонажа копируется из аккаунта только при создании — уже созданных
        // переводим сами, иначе выданные права заработают лишь у следующего персонажа.
        for (var i = 0; i < account.Length; i++)
        {
            if (account[i] is { } m)
            {
                m.AccessLevel = level;
            }
        }

        Console.WriteLine($"Аккаунт «{account.Username}» и его персонажи: {level}.");
    }
}
