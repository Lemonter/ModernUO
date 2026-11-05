using ModernUO.Serialization;
using System;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/CollectionsBritLibraryQuotes.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SherryTheMouseQuotes : BaseStatuette
{
    [Constructible]
    public SherryTheMouseQuotes()
        : base(0xFBD)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 5.0;
    }

    public override int LabelNumber => 1073300; // Library Friends - Quotes from the pen of Sherry the Mouse

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            this.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.RandomMinMax(1073301, 1073309));

            if (Utility.RandomBool())
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x30A, 0x313));
            }
            else
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x419, 0x422));
            }
        }

        base.OnMovement(m, oldLocation);
    }
}

[SerializationGenerator(0, false)]
public partial class WyrdBeastmasterQuotes : BaseStatuette
{
    [Constructible]
    public WyrdBeastmasterQuotes()
        : base(0xFBD)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 5.0;
    }

    public override int LabelNumber => 1073310; // Library Friends - Quotes from the pen of Wyrd Beastmaster

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            this.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.RandomMinMax(1073311, 1073316));

            if (Utility.RandomBool())
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x30A, 0x313));
            }
            else
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x419, 0x422));
            }
        }

        base.OnMovement(m, oldLocation);
    }
}

[SerializationGenerator(0, false)]
public partial class MercenaryJustinQuotes : BaseStatuette
{
    [Constructible]
    public MercenaryJustinQuotes()
        : base(0xFBD)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 5.0;
    }

    public override int LabelNumber => 1073317; // Library Friends - Quotes from the pen of Mercenary Justin

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            this.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.RandomMinMax(1073318, 1073325));

            if (Utility.RandomBool())
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x30A, 0x313));
            }
            else
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x419, 0x422));
            }
        }

        base.OnMovement(m, oldLocation);
    }
}

[SerializationGenerator(0, false)]
public partial class HeigelOfMoonglowQuotes : BaseStatuette
{
    [Constructible]
    public HeigelOfMoonglowQuotes()
        : base(0xFBD)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 5.0;
    }

    public override int LabelNumber => 1073327; // Library Friends - Quotes from the pen of Heigel of Moonglow

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            this.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.RandomList(1073326, 1073328, 1073329, 1073330, 1073331));

            if (Utility.RandomBool())
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x30A, 0x313));
            }
            else
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x419, 0x422));
            }
        }

        base.OnMovement(m, oldLocation);
    }
}

[SerializationGenerator(0, false)]
public partial class TraderHoraceQuotes : BaseStatuette
{
    [Constructible]
    public TraderHoraceQuotes()
        : base(0xFBD)
    {
        this.LootType = LootType.Blessed;
        this.Weight = 5.0;
    }

    public override int LabelNumber => 1073338; // Library Friends - Quotes from the pen of Horace, Trader

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (this.TurnedOn && this.IsLockedDown && (!m.Hidden || m.Player) && Utility.InRange(m.Location, this.Location, 2) && !Utility.InRange(oldLocation, this.Location, 2))
        {
            this.PublicOverheadMessage(MessageType.Regular, 0x3B2, Utility.RandomMinMax(1073332, 1073337));

            if (Utility.RandomBool())
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x30A, 0x313));
            }
            else
            {
                Effects.PlaySound(this.Location, this.Map, Utility.RandomMinMax(0x419, 0x422));
            }
        }

        base.OnMovement(m, oldLocation);
    }
}
