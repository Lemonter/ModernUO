using ModernUO.Serialization;

namespace Server.Items;

/// <summary>The eleven essences of the gargoyle virtues, ported from ServUO
/// (Scripts/Items/Resource/Essence*.cs). One art, eleven hues, one per virtue of the Book of
/// Circles. Sliem pays one out for "Unusual Goods", inside an Essence Box.
///
/// Unlike the rest of the Stygian Abyss imbuing ingredients these are ported in full: they are
/// a closed, self-contained set that belongs to Ter Mur's own lore, and the quest reward hands
/// one out at random. See ImbuingIngredients.cs for why the other ingredients are not here.</summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseEssence : Item, ICommodity
{
    public BaseEssence(int hue, int amount) : base(0x571C)
    {
        Stackable = true;
        Amount = amount;
        Hue = hue;
    }

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}

[SerializationGenerator(0, false)]
public partial class EssenceAchievement : BaseEssence
{
    [Constructible]
    public EssenceAchievement(int amount = 1) : base(1724, amount)
    {
    }

    public override int LabelNumber => 1113325; // essence of achievement
}

[SerializationGenerator(0, false)]
public partial class EssenceBalance : BaseEssence
{
    [Constructible]
    public EssenceBalance(int amount = 1) : base(1268, amount)
    {
    }

    public override int LabelNumber => 1113324; // essence of balance
}

[SerializationGenerator(0, false)]
public partial class EssenceControl : BaseEssence
{
    [Constructible]
    public EssenceControl(int amount = 1) : base(1165, amount)
    {
    }

    public override int LabelNumber => 1113340; // essence of control
}

[SerializationGenerator(0, false)]
public partial class EssenceDiligence : BaseEssence
{
    [Constructible]
    public EssenceDiligence(int amount = 1) : base(1166, amount)
    {
    }

    public override int LabelNumber => 1113338; // essence of diligence
}

[SerializationGenerator(0, false)]
public partial class EssenceDirection : BaseEssence
{
    [Constructible]
    public EssenceDirection(int amount = 1) : base(1156, amount)
    {
    }

    public override int LabelNumber => 1113328; // essence of direction
}

[SerializationGenerator(0, false)]
public partial class EssenceFeeling : BaseEssence
{
    [Constructible]
    public EssenceFeeling(int amount = 1) : base(455, amount)
    {
    }

    public override int LabelNumber => 1113339; // essence of feeling
}

[SerializationGenerator(0, false)]
public partial class EssenceOrder : BaseEssence
{
    [Constructible]
    public EssenceOrder(int amount = 1) : base(1153, amount)
    {
    }

    public override int LabelNumber => 1113342; // essence of order
}

[SerializationGenerator(0, false)]
public partial class EssencePassion : BaseEssence
{
    [Constructible]
    public EssencePassion(int amount = 1) : base(1161, amount)
    {
    }

    public override int LabelNumber => 1113326; // essence of passion
}

[SerializationGenerator(0, false)]
public partial class EssencePersistence : BaseEssence
{
    [Constructible]
    public EssencePersistence(int amount = 1) : base(37, amount)
    {
    }

    public override int LabelNumber => 1113343; // essence of persistence
}

[SerializationGenerator(0, false)]
public partial class EssencePrecision : BaseEssence
{
    [Constructible]
    public EssencePrecision(int amount = 1) : base(1158, amount)
    {
    }

    public override int LabelNumber => 1113327; // essence of precision
}

[SerializationGenerator(0, false)]
public partial class EssenceSingularity : BaseEssence
{
    [Constructible]
    public EssenceSingularity(int amount = 1) : base(1109, amount)
    {
    }

    public override int LabelNumber => 1113341; // essence of singularity
}

/// <summary>A piece of blackrock that took a crystal form. Ported from ServUO
/// (Scripts/Items/Resource/CrystallineBlackrock.cs); Sliem asks for one.</summary>
[SerializationGenerator(0, false)]
public partial class CrystallineBlackrock : Item, ICommodity
{
    [Constructible]
    public CrystallineBlackrock(int amount = 1) : base(0x5732)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1113344; // crystalline blackrock

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
