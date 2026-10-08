using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Mobiles;

// Ported from JustUO (github.com/JustUO/JustUO, a ServUO fork) — see Regions/UnderworldRegion.cs
// header for why ServUO itself wasn't the source here.

[SerializationGenerator(0, false)]
public partial class EnragedEarthElemental : BaseCreature
{
    [Constructible]
    public EnragedEarthElemental() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "разъярённый элементаль земли";
        Body = 14;
        BaseSoundID = 268;

        SetStr(147, 155);
        SetDex(78, 89);
        SetInt(94, 110);

        SetHits(500, 505);
        SetMana(94, 110);
        SetStam(78, 89);

        SetDamage(9, 16);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 59, 65);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 21, 28);
        SetResistance(ResistanceType.Poison, 47, 51);
        SetResistance(ResistanceType.Energy, 30, 33);

        // Mahaon: was AI_Melee. Every elemental and golem casts now; the spell
        // circle is held to 5 by Systems.MahaonCombat.ElementalMagerySystem, so
        // the Magery here is set for reliable casting, not to limit the circle.
        SetSkill(SkillName.Magery, 70.1, 90.0);
        SetSkill(SkillName.EvalInt, 60.1, 80.0);
        SetSkill(SkillName.MagicResist, 100.0);
        SetSkill(SkillName.Tactics, 100.0);
        SetSkill(SkillName.Wrestling, 120.0);

        Fame = 3500;
        Karma = -3500;

        VirtualArmor = 34;
        ControlSlots = 2;

        PackItem(new FertileDirt(Utility.RandomMinMax(1, 4)));
        PackItem(new MandrakeRoot());

        PackItem(new MahaonOre(MahaonMetal.Iron, 5));
    }

    public override string CorpseName => "труп элементаля земли";

    public override double DispelDifficulty => 117.5;
    public override double DispelFocus => 45.0;
    public override bool BleedImmune => true;
    public override int TreasureMapLevel => 1;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Meager);
        AddLoot(LootPack.Gems);
    }
}
