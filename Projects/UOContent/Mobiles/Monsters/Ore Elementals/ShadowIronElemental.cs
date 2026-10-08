using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class ShadowIronElemental : BaseCreature
    {
        [Constructible]
        public ShadowIronElemental(int oreAmount = 2) : base(AIType.AI_Mage)
        {
            Body = 111;
            BaseSoundID = 268;

            SetStr(226, 255);
            SetDex(126, 145);
            SetInt(71, 92);

            SetHits(136, 153);

            SetDamage(9, 16);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 20, 30);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 30, 40);

            // Mahaon: was AI_Melee. Every elemental and golem casts now; the spell
            // circle is held to 5 by Systems.MahaonCombat.ElementalMagerySystem, so
            // the Magery here is set for reliable casting, not to limit the circle.
            SetSkill(SkillName.Magery, 70.1, 90.0);
            SetSkill(SkillName.EvalInt, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 50.1, 95.0);
            SetSkill(SkillName.Tactics, 60.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 100.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 23;

            PackItem(new MahaonOre(MahaonMetal.Shadow, oreAmount));
        }

        public override string CorpseName => "труп рудного элементаля";
        public override string DefaultName => "элементаль теневого железа";

        public override bool AutoDispel => true;
        public override bool BleedImmune => true;
        public override int TreasureMapLevel => 1;
        public override Poison PoisonImmune => Poison.Deadly;
        public override bool BreathImmune => true;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems, 2);
        }

        public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            if (from is BaseCreature bc && (bc.Controlled || bc.BardTarget == this))
            {
                damage = 0; // Immune to pets and provoked creatures
            }
        }

        public override void AlterDamageScalarFrom(Mobile caster, ref double scalar)
        {
            scalar = 0.0; // Immune to magic
        }

        public override void AlterSpellDamageFrom(Mobile from, ref int damage)
        {
            damage = 0;
        }
    }
}
