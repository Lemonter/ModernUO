/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/Time Of Legends/AuctionSafe/AuctionSafe.cs).
///
///     Сейф-аукцион: аддон в публичном доме, на котором выставляется предмет. Владелец дома настраивает
///     аукцион, остальные (по уровню доступа сейфа) делают ставки. Сам аукцион живёт не в предмете, а в
///     <see cref="AuctionStore" />, поэтому предмет сериализует только уровень доступа, а связку восстанавливает
///     <see cref="AuctionSafePersistence" />.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>private Auction _Auction</c> + <c>Serialize/Deserialize</c> → <see cref="SerializableField" /> для
///             <see cref="SecureLevel" /> и свойство <see cref="Auction" />, читающее <see cref="AuctionStore" />.
///             Генератор кода сам создаёт <c>Deserialize</c>, поэтому ручных <c>Serialize</c> нет.
///         </item>
///         <item>
///             Сеттер аукциона больше не роняет выставленный предмет в банк владельца: все вызывающие
///             (<see cref="Auction.ClaimPrize" />, <see cref="Auction.HouseCollapse" />) и так возвращают предмет
///             сами, а прежний побочный эффект срабатывал дважды на одном и том же предмете.
///         </item>
///         <item>
///             <c>Delete()</c> → <see cref="OnAfterDelete" />: в ModernUO очистка ссылок и возврат ставок делаются
///             здесь, <c>Delete</c> не переопределяется.
///         </item>
///         <item>
///             <c>GetContextMenuEntries(Mobile, List&lt;ContextMenuEntry&gt;)</c> →
///             <c>GetContextMenuEntries(Mobile, ref PooledRefList&lt;ContextMenuEntry&gt;)</c>,
///             <c>SetSecureLevelEntry.AddTo</c> принимает <c>ref</c>-список.
///         </item>
///         <item>
///             <c>ObjectPropertyList</c> → <see cref="IPropertyList" />.
///         </item>
///         <item>
///             Открытие окон: <c>HasGump(typeof(...))</c> → <c>HasGump&lt;T&gt;()</c>,
///             <c>SendGump(new ...Gump(...))</c> → статический <c>DisplayTo</c> из <c>Gumps.cs</c>; порядок проверок
///             доступа оставлен прежним.
///         </item>
///         <item>
///             <c>AuctionSafeDeed</c> больше не хранит <c>From</c>/<c>SouthFacing</c> в сейве: владелец нужен только
///             в момент установки аддона, а ориентация в ServUO ни на что не влияла (обе стороны — один и тот же
///             компонент), поэтому <c>SouthFacing</c> убран, а <c>From</c> — обычное незаписываемое поле.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.Auction;

[SerializationGenerator(0, false)]
public partial class AuctionSafe : BaseAddon, ISecurable
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public AuctionSafe() : this(null)
    {
    }

    public AuctionSafe(Mobile from)
    {
        AddComponent(new InternalComponent(), 0, 0, 0);

        Level = SecureLevel.Anyone;

        if (from != null)
        {
            AuctionStore.Set(this, new Auction(from, this));
        }
    }

    public override BaseAddonDeed Deed => new AuctionSafeDeed();

    [CommandProperty(AccessLevel.GameMaster, readOnly: true)]
    public Auction Auction
    {
        get => AuctionStore.Get(this);
        set => AuctionStore.Set(this, value);
    }

    public bool CheckAuctionItem(Item item) => Auction?.OnGoing == true && Auction.AuctionItem == item;

    public override void OnComponentUsed(AddonComponent component, Mobile from)
    {
        var house = BaseHouse.FindHouseAt(this);

        if (!from.InRange(component.Location, 3))
        {
            from.SendLocalizedMessage(500332); // I am too far away to do that.
            return;
        }

        if (house == null || from is not PlayerMobile player)
        {
            return;
        }

        var auction = Auction;

        if (house.IsOwner(from))
        {
            if (!player.HasGump<AuctionBidGump>())
            {
                AuctionOwnerGump.DisplayTo(player, this);
            }

            return;
        }

        if (auction == null)
        {
            from.SendLocalizedMessage(1156432); // There is no active auction to complete this action.
            return;
        }

        if (!house.HasSecureAccess(from, Level))
        {
            from.SendLocalizedMessage(1156447); // This auction is private.
            return;
        }

        if (auction.InClaimPeriod && auction.HighestBid?.Mobile == from)
        {
            auction.ClaimPrize(from);
            return;
        }

        if (!player.HasGump<AuctionBidGump>())
        {
            AuctionBidGump.DisplayTo(player, this);
        }
    }

    public override void OnChop(Mobile from)
    {
        if (Auction?.AuctionItemOnDisplay() == true)
        {
            // You can't use a bladed item on an auction safe that has an auction item or is currently active.
            from.SendLocalizedMessage(1156452);
            return;
        }

        base.OnChop(from);
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);
        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public override void OnAfterDelete()
    {
        Auction?.HouseCollapse();

        AuctionStore.Remove(this);

        base.OnAfterDelete();
    }

    [Flippable(0x9C18, 0x9C19)]
    [SerializationGenerator(0, false)]
    public partial class InternalComponent : AddonComponent
    {
        [Constructible]
        public InternalComponent() : base(0x9C18)
        {
        }

        public override bool ForceShowProperties => true;

        public override int LabelNumber => 1156371; // Auction Safe

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);

            list.Add(501643); // locked down
        }

        /// <summary>
        ///     AddonComponent в этом репозитории больше не передаёт контекстное меню аддону, поэтому уровень
        ///     доступа сейфа задаётся через компонент, по которому игрок и щёлкает.
        /// </summary>
        public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, ref list);

            if (Addon is AuctionSafe safe)
            {
                safe.GetContextMenuEntries(from, ref list);
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class AuctionSafeDeed : BaseAddonDeed, IRewardItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    private Mobile _from;

    [Constructible]
    public AuctionSafeDeed()
    {
        LootType = LootType.Blessed;
    }

    public override BaseAddon Addon => new AuctionSafe(_from);

    public override int LabelNumber => 1156371; // Auction Safe

    public override void OnDoubleClick(Mobile from)
    {
        var house = BaseHouse.FindHouseAt(from);

        if (house == null || house.Owner != from && !house.IsCoOwner(from))
        {
            return;
        }

        if (!house.Public)
        {
            from.SendLocalizedMessage(1156437); // Auction Safes can only be placed in public type houses.
            return;
        }

        _from = from;
        base.OnDoubleClick(from);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (IsRewardItem)
        {
            list.Add(1076217); // 1st Year Veteran Reward
        }
    }
}
