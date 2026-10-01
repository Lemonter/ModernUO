using ModernUO.Serialization;

namespace Server.Mobiles;

// Ported from real OSI/ServUO content (Scripts/Quests/Eodon/Valley of One Quest/Creatures.cs).
// SetSpecialAbility(SpecialAbility.DragonBreath) dropped — that creature special-move
// framework doesn't exist in this codebase (see Mobiles/Monsters/Reptile/Eodon/Dinosaurs.cs
// header for the general explanation).
[SerializationGenerator(0, false)]
public partial class VolcanoElemental : BaseCreature
{
    [Constructible]
    public VolcanoElemental() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "вулканический элементаль";
        Body = 15;
        Hue = 2726;

        SetStr(446, 510);
        SetDex(173, 191);
        SetInt(369, 397);

        SetHits(800, 1200);

        SetDamage(18, 24);

        SetDamageType(ResistanceType.Physical, 10);
        SetDamageType(ResistanceType.Fire, 90);

        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Anatomy, 0.0, 12.8);
        SetSkill(SkillName.EvalInt, 84.8, 92.6);
        // Mahaon: was AI_Melee. Every elemental and golem casts now; the spell
        // circle is held to 5 by Systems.MahaonCombat.ElementalMagerySystem, so
        // the Magery here is set for reliable casting, not to limit the circle.
        SetSkill(SkillName.Magery, 90.1, 92.7);
        SetSkill(SkillName.Meditation, 97.8, 102.8);
        SetSkill(SkillName.MagicResist, 101.9, 106.2);
        SetSkill(SkillName.Tactics, 80.3, 94.0);
        SetSkill(SkillName.Wrestling, 71.7, 85.4);

        Fame = 12500;
        Karma = -12500;
    }

    public override string CorpseName => "дымящиеся останки";

    public override int GetIdleSound() => 1549;
    public override int GetAngerSound() => 1546;
    public override int GetHurtSound() => 1548;
    public override int GetDeathSound() => 1547;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
        AddLoot(LootPack.Gems, 2);
        AddLoot(LootPack.MedScrolls);
    }
}
