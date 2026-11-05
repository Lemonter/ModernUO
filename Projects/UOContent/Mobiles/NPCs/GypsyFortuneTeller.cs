using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class GypsyFortuneTeller : BaseHealer
{
    [Constructible]
    public GypsyFortuneTeller()
    {
        Title = "гадалка";

        SetSkill(SkillName.Begging, 64.0, 100.0);
    }

    public override bool IsInvulnerable => true;

    public override void InitOutfit()
    {
        base.InitOutfit();

        switch (Utility.Random(4))
        {
            case 0:
                AddItem(new JesterHat(Utility.RandomBrightHue()));
                break;
            case 1:
                AddItem(new Bandana(Utility.RandomBrightHue()));
                break;
            case 2:
                AddItem(new SkullCap(Utility.RandomBrightHue()));
                break;
        }

        Item item = FindItemOnLayer(Layer.Pants);

        if (item != null)
        {
            item.Hue = Utility.RandomBrightHue();
        }

        item = FindItemOnLayer(Layer.Shoes);

        if (item != null)
        {
            item.Hue = Utility.RandomBrightHue();
        }

        item = FindItemOnLayer(Layer.OuterLegs);

        if (item != null)
        {
            item.Hue = Utility.RandomBrightHue();
        }

        item = FindItemOnLayer(Layer.InnerLegs);

        if (item != null)
        {
            item.Hue = Utility.RandomBrightHue();
        }

        item = FindItemOnLayer(Layer.OuterTorso);

        if (item != null)
        {
            item.Hue = Utility.RandomBrightHue();
        }

        item = FindItemOnLayer(Layer.InnerTorso);

        if (item != null)
        {
            item.Hue = Utility.RandomBrightHue();
        }

        item = FindItemOnLayer(Layer.Shirt);

        if (item != null)
        {
            item.Hue = Utility.RandomBrightHue();
        }
    }
}
