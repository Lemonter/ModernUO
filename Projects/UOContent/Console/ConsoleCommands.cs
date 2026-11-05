using System;
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
