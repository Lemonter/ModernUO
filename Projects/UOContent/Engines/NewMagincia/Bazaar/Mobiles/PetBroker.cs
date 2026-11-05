/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Mobiles/PetBroker.cs).
///
///     Животный маклер: держит до 10 приручённых питомцев на продажу, показывает их в загоне на осмотр и
///     передаёт покупателю в стойла.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>from.CloseGump(typeof(X))</c> / <c>new X(...)</c> → статические <c>X.DisplayTo(from, …)</c>
///             обычных <see cref="Server.Gumps.Gump" />-ов.
///         </item>
///         <item>
///             <c>from.Stabled</c> в ModernUO есть только у <see cref="Server.Mobiles.PlayerMobile" /> и это
///             <c>HashSet&lt;Mobile&gt;</c>, поэтому пополнение/удаление идёт через
///             <c>AddStabled</c>/<c>RemoveStabled</c>, а не через <c>List</c>.
///         </item>
///         <item>
///             Список питомцев не пишет <c>[SerializationGenerator]</c>: <see cref="PetBrokerEntry" /> — не
///             сущность, поэтому позиции пишет <see cref="MaginciaBazaarPlot.Serialize" /> через
///             <see cref="SerializeEntries" />.
///         </item>
///         <item>
///             Внутренний таймер показа питомца в загоне заменён токенами <see cref="TimerExecutionToken" />:
///             <c>Timer.Stop()</c> в ModernUO нет.
///         </item>
///         <item><c>String.Format</c> в аргументах cliloc → интерполяция с приведением к <c>string</c>.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class PetBroker : BaseBazaarBroker
{
    public static readonly int MaxEntries = 10;

    private static readonly Dictionary<BaseCreature, TimerExecutionToken> _viewTimers = new();

    private List<PetBrokerEntry> _brokerEntries = new();

    [Constructible]
    public PetBroker() : this(null)
    {
    }

    public PetBroker(MaginciaBazaarPlot plot) : base(plot) => FollowersMax = 500;

    public List<PetBrokerEntry> BrokerEntries => _brokerEntries;

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(Location, 4) || Plot == null)
        {
            base.OnDoubleClick(from);
            return;
        }

        if (Plot.Owner == from)
        {
            PetBrokerGump.DisplayTo(from, this);
        }
        else
        {
            PetInventoryGump.DisplayTo(from, this);
        }
    }

    public bool TryAddEntry(BaseCreature bc, Mobile from, int cost)
    {
        if (bc == null || HasEntry(bc) || !bc.Alive || !bc.IsStabled)
        {
            // That pet is not in the stables...
            from.SendLocalizedMessage(1150342);
        }
        else if (_brokerEntries.Count >= MaxEntries)
        {
            // You cannot add more pets to this animal broker's inventory at this time...
            from.SendLocalizedMessage(1150631);
        }
        else if (from is not PlayerMobile pm || pm.Stabled?.Contains(bc) != true)
        {
            // Transferring the pet from the stables to the animal broker's inventory failed...
            from.SendLocalizedMessage(1150344);
        }
        else
        {
            _brokerEntries.Add(new PetBrokerEntry(bc, cost));
            return true;
        }

        return false;
    }

    public void RemoveEntry(PetBrokerEntry entry)
    {
        if (entry != null)
        {
            _brokerEntries.Remove(entry);
        }
    }

    public override bool HasValidEntry(Mobile m)
    {
        var hasValid = false;
        var hasRoom = m is PlayerMobile pm && (pm.Stabled?.Count ?? 0) < AnimalTrainer.GetMaxStabled(m);

        foreach (var entry in _brokerEntries)
        {
            if (entry.Pet == null)
            {
                continue;
            }

            if (hasRoom)
            {
                SendToStables(m, entry.Pet);
            }
            else
            {
                hasValid = true;
            }
        }

        return hasValid;
    }

    public bool HasEntry(BaseCreature bc)
    {
        foreach (var entry in _brokerEntries)
        {
            if (entry.Pet == bc)
            {
                return true;
            }
        }

        return false;
    }

    public void CheckInventory() => _brokerEntries.RemoveAll(entry => entry.Pet == null || entry.Pet.Deleted);

    public override int GetWeeklyFee()
    {
        var total = 0;

        foreach (var entry in _brokerEntries)
        {
            if (entry.SalePrice > 0)
            {
                total += entry.SalePrice;
            }
        }

        return (int)(total * 0.05);
    }

    /// <summary>Возвращает номер cliloc ошибки либо 0, если покупка состоялась.</summary>
    public int TryBuyPet(Mobile from, PetBrokerEntry entry)
    {
        if (from == null || entry?.Pet == null || from is not PlayerMobile pm)
        {
            return 1150377; // Unable to complete the desired transaction at this time.
        }

        var cost = entry.SalePrice;
        var toAdd = cost - (int)(cost * ((double)CommissionFee / 100.0));
        var pet = entry.Pet;

        if (!_brokerEntries.Contains(entry) || pet.Deleted)
        {
            return 1150377; // Unable to complete the desired transaction at this time.
        }

        if (pet.GetControlChance(from) <= 0.0)
        {
            return 1150379; // Unable to transfer that pet to you because you have no chance at all of controlling it.
        }

        if ((pm.Stabled?.Count ?? 0) >= AnimalTrainer.GetMaxStabled(from))
        {
            return 1150376; // You do not have any available stable slots...
        }

        if (!Banker.Withdraw(from, cost))
        {
            return 1150252; // You do not have the funds needed to make this trade...
        }

        BankBalance += toAdd;
        pet.IsBonded = false;

        SendToStables(from, pet);

        // You have purchased ~1_TYPE~ named "~2_NAME~"...
        from.SendLocalizedMessage(1150380, $"{entry.TypeName}\t{pet.Name}");
        _brokerEntries.Remove(entry);
        return 0;
    }

    public static void SendToStables(Mobile to, BaseCreature pet)
    {
        if (pet == null || to is not PlayerMobile pm)
        {
            return;
        }

        EndViewTimer(pet);

        pet.Blessed = false;
        pet.ControlTarget = null;
        pet.ControlOrder = OrderType.Stay;
        pet.Internalize();
        pet.SetControlMaster(null);
        pet.SummonMaster = null;
        pet.IsStabled = true;
        pet.StabledBy = to;
        pet.Loyalty = BaseCreature.MaxLoyalty;

        pm.AddStabled(pet);
    }

    private static void SendToBrokerStables(BaseCreature pet)
    {
        // Запись таймера снимается всегда, иначе таблица показа копила бы удалённых питомцев.
        EndViewTimer(pet);

        if (pet == null || pet.Deleted)
        {
            return;
        }

        if (pet is BaseMount mount)
        {
            mount.Rider = null;
        }

        pet.ControlTarget = null;
        pet.ControlOrder = OrderType.Stay;
        pet.Internalize();

        pet.SetControlMaster(null);
        pet.SummonMaster = null;

        pet.IsStabled = true;
        pet.Loyalty = BaseCreature.MaxLoyalty;

        pet.Home = Point3D.Zero;
        pet.RangeHome = 10;
        pet.Blessed = false;
    }

    /// <summary>Питомец стоит в загоне маклера не дольше двух минут, потом возвращается «в стойло».</summary>
    public static void AddToViewTimer(BaseCreature bc)
    {
        EndViewTimer(bc);
        Timer.StartTimer(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2), () => SendToBrokerStables(bc), out var token);
        _viewTimers[bc] = token;
    }

    public static void EndViewTimer(BaseCreature bc)
    {
        if (_viewTimers.Remove(bc, out var token))
        {
            token.Cancel();
        }
    }

    public void SerializeEntries(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_brokerEntries.Count);

        foreach (var entry in _brokerEntries)
        {
            entry.Serialize(writer);
        }
    }

    public static void DeserializeEntries(IGenericReader reader, PetBroker broker)
    {
        var count = reader.ReadEncodedInt();

        broker?._brokerEntries.Clear();

        for (var i = 0; i < count; i++)
        {
            var entry = PetBrokerEntry.Deserialize(reader);

            if (entry != null)
            {
                broker?._brokerEntries.Add(entry);
            }
        }
    }
}
