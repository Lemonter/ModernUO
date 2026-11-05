using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO's Covetous void pool (Creatures/Repond.cs).</summary>
[SerializationGenerator(0, false)]
public partial class LizardmanWitchdoctor : CovetousCreature
{
    public override string CorpseName => "труп ящера";

    [Constructible]
    public LizardmanWitchdoctor() : base(AIType.AI_Mage)
    {
        Name = "ящер-знахарь";
        Body = Utility.RandomList(35, 36);
        BaseSoundID = 417;
    }

    public LizardmanWitchdoctor(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "ящер-знахарь";
        Body = Utility.RandomList(35, 36);
        BaseSoundID = 417;
    }
}

[SerializationGenerator(0, false)]
public partial class OrcFootSoldier : CovetousCreature
{
    public override string CorpseName => "труп орка";

    [Constructible]
    public OrcFootSoldier() : base(AIType.AI_Melee)
    {
        Name = "орк-пехотинец";
        Body = 17;
        BaseSoundID = 0x45A;
    }

    public OrcFootSoldier(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "орк-пехотинец";
        Body = 17;
        BaseSoundID = 0x45A;
    }
}

[SerializationGenerator(0, false)]
public partial class RatmanAssassin : CovetousCreature
{
    public override string CorpseName => "труп крысолюда";

    [Constructible]
    public RatmanAssassin() : base(AIType.AI_Melee)
    {
        Name = "крысолюд-убийца";
        Body = 42;
        BaseSoundID = 437;
    }

    public RatmanAssassin(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "крысолюд-убийца";
        Body = 42;
        BaseSoundID = 437;
    }
}

[SerializationGenerator(0, false)]
public partial class OgreBoneCrusher : CovetousCreature
{
    public override string CorpseName => "труп огра";

    [Constructible]
    public OgreBoneCrusher() : base(AIType.AI_Melee)
    {
        Name = "огр-костолом";
        Body = 1;
        BaseSoundID = 427;
    }

    public OgreBoneCrusher(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "огр-костолом";
        Body = 1;
        BaseSoundID = 427;
    }
}

[SerializationGenerator(0, false)]
public partial class TitanRockHunter : CovetousCreature
{
    public override string CorpseName => "труп титана";

    [Constructible]
    public TitanRockHunter() : base(AIType.AI_Mage)
    {
        Name = "титан-камнемёт";
        Body = 76;
        BaseSoundID = 609;
    }

    public TitanRockHunter(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "титан-камнемёт";
        Body = 76;
        BaseSoundID = 609;
    }
}
