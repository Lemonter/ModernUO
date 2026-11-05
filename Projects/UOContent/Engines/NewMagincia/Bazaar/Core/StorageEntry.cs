/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Core/StorageEntry.cs).
///
///     Содержимое склада Новой Маджинции для одного игрока: деньги, остатки товаров и питомцы, снятые с
///     прилавка, когда аренда кончилась. Хранится неделю, потом раздаётся по правилам
///     <see cref="MaginciaBazaar.OnTick" />.
///
///     Замены:
///     <list type="bullet">
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />.</item>
///         <item>
///             <c>ScriptCompiler.FindTypeByName</c> в этом репозитории нет —
///             <see cref="AssemblyHandler.FindTypeByName" />. Старые сейвы ServUO не переносятся, поэтому
///             ветка формата версии 0 (в ServUO она осталась для совместимости) не нужна.
///         </item>
///         <item>
///             При передаче питомца на склад он снимается из списка стойл владельца: иначе маклер, у которого
///             <c>HasValidEntry</c> только что отправил питомца в стойла, клал бы его ещё и в склад, и питомец
///             висел бы в двух местах сразу.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

public class StorageEntry
{
    public StorageEntry(Mobile owner, BaseBazaarBroker broker) => AddInventory(owner, broker);

    private StorageEntry()
    {
    }

    public int Funds { get; set; }

    public DateTime Expires { get; private set; }

    public Dictionary<Type, int> CommodityTypes { get; } = new();

    public List<BaseCreature> Creatures { get; } = new();

    public void AddInventory(Mobile owner, BaseBazaarBroker broker)
    {
        Funds += broker.BankBalance;
        Expires = Core.Now + TimeSpan.FromDays(7);

        switch (broker)
        {
            case CommodityBroker commodity:
                {
                    foreach (var entry in commodity.CommodityEntries)
                    {
                        if (entry.Stock > 0)
                        {
                            CommodityTypes[entry.CommodityType] = entry.Stock;
                        }
                    }

                    break;
                }
            case PetBroker petBroker:
                {
                    foreach (var entry in petBroker.BrokerEntries)
                    {
                        if (entry.Pet == null)
                        {
                            continue;
                        }

                        if (entry.Pet.Map != Map.Internal || !entry.Pet.IsStabled)
                        {
                            entry.Internalize();
                        }

                        // Стойла игрока и склад — два разных хозяина питомца; `HasValidEntry` маклера мог
                        // только что отправить его в стойла, поэтому из списка стойл он уходит.
                        if (owner is PlayerMobile pm)
                        {
                            pm.RemoveStabled(entry.Pet);
                        }

                        Creatures.Add(entry.Pet);
                    }

                    break;
                }
        }
    }

    public void RemoveCommodity(Type type, int amount)
    {
        if (!CommodityTypes.TryGetValue(type, out var left))
        {
            return;
        }

        left -= amount;

        if (left <= 0)
        {
            CommodityTypes.Remove(type);
        }
        else
        {
            CommodityTypes[type] = left;
        }
    }

    public void RemovePet(BaseCreature pet) => Creatures.Remove(pet);

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(Funds);
        writer.Write(Expires);

        writer.WriteEncodedInt(CommodityTypes.Count);

        foreach (var (type, amount) in CommodityTypes)
        {
            writer.Write(type.Name);
            writer.Write(amount);
        }

        writer.WriteEncodedInt(Creatures.Count);

        foreach (var creature in Creatures)
        {
            writer.Write(creature);
        }
    }

    public static StorageEntry Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var entry = new StorageEntry
        {
            Funds = reader.ReadInt(),
            Expires = reader.ReadDateTime()
        };

        var commodityCount = reader.ReadEncodedInt();

        for (var i = 0; i < commodityCount; i++)
        {
            var typeName = reader.ReadString();
            var amount = reader.ReadInt();
            var type = typeName == null ? null : AssemblyHandler.FindTypeByName(typeName);

            if (type != null)
            {
                entry.CommodityTypes[type] = amount;
            }
        }

        var creatureCount = reader.ReadEncodedInt();

        for (var i = 0; i < creatureCount; i++)
        {
            var creature = reader.ReadEntity<BaseCreature>();

            if (creature != null)
            {
                entry.Creatures.Add(creature);
            }
        }

        return entry;
    }
}
