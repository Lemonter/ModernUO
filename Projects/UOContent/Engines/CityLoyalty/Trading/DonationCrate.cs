/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Trading/DonationCrate.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             <c>MaritimeCargo</c>, <c>BaseWoodBoard</c> and <c>BaseCrabAndLobster</c> do not exist here, so the item
///             donation table uses the equivalents that do: <see cref="Board" />, <see cref="BaseIngot" />,
///             <see cref="BaseHides" />, <see cref="BaseLeather" />, <see cref="RawRibs" />,
///             <see cref="BreadLoaf" />, <see cref="RawFishSteak" />, <see cref="BasePotion" />,
///             <see cref="Bow" /> and <see cref="Crossbow" />. Cargo no longer tops up the treasury because it has
///             no counterpart here.
///         </item>
///         <item><c>ObjectPropertyList</c> usage dropped (the donation crate never had properties of its own).</item>
///         <item><c>Configure()</c> static tables replaced by static readonly initialisers.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public abstract partial class CityDonationItem : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TradeMinister _minister;

    protected CityDonationItem(City city, TradeMinister minister, int itemId) : base(itemId)
    {
        _city = city;
        _minister = minister;
        Movable = false;
    }

    public CityLoyaltySystem CitySystem => CityLoyaltySystem.GetCityInstance(_city);

    public Dictionary<Type, int> Table { get; protected set; }

    public virtual bool Animals => false;

    public override void OnDoubleClick(Mobile from)
    {
        if (!CityLoyaltySystem.IsSetup())
        {
            return;
        }

        if (Animals && Table is { Count: > 0 })
        {
            from.Target = new InternalTarget(this);
            // Which animal do you wish to donate?
            from.SendLocalizedMessage(1152936);
            return;
        }

        if (!Animals)
        {
            // If you wish to donate to the City simply drop the items into the crate.
            SendMessageTo(from, 1152928);
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (!CityLoyaltySystem.IsSetup() || Animals || Table is not { Count: > 0 })
        {
            return false;
        }

        var love = 0;

        if (dropped is Container container)
        {
            foreach (var item in new List<Item>(container.Items))
            {
                love += CheckTurnin(item);
            }
        }
        else
        {
            love = CheckTurnin(dropped);
        }

        if (love > 0)
        {
            CitySystem?.AwardLove(from, love);
            // The City thanks you for your generosity!
            SendMessageTo(from, 1152926);
            return dropped is not Container;
        }

        // Your generosity is appreciated but the City does not require this item.
        SendMessageTo(from, 1152927);
        return false;
    }

    private int CheckTurnin(Item item)
    {
        var dropType = item.GetType();

        foreach (var (checkType, value) in Table)
        {
            if (checkType != dropType && !dropType.IsSubclassOf(checkType))
            {
                continue;
            }

            if (CitySystem == null)
            {
                return 0;
            }

            var amount = item.Amount;
            item.Delete();

            return value * amount;
        }

        return 0;
    }

    private void SendMessageTo(Mobile m, int message)
    {
        if (_minister != null)
        {
            _minister.SayTo(m, message);
        }
        else
        {
            m.SendLocalizedMessage(message);
        }
    }

    private void AfterDeserialization()
    {
        if (CitySystem?.Minister == null)
        {
            return;
        }

        if (this is CityItemDonation item)
        {
            CitySystem.Minister.DonationCrate = item;
        }
        else if (this is CityPetDonation pet)
        {
            CitySystem.Minister.DonationPost = pet;
        }
    }

    private class InternalTarget : Target
    {
        private readonly CityDonationItem _item;

        public InternalTarget(CityDonationItem item) : base(3, false, TargetFlags.None) => _item = item;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCreature bc)
            {
                // Erm. Uhh. I don't think that'd enjoy the stables much...
                _item.SendMessageTo(from, 1152930);
                return;
            }

            if (!bc.Controlled || bc.Summoned || bc.GetMaster() != from)
            {
                _item.SendMessageTo(from, 1152930);
                return;
            }

            var type = bc.GetType();

            if (_item.Table == null || !_item.Table.TryGetValue(type, out var love))
            {
                // That does not look like an animal the City is in need of.
                _item.SendMessageTo(from, 1152929);
                return;
            }

            var sys = _item.CitySystem;

            if (sys == null)
            {
                return;
            }

            bc.Delete();
            sys.AwardLove(from, love);

            // The City thanks you for your generosity!
            _item.SendMessageTo(from, 1152926);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CityItemDonation : CityDonationItem
{
    public static readonly Dictionary<Type, int> ItemTable = new()
    {
        [typeof(Board)] = 10,
        [typeof(BaseIngot)] = 10,
        [typeof(BaseHides)] = 10,
        [typeof(BaseLeather)] = 10,
        [typeof(RawRibs)] = 10,
        [typeof(BreadLoaf)] = 10,
        [typeof(RawFishSteak)] = 10,
        [typeof(BasePotion)] = 15,
        [typeof(Bow)] = 20,
        [typeof(Crossbow)] = 20
    };

    [Constructible]
    public CityItemDonation() : this(City.Britain, null)
    {
    }

    [Constructible]
    public CityItemDonation(City city, TradeMinister minister) : base(city, minister, 0xE3C)
    {
        Table = ItemTable;

        if (CitySystem?.Minister != null)
        {
            CitySystem.Minister.DonationCrate = this;
        }
    }
}

[SerializationGenerator(0, false)]
public partial class CityPetDonation : CityDonationItem
{
    public static readonly Dictionary<Type, int> PetTable = new()
    {
        [typeof(Dog)] = 10,
        [typeof(Cat)] = 10,
        [typeof(Cow)] = 10,
        [typeof(Goat)] = 10,
        [typeof(Horse)] = 10,
        [typeof(Sheep)] = 10,
        [typeof(Pig)] = 10,
        [typeof(Chicken)] = 10
    };

    [Constructible]
    public CityPetDonation() : this(City.Britain, null)
    {
    }

    [Constructible]
    public CityPetDonation(City city, TradeMinister minister) : base(city, minister, 0x14E7)
    {
        Table = PetTable;

        if (CitySystem?.Minister != null)
        {
            CitySystem.Minister.DonationPost = this;
        }
    }

    public override bool Animals => true;
}
