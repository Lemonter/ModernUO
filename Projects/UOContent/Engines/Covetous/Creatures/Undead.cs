using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO's Covetous void pool (Creatures/Undead.cs).</summary>
[SerializationGenerator(0, false)]
public partial class AngeredSpirit : CovetousCreature
{
    public override string CorpseName => "труп разгневанного духа";

    [Constructible]
    public AngeredSpirit() : base(AIType.AI_Mage)
    {
        Name = "разгневанный дух";
        Body = 3;
        BaseSoundID = 471;
    }

    public AngeredSpirit(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "разгневанный дух";
        Body = 3;
        BaseSoundID = 471;
    }
}

[SerializationGenerator(0, false)]
public partial class BoneSwordSlinger : CovetousCreature
{
    public override string CorpseName => "труп скелета с костяным мечом";

    [Constructible]
    public BoneSwordSlinger() : base(AIType.AI_Melee)
    {
        Name = "скелет с костяным мечом";
        Body = 147;
        BaseSoundID = 451;
    }

    public BoneSwordSlinger(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "скелет с костяным мечом";
        Body = 147;
        BaseSoundID = 451;
    }
}

[SerializationGenerator(0, false)]
public partial class VileCadaver : CovetousCreature
{
    public override string CorpseName => "мерзкий мертвец";

    [Constructible]
    public VileCadaver() : base(AIType.AI_Melee)
    {
        Name = "мерзкий мертвец";
        Body = 154;
        BaseSoundID = 471;
    }

    public VileCadaver(int level, bool voidSpawn) : base(AIType.AI_Melee, level, voidSpawn)
    {
        Name = "мерзкий мертвец";
        Body = 154;
        BaseSoundID = 471;
    }
}

[SerializationGenerator(0, false)]
public partial class DiseasedLich : CovetousCreature
{
    public override string CorpseName => "труп лича";

    [Constructible]
    public DiseasedLich() : base(AIType.AI_Mage)
    {
        Name = "заражённый лич";
        Body = 24;
        BaseSoundID = 0x3E9;
    }

    public DiseasedLich(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "заражённый лич";
        Body = 24;
        BaseSoundID = 0x3E9;
    }
}

[SerializationGenerator(0, false)]
public partial class CovetousRevenant : CovetousCreature
{
    public override string CorpseName => "труп ревенанта";

    public override bool AlwaysMurderer => true;

    [Constructible]
    public CovetousRevenant() : base(AIType.AI_Mage)
    {
        Name = "коварный ревенант";
        Body = 400;
        Hue = 0x847E;

        AddItem(new Robe { ItemID = 0x2683, Hue = 0x4001, Movable = false });
        AddItem(new Boots { Hue = 0x4001, Movable = false });
    }

    public CovetousRevenant(int level, bool voidSpawn) : base(AIType.AI_Mage, level, voidSpawn)
    {
        Name = "коварный ревенант";
        Body = 400;
    }
}
