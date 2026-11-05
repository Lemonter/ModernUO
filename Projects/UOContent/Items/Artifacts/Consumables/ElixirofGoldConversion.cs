using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Consumables/ElixirofGoldConversion.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ElixirofGoldConversion : Item
{
    public override int LabelNumber => 1113007; // Elixir of Gold Conversion

    [Constructible]
    public ElixirofGoldConversion()
        : base(0x99B)
    {
        Hue = 2213;
        Movable = true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        Container backpack = from.Backpack;
        DullCopperIngot item1 = (DullCopperIngot)backpack.FindItemByType(typeof(DullCopperIngot));

        if (item1 != null)
        {
            BaseIngot _ore1 = item1 as BaseIngot;

            int toConsume = _ore1.Amount;

            if ((_ore1.Amount > 499) && (_ore1.Amount < 501))
            {
                _ore1.Delete();
                from.SendLocalizedMessage(1113048); // You've successfully converted the metal.
                from.AddToBackpack(new GoldIngot(500));
                this.Delete();
            }
            else if ((_ore1.Amount < 500) || (_ore1.Amount > 500))
            {
                from.SendLocalizedMessage(1113046); // You can only convert five hundred ingots at a time.
            }
        }
        else
        {
            from.SendLocalizedMessage(1078618); // The item must be in your backpack to be exchanged.
        }
    }
}
