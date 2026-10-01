using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonMetals;
using Server.Misc;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Ratman : BaseCreature
    {
        [Constructible]
        public Ratman() : base(AIType.AI_Melee)
        {
            Name = NameList.RandomName("ratman");
            Body = 42;
            BaseSoundID = 437;

            SetStr(96, 120);
            SetDex(81, 100);
            SetInt(36, 60);

            SetHits(58, 72);

            SetDamage(4, 5);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 25, 30);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 10, 20);
            SetResistance(ResistanceType.Energy, 10, 20);

            SetSkill(SkillName.MagicResist, 35.1, 60.0);
            SetSkill(SkillName.Tactics, 50.1, 75.0);
            SetSkill(SkillName.Wrestling, 50.1, 75.0);

            Fame = 1500;
            Karma = -1500;

            VirtualArmor = 28;

            // Mahaon: полный латный набор одного цвета (тёмная сталь) + щит и оружие —
            // раньше этот ratman был совсем безоружным и голым.
            // Mahaon: 0x3B2 вообще не был цветом металла — латы теперь мельхиоровые,
            // с настоящим материалом в тултипе (см. Orc.cs).
            AddItem(MahaonMetalTracker.Forge(new PlateChest(), MahaonMetal.Melchior));
            AddItem(MahaonMetalTracker.Forge(new PlateArms(), MahaonMetal.Melchior));
            AddItem(MahaonMetalTracker.Forge(new PlateGloves(), MahaonMetal.Melchior));
            AddItem(MahaonMetalTracker.Forge(new PlateGorget(), MahaonMetal.Melchior));
            AddItem(MahaonMetalTracker.Forge(new PlateLegs(), MahaonMetal.Melchior));
            AddItem(MahaonMetalTracker.Forge(new MetalKiteShield(), MahaonMetal.Melchior));
            AddItem(new WarMace());
        }

        public override string CorpseName => "труп крысолюда";
        public override InhumanSpeech SpeechType => InhumanSpeech.Ratman;

        public override bool CanRummageCorpses => true;
        public override int Hides => 8;
        public override HideType HideType => HideType.Spined;

        public override void GenerateLoot()
        {
            AddLoot(LootPack.Meager);
            // TODO: weapon, misc
        }
    }
}
