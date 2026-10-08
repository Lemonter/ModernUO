using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonMetals;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class AgapiteElemental : BaseCreature
    {
        [Constructible]
        public AgapiteElemental(int oreAmount = 2) : base(AIType.AI_Mage)
        {
            Body = 107;
            BaseSoundID = 268;

            SetStr(226, 255);
            SetDex(126, 145);
            SetInt(71, 92);

            SetHits(136, 153);

            SetDamage(28);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 40, 50);
            SetResistance(ResistanceType.Cold, 40, 50);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 10, 20);

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

            VirtualArmor = 32;

            PackItem(new MahaonOre(MahaonMetal.Agapite, oreAmount));
        }

        public override string CorpseName => "труп рудного элементаля";
        public override string DefaultName => "агапитовый элементаль";

        public override bool BleedImmune => true;
        public override bool AutoDispel => true;
        public override int TreasureMapLevel => 1;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Average);
            AddLoot(LootPack.Gems, 2);
        }
    }
}
