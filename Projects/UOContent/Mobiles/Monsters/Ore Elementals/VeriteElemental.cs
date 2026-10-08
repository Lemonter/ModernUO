using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class VeriteElemental : BaseCreature
    {
        [Constructible]
        public VeriteElemental(int oreAmount = 2) : base(AIType.AI_Mage)
        {
            Body = 113;
            BaseSoundID = 268;

            SetStr(226, 255);
            SetDex(126, 145);
            SetInt(71, 92);

            SetHits(136, 153);

            SetDamage(9, 16);

            SetDamageType(ResistanceType.Physical, 50);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 50, 60);
            SetResistance(ResistanceType.Energy, 50, 60);

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

            VirtualArmor = 35;

            PackItem(new MahaonOre(MahaonMetal.Verite, oreAmount));
        }

        public override string CorpseName => "труп рудного элементаля";
        public override string DefaultName => "веритовый элементаль";

        public override bool AutoDispel => true;
        public override bool BleedImmune => true;
        public override int TreasureMapLevel => 1;

        private static MonsterAbility[] _abilities = { MonsterAbilities.DestroyEquipment };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.Gems, 2);
        }
    }
}
