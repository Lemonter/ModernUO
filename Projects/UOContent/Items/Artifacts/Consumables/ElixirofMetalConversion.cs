using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Consumables/ElixirofMetalConversion.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class ElixirofMetalConversion : Item
{
    public override int LabelNumber => 1113011; // Elixir of Metal Conversion

    [Constructible]
    public ElixirofMetalConversion()
        : base(0x99B)
    {
        Hue = 1159;
        Movable = true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        Container backpack = from.Backpack;
        IronIngot item1 = (IronIngot)backpack.FindItemByType(typeof(IronIngot));

        if (item1 != null)
        {
            BaseIngot _ore1 = item1 as BaseIngot;

            int toConsume = _ore1.Amount;

            if ((_ore1.Amount > 499) && (_ore1.Amount < 501))
            {
                _ore1.Delete();

                switch ( Utility.Random(4) )
                {
                    case 0:
                        from.AddToBackpack(new DullCopperIngot(500));
                        break;
                    case 2:
                        from.AddToBackpack(new ShadowIronIngot(500));
                        break;
                    case 1:
                        from.AddToBackpack(new CopperIngot(500));
                        break;
                    case 3:
                        from.AddToBackpack(new BronzeIngot(500));
                        break;
                }

                from.SendLocalizedMessage(1113048); // You've successfully converted the metal.
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
