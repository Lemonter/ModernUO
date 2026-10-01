using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

// Ported from JustUO (github.com/JustUO/JustUO, a ServUO fork) — see Regions/UnderworldRegion.cs
// header for why ServUO itself wasn't the source here.

[SerializationGenerator(0, false)]
public partial class Gremlin : BaseCreature
{
    [Constructible]
    public Gremlin() : base(AIType.AI_Archer, FightMode.Closest)
    {
        Name = "гремлин";
        Body = 724;

        SetStr(106);
        SetDex(130);
        SetInt(36);

        SetHits(70);

        SetDamage(5, 7);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 26);
        SetResistance(ResistanceType.Fire, 36);
        SetResistance(ResistanceType.Cold, 22);
        SetResistance(ResistanceType.Poison, 17);
        SetResistance(ResistanceType.Energy, 30);

        SetSkill(SkillName.Anatomy, 78.5);
        SetSkill(SkillName.MagicResist, 82.5);
        SetSkill(SkillName.Tactics, 65.3);

        AddItem(new Bow());
        PackItem(new Arrow(Utility.RandomMinMax(60, 80)));
        PackItem(new Apple(5));
    }

    public override string CorpseName => "труп гремлина";

    public override void GenerateLoot() => AddLoot(LootPack.Rich);
}
