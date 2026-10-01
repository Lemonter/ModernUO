using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/CoralSnake.cs). SetMagicalAbility
/// dropped (legacy-save migration only in the original, irrelevant for a fresh port) — see
/// Rotworm.cs's class doc comment for why this framework doesn't exist here at all.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a snake corpse")]
public partial class CoralSnake : BaseCreature
{
    [Constructible]
    public CoralSnake() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 52;
        Hue = 0x21;
        BaseSoundID = 0xDB;

        SetStr(205, 340);
        SetDex(248, 300);
        SetInt(28, 35);

        SetHits(132, 200);
        SetMana(28, 35);

        SetDamage(5, 21);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Poison, 50);

        SetResistance(ResistanceType.Physical, 42, 50);
        SetResistance(ResistanceType.Fire, 5, 20);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 5, 20);

        SetSkill(SkillName.Poisoning, 99.7, 110.9);
        SetSkill(SkillName.MagicResist, 98.1, 105.0);
        SetSkill(SkillName.Tactics, 82.0, 98.0);
        SetSkill(SkillName.Wrestling, 90.3, 105.0);

        Fame = 300;
        Karma = -300;

        Tamable = false;
        ControlSlots = 1;
        MinTameSkill = 59.1;
    }

    public override string DefaultName => "коралловая змея";

    public override Poison PoisonImmune => Poison.Lesser;
    public override Poison HitPoison => Poison.Deadly;

    public override int Meat => 1;
    public override FoodType FavoriteFood => FoodType.Eggs;
}
