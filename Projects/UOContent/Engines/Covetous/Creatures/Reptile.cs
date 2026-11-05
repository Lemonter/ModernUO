using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO's Covetous void pool (Creatures/Reptile.cs).</summary>
[SerializationGenerator(0, false)]
public partial class WarAlligator : CovetousCreature
{
    public override string CorpseName => "труп аллигатора";

    [Constructible]
    public WarAlligator() : base(AIType.AI_Melee)
    {
        Name = "боевой аллигатор";
        Body = 0xCA;
        BaseSoundID = 660;
    }

    public WarAlligator(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "боевой аллигатор";
        Body = 0xCA;
        BaseSoundID = 660;
    }
}

[SerializationGenerator(0, false)]
public partial class MagmaLizard : CovetousCreature
{
    public override string CorpseName => "труп магмовой ящерицы";

    [Constructible]
    public MagmaLizard() : base(AIType.AI_Melee)
    {
        Name = "магмовая ящерица";
        Body = 0xCE;
        Hue = Utility.RandomList(0x647, 0x650, 0x659, 0x662, 0x66B, 0x674);
        BaseSoundID = 0x5A;
    }

    public MagmaLizard(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "магмовая ящерица";
        Body = 0xCE;
        Hue = Utility.RandomList(0x647, 0x650, 0x659, 0x662, 0x66B, 0x674);
        BaseSoundID = 0x5A;
    }
}

[SerializationGenerator(0, false)]
public partial class ViciousDrake : CovetousCreature
{
    public override string CorpseName => "труп дрейка";

    [Constructible]
    public ViciousDrake() : base(AIType.AI_Melee)
    {
        Name = "свирепый дрейк";
        Body = Utility.RandomList(60, 61);
        BaseSoundID = 362;
    }

    public ViciousDrake(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "свирепый дрейк";
        Body = Utility.RandomList(60, 61);
        BaseSoundID = 362;
    }
}

[SerializationGenerator(0, false)]
public partial class CorruptedWyvern : CovetousCreature
{
    public override string CorpseName => "труп виверны";

    [Constructible]
    public CorruptedWyvern() : base(AIType.AI_Mage)
    {
        Name = "искажённая виверна";
        Body = 62;
        BaseSoundID = 362;
    }

    public CorruptedWyvern(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "искажённая виверна";
        Body = 62;
        BaseSoundID = 362;
    }
}

[SerializationGenerator(0, false)]
public partial class CovetousWyrm : CovetousCreature
{
    public override string CorpseName => "труп коварного змея";

    [Constructible]
    public CovetousWyrm() : base(AIType.AI_NecroMage)
    {
        Name = "коварный змей";
        Body = 106;
        BaseSoundID = 362;
    }

    public CovetousWyrm(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "коварный змей";
        Body = 106;
        BaseSoundID = 362;
    }
}
