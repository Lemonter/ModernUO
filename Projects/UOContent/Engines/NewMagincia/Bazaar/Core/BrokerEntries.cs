/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Core/BrokerEntries.cs).
///
///     Строки прилавков: товарная позиция маклера (<see cref="CommodityBrokerEntry" />) и питомец на
///     продажу (<see cref="PetBrokerEntry" />). Обе живут в памяти и пишутся <see cref="BazaarPersistence" />
///     через своих маклеров.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>ScriptCompiler.FindTypeByName</c> в этом репозитории нет — используется
///             <see cref="AssemblyHandler.FindTypeByName" />.
///         </item>
///         <item>
///             <c>ICommodity.Description</c> → <see cref="ICommodity.DescriptionNumber" />: это поле
///             переименовано в ModernUO.
///         </item>
///         <item>
///             <c>Activator.CreateInstance</c> для получения исходного имени питомца обёрнут в <c>try</c>:
///             часть существ в ModernUO требует данных о скоростях и бросает в конструкторе, а падать из-за
///             имени в списке продажи нельзя — тогда берётся имя типа.
///         </item>
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" />; <c>Timer.DelayCall</c> → <c>Timer.StartTimer</c>.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

public class CommodityBrokerEntry
{
    private CommodityBrokerEntry()
    {
    }

    /*
     *	SellPricePer - цена за единицу, по которой маклер продаёт игрокам
     *	BuyPricePer - цена за единицу, по которой маклер покупает у игроков
     *	BuyLimit - сколько единиц маклер купит у игроков
     *	SellLimit - сколько единиц маклер продаст игрокам
     */
    public CommodityBrokerEntry(Item item, CommodityBroker broker, int amount)
    {
        CommodityType = item.GetType();
        ItemID = item.ItemID;
        Broker = broker;
        Stock = amount;
        Label = item is ICommodity commodity ? commodity.DescriptionNumber : item.LabelNumber;
    }

    public Type CommodityType { get; private set; }

    public CommodityBroker Broker { get; private set; }

    public int ItemID { get; private set; }

    public int Label { get; private set; }

    public int SellPricePer { get; set; }

    public int BuyPricePer { get; set; }

    public int BuyLimit { get; set; }

    public int SellLimit { get; set; }

    public int Stock { get; set; }

    public int ActualSellLimit => Stock < SellLimit ? Stock : SellLimit;

    public int ActualBuyLimit
    {
        get
        {
            if (Broker != null && Broker.BankBalance < BuyLimit * BuyPricePer && BuyPricePer > 0)
            {
                return Broker.BankBalance / BuyPricePer;
            }

            var limit = BuyLimit - Stock;

            return limit <= 0 ? 0 : limit;
        }
    }

    /// <summary>Игрок покупает, маклер продаёт.</summary>
    public bool PlayerCanBuy(int amount) => (SellLimit == 0 || amount <= ActualSellLimit) && SellPricePer > 0;

    /// <summary>Игрок продаёт, маклер покупает.</summary>
    public bool PlayerCanSell(int amount) =>
        (BuyLimit == 0 || amount <= ActualBuyLimit) && Stock > 0 && BuyPricePer > 0 && BuyPricePer <= Broker.BankBalance;

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(CommodityType?.Name);
        writer.Write(Label);
        writer.Write(Broker);
        writer.Write(ItemID);
        writer.Write(SellPricePer);
        writer.Write(BuyPricePer);
        writer.Write(BuyLimit);
        writer.Write(SellLimit);
        writer.Write(Stock);
    }

    public static CommodityBrokerEntry Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var typeName = reader.ReadString();
        var label = reader.ReadInt();
        var broker = reader.ReadEntity<CommodityBroker>();
        var itemId = reader.ReadInt();
        var sellPricePer = reader.ReadInt();
        var buyPricePer = reader.ReadInt();
        var buyLimit = reader.ReadInt();
        var sellLimit = reader.ReadInt();
        var stock = reader.ReadInt();

        var type = typeName == null ? null : AssemblyHandler.FindTypeByName(typeName);

        if (type == null)
        {
            return null;
        }

        return new CommodityBrokerEntry
        {
            CommodityType = type,
            Label = label,
            Broker = broker,
            ItemID = itemId,
            SellPricePer = sellPricePer,
            BuyPricePer = buyPricePer,
            BuyLimit = buyLimit,
            SellLimit = sellLimit,
            Stock = stock
        };
    }
}

public class PetBrokerEntry
{
    public static readonly int DefaultPrice = 1000;

    private static readonly Dictionary<Type, string> _nameBuffer = new();

    private PetBrokerEntry()
    {
    }

    public PetBrokerEntry(BaseCreature pet) : this(pet, DefaultPrice)
    {
    }

    public PetBrokerEntry(BaseCreature pet, int price)
    {
        Pet = pet;
        SalePrice = price;
        TypeName = GetOriginalName(pet);
    }

    public BaseCreature Pet { get; private set; }

    public int SalePrice { get; set; }

    public string TypeName { get; set; }

    /// <summary>Кэш исходных имён по типу: экземпляр существа ради имени создаётся один раз на тип.</summary>
    public static Dictionary<Type, string> NameBuffer => _nameBuffer;

    public static string GetOriginalName(BaseCreature bc)
    {
        if (bc == null)
        {
            return null;
        }

        var type = bc.GetType();

        if (_nameBuffer.TryGetValue(type, out var cached))
        {
            return cached;
        }

        string name = null;
        BaseCreature probe = null;

        try
        {
            probe = Activator.CreateInstance(type) as BaseCreature;
            name = probe?.Name;
        }
        catch
        {
            // Существа без конструктора без параметров (или без данных о скоростях) имени не дадут.
        }
        finally
        {
            probe?.Delete();
        }

        name ??= type.Name;

        AddToBuffer(type, name);
        return name;
    }

    public static void AddToBuffer(Type type, string name)
    {
        if (!string.IsNullOrEmpty(name) && !_nameBuffer.ContainsKey(type))
        {
            _nameBuffer[type] = name;
        }
    }

    /// <summary>Убирает питомца с глаз долой: он лежит в «стойле» маклера до продажи или возврата.</summary>
    public void Internalize()
    {
        if (Pet == null || Pet.Deleted || Pet.Map == Map.Internal)
        {
            return;
        }

        Pet.ControlTarget = null;
        Pet.ControlOrder = OrderType.Stay;
        Pet.Internalize();

        Pet.SetControlMaster(null);
        Pet.SummonMaster = null;

        Pet.IsStabled = true;
        Pet.Loyalty = BaseCreature.MaxLoyalty;

        Pet.Home = Point3D.Zero;
        Pet.RangeHome = 10;
        Pet.Blessed = false;
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.Write(Pet);
        writer.Write(SalePrice);
        writer.Write(TypeName);
    }

    public static PetBrokerEntry Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var pet = reader.ReadEntity<BaseCreature>();
        var salePrice = reader.ReadInt();
        var typeName = reader.ReadString();

        var entry = new PetBrokerEntry
        {
            Pet = pet,
            SalePrice = salePrice,
            TypeName = typeName ?? pet?.GetType().Name
        };

        if (pet != null)
        {
            AddToBuffer(pet.GetType(), entry.TypeName);
            pet.IsStabled = true;

            Timer.StartTimer(TimeSpan.FromSeconds(10), entry.Internalize);
        }

        return entry;
    }
}
