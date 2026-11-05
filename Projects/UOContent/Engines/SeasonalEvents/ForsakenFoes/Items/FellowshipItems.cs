using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/AdmiralJacksPumpkinSpiceAle.cs).</summary>
[SerializationGenerator(0, false)]
public partial class AdmiralJacksPumpkinSpiceAle : BaseShield
{
    public override int LabelNumber => 1159230; // Admiral Jack's Pumpkin Spice Ale

    [Constructible]
    public AdmiralJacksPumpkinSpiceAle() : base(0xA40B)
    {
        Hue = 1922;
        Weight = 3.0;
        Attributes.SpellChanneling = 1;
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/EtherealSoulcleanser.cs).
///     The reward credit and burst are kept; ServUO's raw <c>GraphicalEffect</c> packet is replaced with
///     <see cref="Mobile.FixedParticles" />. Points live in <see cref="FellowshipData" /> instead of a points system.
/// </summary>
[SerializationGenerator(0, false)]
public partial class EtherealSoulcleanser : BaseContainer
{
    public override int LabelNumber => 1159196; // Ethereal Soulcleanser

    public override int DefaultGumpID => 0x10C;

    public override bool DisplaysContent => false;

    public static EtherealSoulcleanser InstanceTram { get; set; }

    public static EtherealSoulcleanser InstanceFel { get; set; }

    [Constructible]
    public EtherealSoulcleanser() : base(0x2DF4)
    {
        Hue = 2591;
        Movable = false;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Map == Map.Trammel)
        {
            InstanceTram = this;
        }

        if (Map == Map.Felucca)
        {
            InstanceFel = this;
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped) => Check(from, dropped);

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p) => Check(from, item);

    public bool Check(Mobile from, Item item)
    {
        if (from == null || from.Deleted || item == null)
        {
            return false;
        }

        if (item is not EtherealSoulbinder binder || binder.SoulPoint <= 0)
        {
            from.SendLocalizedMessage(1159170); // The machine only accepts filled ethereal soulbinders.
            return false;
        }

        FellowshipData.AwardPoints(from, 100 * binder.SoulPoint);

        from.FixedParticles(0x373A, 10, 15, 0, EffectLayer.Waist);
        from.PlaySound(0x1F2);

        item.Delete();

        return true;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/FellowshipMedallion.cs).</summary>
[SerializationGenerator(0, false)]
public partial class FellowshipMedallion : Item
{
    public override int LabelNumber => 1159248; // Fellowship Medallion

    [Constructible]
    public FellowshipMedallion() : base(0xA429)
    {
        Weight = 1.0;
        Layer = Layer.Neck;
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.SendGump(new FellowshipMedallionGump());
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1157722, "its origin");
        from.PlaySound(1050);
    }

    private class FellowshipMedallionGump : Gumps.Gump
    {
        public FellowshipMedallionGump() : base(100, 100)
        {
            AddPage(0);

            AddBackground(0, 0, 454, 400, 0x24A4);
            AddItem(75, 120, 0xA429);
            AddHtmlLocalized(177, 50, 250, 18, 1114513, "#1159248", 0x3442, false, false);
            AddHtmlLocalized(177, 77, 250, 36, 1114513, "#1159033", 0x3442, false, false);
            AddHtmlLocalized(177, 122, 250, 228, 1159247, 0xC63, true, true);
        }
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/FellowshipDonationBox.cs).
///     The donation ledger is persisted through <see cref="FellowshipData" />'s <see cref="GenericPersistence" />
///     instead of ServUO's <c>Persistence.Serialize</c> file.
/// </summary>
[SerializationGenerator(0, false)]
public partial class FellowshipDonationBox : BaseContainer
{
    public override int LabelNumber => 1159042; // Fellowship Donation Box

    public override int DefaultGumpID => 0x10C;

    public static FellowshipDonationBox InstanceTram { get; set; }

    public static FellowshipDonationBox InstanceFel { get; set; }

    [Constructible]
    public FellowshipDonationBox() : base(0x2DE9)
    {
        Hue = 1191;
        Movable = false;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Map == Map.Trammel)
        {
            InstanceTram = this;
        }

        if (Map == Map.Felucca)
        {
            InstanceFel = this;
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped) => Check(from, dropped);

    public override bool OnDragDropInto(Mobile from, Item item, Point3D p) => Check(from, item);

    public bool Check(Mobile from, Item item)
    {
        if (from == null || from.Deleted)
        {
            return false;
        }

        if (item is not MaritimeCargo cargo)
        {
            from.SendLocalizedMessage(1159030); // The Fellowship only requires trade cargo donations at this time.
            return false;
        }

        FellowshipData.AddDonation(from, cargo.GetAwardAmount() * 1000);

        var donated = FellowshipData.GetDonations(from);

        from.SendLocalizedMessage(1159032, donated.ToString()); // The Fellowship thanks you for your donation. You have donated ~1_val~ worth of goods!

        if (donated >= 450000000)
        {
            from.SendLocalizedMessage(1152339, donated.ToString()); // A reward of ~1_ITEM~ has been placed in your backpack.
            from.AddToBackpack(new FellowshipCoin());

            FellowshipData.SetDonation(from, donated - 450000000);
        }

        item.Delete();

        return true;
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/ForsakenFoes/Items/TheFellowshipStaff.cs).</summary>
[SerializationGenerator(0, false)]
[Flippable(0xA343, 0xA344)]
public partial class TheFellowshipStaff : BaseStaff
{
    public override int LabelNumber => 1159034; // The Fellowship Staff

    public static TheFellowshipStaff InstanceTram { get; set; }

    public static TheFellowshipStaff InstanceFel { get; set; }

    [Constructible]
    public TheFellowshipStaff() : base(0xA343)
    {
        Hue = 2721;
    }

    public override WeaponAbility PrimaryAbility => WeaponAbility.Block;

    public override WeaponAbility SecondaryAbility => WeaponAbility.ForceOfNature;

    public override int AosStrengthReq => 20;

    public override int AosMinDamage => 15;

    public override int AosMaxDamage => 18;

    public override float MlSpeed => 3.25f;

    public override int InitMinHits => 30;

    public override int InitMaxHits => 60;

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (Map == Map.Trammel)
        {
            InstanceTram = this;
        }

        if (Map == Map.Felucca)
        {
            InstanceFel = this;
        }
    }

    public override void GetDamageTypes(
        Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy, out int chaos, out int direct
    )
    {
        fire = cold = pois = nrgy = chaos = direct = 0;
        phys = 100;
    }
}
