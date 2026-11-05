/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Trading/TradeEntry.cs).
///     <c>ScriptCompiler.FindTypeByName</c> does not exist here — <see cref="AssemblyHandler.FindTypeByName" /> is
///     used to resolve the turn-in item type instead.
/// </summary>
using System;
using System.Collections.Generic;

namespace Server.Engines.CityLoyalty;

[PropertyObject]
public class TradeEntry
{
    public TradeEntry(City destination, City origin, int distance)
    {
        Origin = origin;
        Destination = destination;
        Distance = distance;

        Details = new List<TradeDetails>();
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public City Origin { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public City Destination { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Kills { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public int Distance { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime LastAmbush { get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public string TurnIn
    {
        get
        {
            var str = string.Empty;

            foreach (var detail in Details)
            {
                str += detail.ItemType.Name + ", ";
            }

            return str;
        }
    }

    public List<TradeDetails> Details { get; set; }

    public int CalculateGold()
    {
        if (Distance == 0)
        {
            return 0;
        }

        var gold = 0;

        foreach (var detail in Details)
        {
            gold += detail.Worth * 3;
        }

        return Math.Max(CityTradeSystem.TurnInGold, gold + Distance * 10 + Math.Min(25000, Kills * 500));
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.Write(0);

        writer.Write((int)Origin);
        writer.Write((int)Destination);

        writer.Write(LastAmbush);

        writer.Write(Kills);
        writer.Write(Distance);

        writer.Write(Details.Count);
        foreach (var detail in Details)
        {
            detail.Serialize(writer);
        }
    }

    public static TradeEntry Load(IGenericReader reader)
    {
        reader.ReadInt(); // version

        var origin = (City)reader.ReadInt();
        var destination = (City)reader.ReadInt();

        var entry = new TradeEntry(destination, origin, 0)
        {
            LastAmbush = reader.ReadDateTime(),
            Kills = reader.ReadInt(),
            Distance = reader.ReadInt()
        };

        var count = reader.ReadInt();
        for (var i = 0; i < count; i++)
        {
            entry.Details.Add(TradeDetails.Load(reader));
        }

        return entry;
    }

    public override string ToString() => $"{Origin} -> {Destination}";

    public class TradeDetails
    {
        public TradeDetails(Type type, int worth, int amount, string fallbackName)
        {
            ItemType = type;
            Worth = worth;
            Amount = amount;

            Name = CityTradeSystem.GetNameFor(type, fallbackName);
        }

        private TradeDetails()
        {
        }

        /// <summary>Restores saved details without re-deriving the display name (which would construct an item).</summary>
        public static TradeDetails Restore(Type type, int worth, int amount, string name) =>
            new() { ItemType = type, Worth = worth, Amount = amount, Name = name };

        public Type ItemType { get; set; }
        public int Amount { get; set; }
        public int Worth { get; set; }
        public string Name { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(0);

            writer.Write(ItemType.Name);
            writer.Write(Worth);
            writer.Write(Amount);
            writer.Write(Name);
        }

        public static TradeDetails Load(IGenericReader reader)
        {
            reader.ReadInt(); // version

            var type = AssemblyHandler.FindTypeByName(reader.ReadString());

            var worth = reader.ReadInt();
            var amount = reader.ReadInt();
            var name = reader.ReadString();

            return Restore(type, worth, amount, name);
        }
    }
}
