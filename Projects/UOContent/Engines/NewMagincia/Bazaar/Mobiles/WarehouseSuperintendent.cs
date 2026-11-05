/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Mobiles/WarehouseSuperintendent.cs).
///
///     Супертендант склада Новой Маджинции: отдаёт игроку вещи, товары и питомцев, оставшиеся от прилавка,
///     принимает доплату по долгам маклера и открывает окно «ответной» ставки.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>[Constructable]</c> → <c>[Constructible]</c>; <c>SetWearable</c>/<c>EquipItem</c> →
///             <c>AddItem(new X { Hue = hue })</c>, <c>InitBody</c> здесь не было.
///         </item>
///         <item>
///             <c>Race.RandomHairHue</c> — член экземпляра, поэтому <c>Race.Human.RandomHairHue()</c>.
///         </item>
///         <item>Звание и имя переведены на русский: это то, что видит игрок.</item>
///         <item>
///             <c>GetContextMenuEntries(Mobile, List&lt;ContextMenuEntry&gt;)</c> →
///             <c>AddCustomContextEntries(Mobile, ref PooledRefList&lt;ContextMenuEntry&gt;)</c>, а
///             <c>ContextMenuEntry.OnClick()</c> → <c>OnClick(Mobile from, IEntity target)</c>: пользователь
///             приходит параметром, отдельного поля <c>Owner.From</c> в ModernUO нет.
///         </item>
///         <item>
///             <c>from.Stabled</c> есть только у <see cref="Server.Mobiles.PlayerMobile" /> и является
///             <c>HashSet&lt;Mobile&gt;</c>; пополняется через <c>AddStabled</c>/<c>RemoveStabled</c>.
///         </item>
///         <item><c>String.Format</c> → интерполяция прямо в <c>SayTo</c>.</item>
///         <item>
///             <c>TryPayFunds</c> не вызывает <c>Banker.Withdraw</c> на нулевую сумму: в ModernUO такой вызов
///             возвращает <c>false</c> при пустом банке, и игрок не смог бы забрать вещи, ничего не задолжав.
///         </item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class WarehouseSuperintendent : BaseCreature
{
    [Constructible]
    public WarehouseSuperintendent() : base(AIType.AI_Vendor, FightMode.None, 2, 1)
    {
        Race = Race.Human;
        Blessed = true;
        Title = "смотритель склада";

        Female = Utility.RandomBool();
        Body = Female ? 0x191 : 0x190;
        Name = RussianNames.RandomPerson(Female);

        if (Female)
        {
            AddItem(new Skirt { Hue = Utility.RandomPinkHue() });
        }
        else
        {
            AddItem(new ShortPants { Hue = Utility.RandomBlueHue() });
        }

        AddItem(new Tunic { Hue = Utility.RandomBlueHue() });
        AddItem(new Boots());

        Utility.AssignRandomHair(this, Race.Human.RandomHairHue());
        Utility.AssignRandomFacialHair(this, Race.Human.RandomHairHue());

        Hue = Race.Human.RandomSkinHue();
    }

    public override bool IsInvulnerable => true;

    public void TryTransfer(Mobile from, StorageEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        var fees = entry.Funds;

        if (fees < 0)
        {
            var owed = fees * -1;
            SayTo(from, $"Похоже, вы задолжали {owed:#,0} золота. Сколько вы готовы внести сейчас?");
            from.Prompt = new BackfeePrompt(this, entry);
            return;
        }

        if (!TryPayFunds(from, entry))
        {
            // Some personal possessions that were equipped on the broker still remain in storage...
            BazaarInformationGump.DisplayTo(from, 1150681, 1150678);
            return;
        }

        if (entry.Creatures.Count > 0)
        {
            var creatures = new List<BaseCreature>(entry.Creatures);

            foreach (var creature in creatures)
            {
                if (creature == null || creature.Deleted)
                {
                    entry.RemovePet(creature);
                    continue;
                }

                if (from is not PlayerMobile pm || (pm.Stabled?.Count ?? 0) >= AnimalTrainer.GetMaxStabled(from))
                {
                    BazaarInformationGump.DisplayTo(from, 1150681, 1150678);
                    return;
                }

                creature.Blessed = false;
                creature.ControlOrder = OrderType.Stay;
                creature.Internalize();
                creature.IsStabled = true;
                creature.StabledBy = from;
                creature.Loyalty = BaseCreature.MaxLoyalty;
                creature.SetControlMaster(null);
                creature.SummonMaster = null;

                pm.AddStabled(creature);
                entry.RemovePet(creature);
            }
        }

        if (entry.CommodityTypes.Count > 0)
        {
            var commodities = new Dictionary<Type, int>(entry.CommodityTypes);

            foreach (var (type, amount) in commodities)
            {
                if (!GiveItems(from, type, amount, entry))
                {
                    BazaarInformationGump.DisplayTo(from, 1150681, 1150678);
                    return;
                }
            }
        }

        entry.CommodityTypes.Clear();
        entry.Creatures.Clear();

        // There are no longer any items or funds in storage for your former bazaar stall...
        BazaarInformationGump.DisplayTo(from, 1150681, 1150677);
        MaginciaBazaar.RemoveFromStorage(from);
    }

    private bool GiveItems(Mobile from, Type type, int amount, StorageEntry entry)
    {
        var left = amount;

        while (left > 60000)
        {
            var deed = new CommodityDeed();
            var item = Loot.Construct(type);
            item.Amount = 60000;
            deed.SetCommodity(item);
            left -= 60000;

            if (from.Backpack == null || !from.Backpack.TryDropItem(from, deed, false))
            {
                deed.Delete();
                return false;
            }

            entry.RemoveCommodity(type, 60000);
        }

        var lastDeed = new CommodityDeed();
        var lastItem = Loot.Construct(type);
        lastItem.Amount = left;
        lastDeed.SetCommodity(lastItem);

        if (from.Backpack == null || !from.Backpack.TryDropItem(from, lastDeed, false))
        {
            lastDeed.Delete();
            return false;
        }

        entry.RemoveCommodity(type, left);
        return true;
    }

    private static bool TryPayFunds(Mobile from, StorageEntry entry)
    {
        var amount = entry.Funds;

        if (amount <= 0)
        {
            entry.Funds = 0;
            return true;
        }

        if (!Banker.Withdraw(from, amount))
        {
            return false;
        }

        entry.Funds = 0;
        return true;
    }

    public void TryPayBackfee(Mobile from, string text, StorageEntry entry)
    {
        var amount = Utility.ToInt32(text);
        var owed = entry.Funds * -1;

        if (amount <= 0)
        {
            return;
        }

        var toDeduct = Math.Min(owed, amount);

        if (!Banker.Withdraw(from, toDeduct))
        {
            SayTo(from, "В вашем банке недостаточно золота для этой суммы.");
            return;
        }

        entry.Funds += toDeduct;
        var left = entry.Funds;

        if (left >= 0)
        {
            TryTransfer(from, entry);
        }
        else
        {
            SayTo(from, $"Спасибо! У вас остался долг {left * -1:#,0} золота.");
        }
    }

    public override void AddCustomContextEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        list.Add(new ClaimStorageEntry(from, this));
        list.Add(new ChangeMatchBidEntry(from));

        base.AddCustomContextEntries(from, ref list);
    }

    private class BackfeePrompt : Prompt
    {
        private readonly WarehouseSuperintendent _superintendent;
        private readonly StorageEntry _entry;

        public BackfeePrompt(WarehouseSuperintendent superintendent, StorageEntry entry)
        {
            _superintendent = superintendent;
            _entry = entry;
        }

        public override void OnResponse(Mobile from, string text) => _superintendent.TryPayBackfee(from, text, _entry);
    }

    private class ClaimStorageEntry : ContextMenuEntry
    {
        private readonly WarehouseSuperintendent _superintendent;
        private readonly StorageEntry _entry;

        public ClaimStorageEntry(Mobile from, WarehouseSuperintendent superintendent) : base(1150681, 3)
        {
            _superintendent = superintendent;
            _entry = MaginciaBazaar.GetStorageEntry(from);

            if (_entry == null)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from == null || _entry == null)
            {
                return;
            }

            _superintendent.TryTransfer(from, _entry);
        }
    }

    private class ChangeMatchBidEntry : ContextMenuEntry
    {
        public ChangeMatchBidEntry(Mobile from) : base(1150587, 3)
        {
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from != null)
            {
                MatchBidGump.DisplayTo(from, null);
            }
        }
    }
}
