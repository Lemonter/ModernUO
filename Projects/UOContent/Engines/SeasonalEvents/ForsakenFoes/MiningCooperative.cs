using System.Collections.Generic;

namespace Server.Engines.SeasonalEvents;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Mobile/MiningCooperativeMerchant.cs) — the
///     per-account purchase ledger that caps how much ethereal sand one family may buy. ServUO wrote it with
///     <c>Persistence.Serialize</c> to <c>Saves/Misc/MiningCooperative.bin</c>; this codebase has no file-backed
///     persistence, so the ledger is a <see cref="GenericPersistence" /> keyed by account username (an account can
///     be deleted while its ledger row survives, which a <c>Mobile</c> key could not).
/// </summary>
public static class MiningCooperative
{
    private static readonly Dictionary<string, int> _purchases = new();

    private static MiningCooperativePersistence _persistence;

    public static void Configure()
    {
        _persistence = new MiningCooperativePersistence();
    }

    public static int PurchaseAmount(Mobile from) =>
        from?.Account?.Username is { } account && _purchases.TryGetValue(account, out var amount) ? amount : 0;

    public static void AddPurchase(Mobile from, int amount)
    {
        if (from?.Account?.Username is not { } account)
        {
            return;
        }

        _purchases[account] = PurchaseAmount(from) + amount;
    }

    public static void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write(_purchases.Count);

        foreach (var (account, purchase) in _purchases)
        {
            writer.Write(account);
            writer.Write(purchase);
        }
    }

    public static void Deserialize(IGenericReader reader)
    {
        reader.ReadInt();

        var count = reader.ReadInt();

        for (var i = 0; i < count; i++)
        {
            var account = reader.ReadString();
            var purchase = reader.ReadInt();

            if (account != null)
            {
                _purchases[account] = purchase;
            }
        }
    }
}

public class MiningCooperativePersistence : GenericPersistence
{
    public MiningCooperativePersistence() : base("MiningCooperative", 10)
    {
    }

    public override void Serialize(IGenericWriter writer) => MiningCooperative.Serialize(writer);

    public override void Deserialize(IGenericReader reader) => MiningCooperative.Deserialize(reader);
}
