using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonWorld;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Lich : BaseCreature
    {
        [Constructible]
        public Lich() : base(AIType.AI_Mage)
        {
            Body = 24;
            BaseSoundID = 0x3E9;

            SetStr(171, 200);
            SetDex(126, 145);
            SetInt(276, 305);

            SetHits(103, 120);

            SetDamage(24, 26);

            SetDamageType(ResistanceType.Physical, 10);
            SetDamageType(ResistanceType.Cold, 40);
            SetDamageType(ResistanceType.Energy, 50);

            SetResistance(ResistanceType.Physical, 40, 60);
            SetResistance(ResistanceType.Fire, 20, 30);
            SetResistance(ResistanceType.Cold, 50, 60);
            SetResistance(ResistanceType.Poison, 55, 65);
            SetResistance(ResistanceType.Energy, 40, 50);

            SetSkill(SkillName.Necromancy, 89, 99.1);
            SetSkill(SkillName.SpiritSpeak, 90.0, 99.0);

            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Magery, 70.1, 80.0);
            SetSkill(SkillName.Meditation, 85.1, 95.0);
            SetSkill(SkillName.MagicResist, 80.1, 100.0);
            SetSkill(SkillName.Tactics, 70.1, 90.0);

            Fame = 8000;
            Karma = -8000;

            VirtualArmor = 50;
            PackItem(new GnarledStaff());
            PackNecroReg(17, 24);
        }

        public override string CorpseName => "труп лича";
        public override string DefaultName => "лич";
        public override bool IsUndead => true;

        public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

        public override bool CanRummageCorpses => true;
        public override bool BleedImmune => true;
        public override Poison PoisonImmune => Poison.Lethal;
        public override int TreasureMapLevel => 3;

        // Base MageAI already handles some necromancy (PainSpikeSpell/StrangleSpell) for
        // anything with Necromancy > 50, but never actually summons undead. This gives
        // liches that on top, without touching the rest of their normal mage behavior.
        private BaseAI _mahaonForcedAI;
        protected override BaseAI ForcedAI => _mahaonForcedAI ??= new MahaonNecroSummonerAI(this);

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Rich);
            AddLoot(LootPack.MedScrolls, 2);
        }

        public override void OnCarve(Mobile from, Corpse corpse, Item with)
        {
            base.OnCarve(from, corpse, with);
            MahaonCarvedBoneSystem.TryDropLichBone(from, corpse);
            corpse.Carved = true;
        }
    }
}
