using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Covetous void pool (Creatures/Abyss.cs). The level/voidSpawn constructor
///     pair is kept: the pool's spawner builds these with a wave level, the parameterless one is for
///     `[add` and hand placement. ServUO's <c>SetSpecialAbility(SpecialAbility.AngryFire)</c> on the
///     lesser oni has no counterpart in this codebase's monster-ability framework and stays dropped.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DaemonMongbat : CovetousCreature
{
    public override string CorpseName => "труп монгбата";

    [Constructible]
    public DaemonMongbat() : base(AIType.AI_NecroMage)
    {
        Name = "демонический монгбат";
        Body = 39;
        BaseSoundID = 422;
    }

    public DaemonMongbat(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "демонический монгбат";
        Body = 39;
        BaseSoundID = 422;
    }
}

[SerializationGenerator(0, false)]
public partial class GargoyleAssassin : CovetousCreature
{
    public override string CorpseName => "труп горгульи";

    [Constructible]
    public GargoyleAssassin() : base(AIType.AI_Mage)
    {
        Name = "горгулья-убийца";
        Body = 0x4;
        BaseSoundID = 0x174;
    }

    public GargoyleAssassin(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "горгулья-убийца";
        Body = 0x4;
        BaseSoundID = 0x174;
    }
}

[SerializationGenerator(0, false)]
public partial class CovetousDoppleganger : CovetousCreature
{
    public override string CorpseName => "труп доппельгангера";

    [Constructible]
    public CovetousDoppleganger() : base(AIType.AI_Melee)
    {
        Name = "доппельгангер";
        Body = 0x309;
        BaseSoundID = 0x451;
    }

    public CovetousDoppleganger(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "доппельгангер";
        Body = 0x309;
        BaseSoundID = 0x451;
    }
}

[SerializationGenerator(0, false)]
public partial class LesserOni : CovetousCreature
{
    public override string CorpseName => "труп они";

    [Constructible]
    public LesserOni() : base(AIType.AI_Mage)
    {
        Name = "младший они";
        Body = 241;
    }

    public LesserOni(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "младший они";
        Body = 241;
    }

    public override int GetAngerSound() => 0x4E3;
    public override int GetIdleSound() => 0x4E2;
    public override int GetAttackSound() => 0x4E1;
    public override int GetHurtSound() => 0x4E4;
    public override int GetDeathSound() => 0x4E0;
}

[SerializationGenerator(0, false)]
public partial class CovetousFireDaemon : CovetousCreature
{
    public override string CorpseName => "труп огненного демона";

    [Constructible]
    public CovetousFireDaemon() : base(AIType.AI_Mage)
    {
        Name = "огненный демон";
        Body = 9;
        BaseSoundID = 357;
    }

    public CovetousFireDaemon(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "огненный демон";
        Body = 9;
        BaseSoundID = 357;
    }
}
