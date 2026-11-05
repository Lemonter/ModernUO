/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Gumps/PetBrokerGumps.cs).
///
///     Окна животного маклера: меню владельца, передача питомцев из стойл маклеру и обратно, цены, витрина с
///     осмотром и покупкой.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Все окна — обычные <see cref="Gump" /> со статическим <c>DisplayTo</c>; <c>TextRelay</c> →
///             <c>info.GetTextEntry(id)</c>, <c>Convert.ToInt32</c> → <c>int.TryParse</c>.
///         </item>
///         <item>
///             <c>from.Stabled</c> есть только у <see cref="Server.Mobiles.PlayerMobile" /> (и это
///             <c>HashSet&lt;Mobile&gt;</c>), поэтому список питомцев собирается по нему, а удаление идёт через
///             <c>RemoveStabled</c>.
///         </item>
///         <item>
///             Ветка <c>PetTrainingHelper</c>/<c>NewAnimalLoreGump</c> из ServUO не портирована: в этом
///             репозитории такого помощника нет. Осмотр питомца открывает <c>AnimalLoreGump</c> — он есть и
///             показывает то же окно лора.
///         </item>
///         <item>Собственные подписи переведены на русский, идентификаторы cliloc не тронуты.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;

namespace Server.Engines.NewMagincia;

public class PetBrokerGump : BaseBazaarGump
{
    private readonly PetBroker _broker;

    private PetBrokerGump(Mobile from, PetBroker broker) : base(520, 520)
    {
        _broker = broker;

        AddHtmlLocalized(215, 10, 200, 18, 1150311, RedColor16, false, false); // Animal Broker

        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(173, 40, 173, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(180, 40, 200, 18, 1150314, BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtml(173, 65, 173, 18, Color(FormatBrokerName($"Владелец: {broker.Name}"), BlueColor), false, false);

        AddHtmlLocalized(215, 100, 200, 18, 1150328, GreenColor16, false, false); // Owner Menu

        AddButton(150, 150, 4005, 4007, 1);
        AddHtmlLocalized(190, 150, 200, 18, 1150392, OrangeColor16, false, false); // INFORMATION

        AddHtmlLocalized(39, 180, 200, 18, 1150199, RedColor16, false, false); // Broker Account Balance
        AddHtml(190, 180, 300, 18, FormatAmt(broker.BankBalance), false, false);

        var balance = Banker.GetBalance(from);
        AddHtmlLocalized(68, 200, 200, 18, 1150149, GreenColor16, false, false); // Your Bank Balance:
        AddHtml(190, 200, 200, 18, FormatAmt(balance), false, false);

        AddHtmlLocalized(32, 230, 200, 18, 1150329, OrangeColor16, false, false); // Broker Sales Comission
        AddHtmlLocalized(190, 230, 100, 18, 1150330, false, false); // 5%

        AddHtmlLocalized(110, 250, 200, 18, 1150331, OrangeColor16, false, false); // Weekly Fee:
        AddHtml(190, 250, 250, 18, FormatAmt(broker.GetWeeklyFee()), false, false);

        AddHtmlLocalized(113, 280, 200, 18, 1150332, OrangeColor16, false, false); // Shop Name:
        AddBackground(190, 280, 285, 22, 9350);
        AddTextEntry(191, 280, 285, 20, LabelHueBlue, 0, broker.Plot?.ShopName ?? "");
        AddButton(480, 280, 4014, 4016, 2);

        AddHtmlLocalized(83, 305, 150, 18, 1150195, OrangeColor16, false, false); // Withdraw Funds
        AddBackground(190, 305, 285, 22, 9350);
        AddTextEntry(191, 305, 285, 20, LabelHueBlue, 1, "");
        AddButton(480, 305, 4014, 4016, 3);

        AddHtmlLocalized(95, 330, 150, 18, 1150196, OrangeColor16, false, false); // Deposit Funds
        AddBackground(190, 330, 285, 22, 9350);
        AddTextEntry(191, 330, 285, 20, LabelHueBlue, 2, "");
        AddButton(480, 330, 4014, 4016, 4);

        AddButton(150, 365, 4005, 4007, 5);
        AddHtmlLocalized(190, 365, 350, 18, 1150276, OrangeColor16, false, false); // TRANSFER STABLED PETS TO MERCHANT

        AddButton(150, 390, 4005, 4007, 6);
        AddHtmlLocalized(190, 390, 350, 18, 1150277, OrangeColor16, false, false); // TRANSFER MERCHANT INVENTORY TO STABLE

        AddButton(150, 415, 4005, 4007, 7);
        AddHtmlLocalized(190, 415, 350, 18, 1150334, OrangeColor16, false, false); // VIEW INVENTORY / EDIT PRICES
    }

    public static void DisplayTo(Mobile from, PetBroker broker)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<PetBrokerGump>();
        from.SendGump(new PetBrokerGump(from, broker));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false || _broker.Plot == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 1:
                {
                    BazaarInformationGump.DisplayTo(from, 1150311, 1150614, new PetBrokerGump(from, _broker));
                    return;
                }
            case 2:
                {
                    if (!_broker.Plot.TrySetShopName(from, info.GetTextEntry(0)))
                    {
                        // Shop names are limited to 40 characters... The text you have entered is not valid.
                        from.SendLocalizedMessage(1150775);
                    }

                    break;
                }
            case 3:
                {
                    if (int.TryParse(info.GetTextEntry(1), out var amount) && amount > 0)
                    {
                        _broker.TryWithdrawFunds(from, amount);
                    }

                    break;
                }
            case 4:
                {
                    if (int.TryParse(info.GetTextEntry(2), out var amount) && amount > 0)
                    {
                        _broker.TryDepositFunds(from, amount);
                    }

                    break;
                }
            case 5:
                {
                    if (from is PlayerMobile pm && pm.Stabled?.Count > 0)
                    {
                        SelectPetsGump.DisplayTo(from, _broker);
                    }
                    else
                    {
                        // You currently have no pets in your stables that can be traded via an animal broker.
                        from.SendLocalizedMessage(1150335);
                    }

                    return;
                }
            case 6:
                {
                    _broker.CheckInventory();

                    if (_broker.BrokerEntries.Count == 0)
                    {
                        // The animal broker has no pets in its inventory.
                        from.SendLocalizedMessage(1150336);
                        return;
                    }

                    if (_broker.BankBalance < 0)
                    {
                        BazaarInformationGump.DisplayTo(from, 1150623, 1150615);
                        return;
                    }

                    RemovePetsGump.DisplayTo(from, _broker);
                    return;
                }
            case 7:
                {
                    _broker.CheckInventory();

                    if (_broker.BrokerEntries.Count == 0)
                    {
                        from.SendLocalizedMessage(1150336);
                        return;
                    }

                    if (_broker.BankBalance < 0)
                    {
                        BazaarInformationGump.DisplayTo(from, 1150623, 1150615);
                        return;
                    }

                    SetPetPricesGump.DisplayTo(from, _broker);
                    return;
                }
        }

        DisplayTo(from, _broker);
    }
}

public class SelectPetsGump : BaseBazaarGump
{
    private readonly PetBroker _broker;
    private readonly int _index;
    private readonly List<BaseCreature> _list;

    private SelectPetsGump(Mobile from, PetBroker broker, int index) : base(520, 520)
    {
        _broker = broker;
        _index = index;

        AddHtmlLocalized(215, 10, 200, 18, 1150311, RedColor16, false, false); // Animal Broker
        AddHtmlLocalized(145, 50, 250, 18, 1150337, RedColor16, false, false); // ADD PET TO BROKER INVENTORY
        AddHtmlLocalized(10, 100, 500, 40, 1150338, GreenColor16, false, false); // Click the button next to a pet...

        _list = GetList(from);

        if (_list.Count == 0)
        {
            // You currently have no pets in your stables that can be traded via an animal broker.
            AddHtmlLocalized(10, 150, 500, 40, 1150335, OrangeColor16, false, false);
        }

        var y = 150;

        for (var i = 0; i < _list.Count && i < 10; i++)
        {
            var pet = _list[i];
            var color = index == i ? YellowColor16 : OrangeColor16;

            AddButton(10, y, 4005, 4007, i + 1);
            AddHtmlLocalized(60, y, 200, 18, 1150340, $"{pet.Name}\t{PetBrokerEntry.GetOriginalName(pet)}", color, false, false); // ~1_NAME~ (~2_type~)
            y += 22;
        }

        AddHtmlLocalized(215, 380, 100, 18, 1150339, OrangeColor16, false, false); // ADD PET
        AddButton(175, 405, 4005, 4007, 501);
        AddBackground(215, 405, 295, 22, 9350);
        AddTextEntry(216, 405, 294, 20, 0, 0, "");

        AddButton(10, 490, 4014, 4016, 500);
        AddHtmlLocalized(50, 490, 100, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, PetBroker broker, int index = -1)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<SelectPetsGump>();
        from.SendGump(new SelectPetsGump(from, broker, index));
    }

    /// <summary>Стойла игрока, из которых убраны удалённые питомцы.</summary>
    private static List<BaseCreature> GetList(Mobile from)
    {
        var list = new List<BaseCreature>();

        if (from is not PlayerMobile pm || pm.Stabled == null)
        {
            return list;
        }

        List<Mobile> removed = null;

        foreach (var mobile in pm.Stabled)
        {
            if (mobile is BaseCreature pet && !pet.Deleted)
            {
                list.Add(pet);
                continue;
            }

            if (mobile != null)
            {
                if (mobile is BaseCreature creature)
                {
                    creature.IsStabled = false;
                }
            }

            (removed ??= new List<Mobile>()).Add(mobile);
        }

        if (removed != null)
        {
            foreach (var mobile in removed)
            {
                pm.RemoveStabled(mobile);
            }
        }

        return list;
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 500:
                {
                    PetBrokerGump.DisplayTo(from, _broker);
                    return;
                }
            case 501:
                {
                    AddPet(from, info);
                    return;
                }
            default:
                {
                    DisplayTo(from, _broker, info.ButtonID - 1);
                    return;
                }
        }
    }

    private void AddPet(Mobile from, in RelayInfo info)
    {
        if (_index < 0 || _index >= _list.Count)
        {
            // You did not select a pet.
            from.SendLocalizedMessage(1150341);
            PetBrokerGump.DisplayTo(from, _broker);
            return;
        }

        if (_broker.BankBalance < 0)
        {
            BazaarInformationGump.DisplayTo(from, 1150623, 1150615);
            return;
        }

        var cost = int.TryParse(info.GetTextEntry(0), out var parsed) ? parsed : PetBrokerEntry.DefaultPrice;

        if (cost <= 0)
        {
            // You have entered an invalid price.
            from.SendLocalizedMessage(1150343);
            DisplayTo(from, _broker, _index);
            return;
        }

        var pet = _list[_index];

        if (!_broker.TryAddEntry(pet, from, cost))
        {
            DisplayTo(from, _broker, _index);
            return;
        }

        if (from is PlayerMobile pm)
        {
            pm.RemoveStabled(pet);
        }

        SetPetPricesGump.DisplayTo(from, _broker);

        // Your pet ~1_TYPE~ named ~2_NAME~ has been transferred to the inventory of your animal broker...
        from.SendLocalizedMessage(
            1150345,
            $"{PetBrokerEntry.GetOriginalName(pet)}\t{pet.Name}\t{_broker.Name}\t{cost.ToString()}"
        );
    }
}

public class RemovePetsGump : BaseBazaarGump
{
    private readonly PetBroker _broker;
    private readonly int _index;

    private RemovePetsGump(Mobile from, PetBroker broker, int index) : base(520, 520)
    {
        _broker = broker;
        _index = index;

        AddHtmlLocalized(215, 10, 200, 18, 1150311, RedColor16, false, false); // Animal Broker
        AddHtmlLocalized(145, 50, 250, 18, 1150337, RedColor16, false, false); // ADD PET TO BROKER INVENTORY
        // Click the button next to a pet to select it, then click the REMOVE PET button below...
        AddHtmlLocalized(10, 80, 500, 40, 1150633, GreenColor16, false, false);

        broker.CheckInventory();

        var y = 130;

        for (var i = 0; i < broker.BrokerEntries.Count && i < 12; i++)
        {
            var pet = broker.BrokerEntries[i].Pet;

            if (pet == null)
            {
                continue;
            }

            var color = index == i ? YellowColor16 : OrangeColor16;
            AddButton(10, y, 4005, 4007, i + 1);
            AddHtmlLocalized(50, y, 200, 18, 1150340, $"{pet.Name}\t{PetBrokerEntry.GetOriginalName(pet)}", color, false, false); // ~1_NAME~ (~2_type~)
            y += 20;
        }

        AddHtmlLocalized(215, 405, 150, 18, 1150632, OrangeColor16, false, false); // REMOVE PET
        AddButton(175, 405, 4014, 4016, 501);

        AddButton(10, 490, 4014, 4016, 500);
        AddHtmlLocalized(50, 490, 100, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, PetBroker broker, int index = -1)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<RemovePetsGump>();
        from.SendGump(new RemovePetsGump(from, broker, index));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 500:
                {
                    PetBrokerGump.DisplayTo(from, _broker);
                    return;
                }
            case 501:
                {
                    RemovePet(from);
                    return;
                }
            default:
                {
                    DisplayTo(from, _broker, info.ButtonID - 1);
                    return;
                }
        }
    }

    private void RemovePet(Mobile from)
    {
        if (_index < 0 || _index >= _broker.BrokerEntries.Count)
        {
            // You did not select a pet.
            from.SendLocalizedMessage(1150341);
            DisplayTo(from, _broker, _index);
            return;
        }

        var entry = _broker.BrokerEntries[_index];

        var hasRoom = from is PlayerMobile pm && (pm.Stabled?.Count ?? 0) < AnimalTrainer.GetMaxStabled(from);

        if (!hasRoom || entry.Pet == null)
        {
            // Failed to transfer the selected pet to your stables...
            from.SendLocalizedMessage(1150634);
            DisplayTo(from, _broker, _index);
            return;
        }

        var pet = entry.Pet;
        _broker.RemoveEntry(entry);

        PetBroker.SendToStables(from, pet);

        // Your pet ~1_TYPE~ named ~2_NAME~ has been transferred to the stables.
        from.SendLocalizedMessage(1150635, $"{entry.TypeName}\t{pet.Name}");
        PetBrokerGump.DisplayTo(from, _broker);
    }
}

public class SetPetPricesGump : BaseBazaarGump
{
    private readonly PetBroker _broker;
    private readonly int _index;

    private SetPetPricesGump(PetBroker broker, int index) : base(520, 520)
    {
        _broker = broker;
        _index = index;

        AddHtmlLocalized(215, 10, 200, 18, 1150311, RedColor16, false, false); // Animal Broker

        AddHtmlLocalized(60, 90, 100, 18, 1150347, OrangeColor16, false, false); // NAME
        AddHtmlLocalized(220, 90, 100, 18, 1150348, OrangeColor16, false, false); // TYPE
        AddHtmlLocalized(400, 90, 100, 18, 1150349, OrangeColor16, false, false); // PRICE

        broker.CheckInventory();

        var y = 130;

        for (var i = 0; i < broker.BrokerEntries.Count && i < 10; i++)
        {
            var entry = broker.BrokerEntries[i];

            if (entry?.Pet == null)
            {
                continue;
            }

            var color = index == i ? YellowColor : OrangeColor;

            AddHtml(60, y, 200, 18, Color(entry.Pet.Name ?? "Неизвестно", color), false, false);
            AddHtml(220, y, 200, 18, Color(entry.TypeName, color), false, false);
            AddHtml(400, y, 200, 18, Color(FormatAmt(entry.SalePrice), color), false, false);
            AddButton(10, y, 4005, 4007, i + 3);

            y += 22;
        }

        var price = index >= 0 && index < broker.BrokerEntries.Count ? broker.BrokerEntries[index].SalePrice : 0;

        AddHtmlLocalized(215, 380, 150, 18, 1150627, BlueColor16, false, false); // SET PRICE
        AddBackground(215, 405, 295, 22, 9350);
        AddTextEntry(216, 405, 294, 20, 0, 0, FormatAmt(price));
        AddButton(175, 405, 4005, 4007, 500);

        AddButton(10, 490, 4014, 4016, 1);
        AddHtmlLocalized(50, 490, 100, 18, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, PetBroker broker, int index = -1)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<SetPetPricesGump>();
        from.SendGump(new SetPetPricesGump(broker, index));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 1:
                {
                    PetBrokerGump.DisplayTo(from, _broker);
                    return;
                }
            case 500:
                {
                    SetPrice(from, info);
                    DisplayTo(from, _broker);
                    return;
                }
            default:
                {
                    var id = info.ButtonID - 3;

                    if (id >= 0 && id < _broker.BrokerEntries.Count)
                    {
                        DisplayTo(from, _broker, id);
                        return;
                    }

                    DisplayTo(from, _broker);
                    return;
                }
        }
    }

    private void SetPrice(Mobile from, in RelayInfo info)
    {
        if (_index < 0 || _index >= _broker.BrokerEntries.Count)
        {
            // You did not select a pet.
            from.SendLocalizedMessage(1150341);
            return;
        }

        if (!int.TryParse(info.GetTextEntry(0), out var amount) || amount <= 0)
        {
            // You have entered an invalid price.
            from.SendLocalizedMessage(1150343);
            return;
        }

        _broker.BrokerEntries[_index].SalePrice = amount;
    }
}

public class PetInventoryGump : BaseBazaarGump
{
    private readonly PetBroker _broker;
    private readonly List<PetBrokerEntry> _entries;

    private PetInventoryGump(PetBroker broker) : base(520, 520)
    {
        _broker = broker;
        _entries = broker.BrokerEntries;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150311", RedColor16, false, false); // Animal Broker

        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(10, 37, 500, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(10, 37, 500, 18, 1114513, "#1150314", BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 55, 240, 18, 1114514, "#1150313", BlueColor16, false, false); // Proprietor:
        AddHtml(260, 55, 250, 18, Color(broker.Name, BlueColor), false, false);

        if (_entries.Count == 0)
        {
            // The animal broker has no pets in its inventory.
            AddHtmlLocalized(10, 127, 500, 300, 1114513, "#1150336", OrangeColor16, false, false);

            AddButton(10, 490, 0xFAE, 0xFAF, 1);
            AddHtmlLocalized(50, 490, 210, 20, 1149777, BlueColor16, false, false); // MAIN MENU
            return;
        }

        AddHtmlLocalized(10, 91, 500, 18, 1114513, "#1150346", GreenColor16, false, false); // PETS FOR SALE
        AddHtmlLocalized(10, 118, 500, 72, 1114513, "#1150352", GreenColor16, false, false); // LORE: See the animal's

        AddHtmlLocalized(10, 199, 52, 18, 1150351, OrangeColor16, false, false); // LORE
        AddHtmlLocalized(68, 199, 52, 18, 1150353, OrangeColor16, false, false); // VIEW
        AddHtmlLocalized(126, 199, 104, 18, 1150347, OrangeColor16, false, false); // NAME
        AddHtmlLocalized(236, 199, 104, 18, 1150348, OrangeColor16, false, false); // TYPE
        AddHtmlLocalized(346, 199, 104, 18, 1114514, "#1150349", OrangeColor16, false, false); // PRICE
        AddHtmlLocalized(456, 199, 52, 18, 1150350, OrangeColor16, false, false); // BUY

        for (var i = 0; i < _entries.Count && i < 13; i++)
        {
            var entry = _entries[i];

            if (entry?.Pet == null)
            {
                continue;
            }

            var y = 219 + i * 20;

            AddButton(10, y, 4011, 4013, 100 + i);
            AddButton(68, y, 4008, 4010, 200 + i);

            AddHtml(126, y, 104, 14, Color(entry.Pet.Name, BlueColor), false, false);
            AddHtml(236, y, 104, 20, Color(entry.TypeName, BlueColor), false, false);
            AddHtml(346, y, 104, 20, Color(AlignRight(FormatAmt(entry.SalePrice)), GreenColor), false, false);

            AddButton(456, y, 4014, 4016, 300 + i);
        }
    }

    public static void DisplayTo(Mobile from, PetBroker broker)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<PetInventoryGump>();
        from.SendGump(new PetInventoryGump(broker));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        if (info.ButtonID == 0)
        {
            return;
        }

        if (info.ButtonID == 1)
        {
            DisplayTo(from, _broker);
            return;
        }

        var id = info.ButtonID % 100;
        var action = info.ButtonID / 100;

        if (id < 0 || id >= _entries.Count)
        {
            DisplayTo(from, _broker);
            return;
        }

        var entry = _entries[id];

        switch (action)
        {
            case 1: // LORE
                {
                    if (entry?.Pet == null || !_broker.BrokerEntries.Contains(entry))
                    {
                        // The selected animal is not available.
                        from.SendLocalizedMessage(1150368);
                        break;
                    }

                    DisplayTo(from, _broker);
                    AnimalLoreGump.DisplayTo(from, entry.Pet);
                    return;
                }
            case 2: // VIEW
                {
                    if (entry?.Pet == null || !_broker.BrokerEntries.Contains(entry) || !entry.Pet.IsStabled)
                    {
                        from.SendLocalizedMessage(1150368);
                        DisplayTo(from, _broker);
                        return;
                    }

                    var pet = entry.Pet;

                    pet.Blessed = true;
                    pet.SetControlMaster(_broker);
                    pet.ControlTarget = _broker;
                    pet.ControlOrder = OrderType.None;
                    pet.MoveToWorld(_broker.Location, _broker.Map);
                    pet.IsStabled = false;
                    pet.Home = pet.Location;
                    pet.RangeHome = 2;
                    pet.Loyalty = BaseCreature.MaxLoyalty;

                    PetBroker.AddToViewTimer(pet);

                    // The ~1_TYPE~ named "~2_NAME~" is now in the animal broker's pen for inspection.
                    from.SendLocalizedMessage(1150369, $"{entry.TypeName}\t{pet.Name}");

                    DisplayTo(from, _broker);
                    return;
                }
            case 3: // BUY
                {
                    if (entry?.Pet != null && _broker.BrokerEntries.Contains(entry))
                    {
                        ConfirmBuyPetGump.DisplayTo(from, _broker, entry);
                    }

                    return;
                }
        }

        DisplayTo(from, _broker);
    }
}

public class ConfirmBuyPetGump : BaseBazaarGump
{
    private readonly PetBroker _broker;
    private readonly PetBrokerEntry _entry;

    private ConfirmBuyPetGump(PetBroker broker, PetBrokerEntry entry) : base(520, 520)
    {
        _broker = broker;
        _entry = entry;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150311", RedColor16, false, false); // Animal Broker

        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(10, 37, 500, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(10, 37, 500, 18, 1114513, "#1150314", BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 55, 240, 18, 1114514, "#1150313", BlueColor16, false, false); // Proprietor:
        AddHtml(260, 55, 250, 18, Color(broker.Name, BlueColor), false, false);

        AddHtmlLocalized(10, 91, 500, 18, 1114513, "#1150375", GreenColor16, false, false); // PURCHASE PET
        AddHtmlLocalized(10, 118, 500, 72, 1114513, "#1150370", GreenColor16, false, false); // Please confirm your purchase order...

        AddHtmlLocalized(10, 235, 245, 18, 1114514, "#1150372", OrangeColor16, false, false); // Animal Name:
        AddHtmlLocalized(10, 255, 245, 18, 1114514, "#1150371", OrangeColor16, false, false); // Animal Type:
        AddHtmlLocalized(10, 275, 245, 18, 1114514, "#1150373", OrangeColor16, false, false); // Sale Price:

        AddHtml(265, 235, 245, 18, Color(entry.Pet?.Name ?? "?", BlueColor), false, false);
        AddHtml(265, 255, 245, 18, Color(entry.TypeName, BlueColor), false, false);
        AddHtml(265, 275, 245, 18, Color(FormatAmt(entry.SalePrice), BlueColor), false, false);

        AddHtmlLocalized(265, 295, 245, 22, 1150374, OrangeColor16, false, false); // ACCEPT
        AddButton(225, 295, 4005, 4007, 1);

        AddButton(10, 490, 0xFAE, 0xFAF, 2);
        AddHtmlLocalized(50, 490, 210, 20, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, PetBroker broker, PetBrokerEntry entry)
    {
        if (from == null || broker?.Deleted != false || entry == null)
        {
            return;
        }

        from.CloseGump<ConfirmBuyPetGump>();
        from.SendGump(new ConfirmBuyPetGump(broker, entry));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        if (from == null || _broker?.Deleted != false)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1:
                {
                    var cliloc = _broker.TryBuyPet(from, _entry);

                    if (cliloc != 0)
                    {
                        PurchasePetGump.DisplayTo(from, _broker, cliloc);
                    }
                    else
                    {
                        PetInventoryGump.DisplayTo(from, _broker);
                    }

                    break;
                }
            case 2:
                {
                    PetInventoryGump.DisplayTo(from, _broker);
                    break;
                }
        }
    }
}

public class PurchasePetGump : BaseBazaarGump
{
    private readonly PetBroker _broker;

    private PurchasePetGump(PetBroker broker, int cliloc) : base(520, 520)
    {
        _broker = broker;

        AddHtmlLocalized(10, 10, 500, 18, 1114513, "#1150311", RedColor16, false, false); // Animal Broker

        if (broker.Plot?.ShopName is { Length: > 0 } shopName)
        {
            AddHtml(10, 37, 500, 18, Color(FormatStallName(shopName), BlueColor), false, false);
        }
        else
        {
            AddHtmlLocalized(10, 37, 500, 18, 1114513, "#1150314", BlueColor16, false, false); // This Shop Has No Name
        }

        AddHtmlLocalized(10, 55, 240, 18, 1114514, "#1150313", BlueColor16, false, false); // Proprietor:
        AddHtml(260, 55, 250, 18, Color(broker.Name, BlueColor), false, false);

        AddHtmlLocalized(10, 127, 500, 340, 1114513, $"#{cliloc}", OrangeColor16, false, false);

        AddButton(10, 490, 0xFAE, 0xFAF, 1);
        AddHtmlLocalized(50, 490, 210, 20, 1149777, BlueColor16, false, false); // MAIN MENU
    }

    public static void DisplayTo(Mobile from, PetBroker broker, int cliloc)
    {
        if (from == null || broker?.Deleted != false)
        {
            return;
        }

        from.CloseGump<PurchasePetGump>();
        from.SendGump(new PurchasePetGump(broker, cliloc));
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1 && state.Mobile != null && _broker?.Deleted == false)
        {
            PetInventoryGump.DisplayTo(state.Mobile, _broker);
        }
    }
}
