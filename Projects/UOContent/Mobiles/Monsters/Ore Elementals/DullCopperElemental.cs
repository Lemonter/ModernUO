using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class DullCopperElemental : BaseCreature
    {
        [Constructible]
        public DullCopperElemental(int oreAmount = 2) : base(AIType.AI_Mage)
        {
            Body = 110;
            BaseSoundID = 268;

            SetStr(226, 255);
            SetDex(126, 145);
            SetInt(71, 92);

            SetHits(136, 153);

            SetDamage(9, 16);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 20, 30);

            // Mahaon: was AI_Melee. Every elemental and golem casts now; the spell
            // circle is held to 5 by Systems.MahaonCombat.ElementalMagerySystem, so
            // the Magery here is set for reliable casting, not to limit the circle.
            SetSkill(SkillName.Magery, 70.1, 90.0);
            SetSkill(SkillName.EvalInt, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 50.1, 95.0);
            SetSkill(SkillName.Tactics, 60.1, 100.0);
            SetSkill(SkillName.Wrestling, 60.1, 100.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 20;

            PackItem(new MahaonOre(MahaonMetal.Cobalt, oreAmount));
        }

        public override string CorpseName => "труп рудного элементаля";
        public override string DefaultName => "тусклый медный элементаль";

        public override bool AutoDispel => true;
        public override bool BleedImmune => true;
        public override int TreasureMapLevel => 1;

        private static MonsterAbility[] _abilities = { MonsterAbilities.DeathExplosion };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems, 2);
        }
    }
}
