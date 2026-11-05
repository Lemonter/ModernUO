using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO's Covetous void pool (Creatures/Elementals.cs).</summary>
[SerializationGenerator(0, false)]
public partial class CovetousEarthElemental : CovetousCreature
{
    public override string CorpseName => "труп земляного элементаля";

    [Constructible]
    public CovetousEarthElemental() : base(AIType.AI_Melee)
    {
        Name = "земляной элементаль";
        Body = 14;
        BaseSoundID = 268;
    }

    public CovetousEarthElemental(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "земляной элементаль";
        Body = 14;
        BaseSoundID = 268;
    }
}

[SerializationGenerator(0, false)]
public partial class CovetousWaterElemental : CovetousCreature
{
    public override string CorpseName => "труп водяного элементаля";

    [Constructible]
    public CovetousWaterElemental() : base(AIType.AI_Mage)
    {
        Name = "водяной элементаль";
        Body = 16;
        BaseSoundID = 278;
    }

    public CovetousWaterElemental(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "водяной элементаль";
        Body = 16;
        BaseSoundID = 278;
    }
}

[SerializationGenerator(0, false)]
public partial class VortexElemental : CovetousCreature
{
    public override string CorpseName => "труп вихревого элементаля";

    [Constructible]
    public VortexElemental() : base(AIType.AI_Melee)
    {
        Name = "вихревой элементаль";
        Body = 13;
        Hue = 0x4001;
        BaseSoundID = 655;
    }

    public VortexElemental(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "вихревой элементаль";
        Body = 13;
        Hue = 0x4001;
        BaseSoundID = 655;
    }
}

[SerializationGenerator(0, false)]
public partial class SearingElemental : CovetousCreature
{
    public override string CorpseName => "труп палящего элементаля";

    [Constructible]
    public SearingElemental() : base(AIType.AI_Mage)
    {
        Name = "палящий элементаль";
        Body = 15;
        BaseSoundID = 838;
    }

    public SearingElemental(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "палящий элементаль";
        Body = 15;
        BaseSoundID = 838;
    }
}

[SerializationGenerator(0, false)]
public partial class VenomElemental : CovetousCreature
{
    public override string CorpseName => "труп ядовитого элементаля";

    public override bool BleedImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override Poison HitPoison => Poison.Lethal;
    public override double HitPoisonChance => 0.75;
    public override int TreasureMapLevel => 5;

    [Constructible]
    public VenomElemental() : base(AIType.AI_Mage)
    {
        Name = "ядовитый элементаль";
        Body = 162;
        BaseSoundID = 263;
    }

    public VenomElemental(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "ядовитый элементаль";
        Body = 162;
        BaseSoundID = 263;
    }
}
