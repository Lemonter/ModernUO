using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class Golem : BaseCreature
    {
        [Constructible]
        public Golem(bool summoned = false, double scalar = 1.0) : base(AIType.AI_Mage)
        {
            Body = 752;

            if (summoned)
            {
                Hue = 2101;
            }

            SetStr((int)(251 * scalar), (int)(350 * scalar));
            SetDex((int)(76 * scalar), (int)(100 * scalar));
            SetInt((int)(101 * scalar), (int)(150 * scalar));

            SetHits((int)(151 * scalar), (int)(210 * scalar));

            SetDamage((int)(13 * scalar), (int)(24 * scalar));

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, (int)(35 * scalar), (int)(55 * scalar));

            if (summoned)
            {
                SetResistance(ResistanceType.Fire, (int)(50 * scalar), (int)(60 * scalar));
            }
            else
            {
                SetResistance(ResistanceType.Fire, (int)(100 * scalar));
            }

            SetResistance(ResistanceType.Cold, (int)(10 * scalar), (int)(30 * scalar));
            SetResistance(ResistanceType.Poison, (int)(10 * scalar), (int)(25 * scalar));
            SetResistance(ResistanceType.Energy, (int)(30 * scalar), (int)(40 * scalar));

            // Mahaon: was AI_Melee. Every elemental and golem casts now; the spell
            // circle is held to 5 by Systems.MahaonCombat.ElementalMagerySystem, so
            // the Magery here is set for reliable casting, not to limit the circle.
            // Scaled like every other skill on this creature — a poorly built clockwork
            // golem should be a poorer caster too, not a full-strength one.
            SetSkill(SkillName.Magery, 70.1 * scalar, 90.0 * scalar);
            SetSkill(SkillName.EvalInt, 60.1 * scalar, 80.0 * scalar);
            SetSkill(SkillName.MagicResist, 150.1 * scalar, 190.0 * scalar);
            SetSkill(SkillName.Tactics, 60.1 * scalar, 100.0 * scalar);
            SetSkill(SkillName.Wrestling, 60.1 * scalar, 100.0 * scalar);

            if (summoned)
            {
                Fame = 10;
                Karma = 10;
            }
            else
            {
                Fame = 3500;
                Karma = -3500;
            }

            if (!summoned)
            {
                PackItem(new IronIngot(Utility.RandomMinMax(13, 21)));

                if (Utility.RandomDouble() < 0.1)
                {
                    PackItem(new PowerCrystal());
                }

                if (Utility.RandomDouble() < 0.15)
                {
                    PackItem(new ClockworkAssembly());
                }

                if (Utility.RandomDouble() < 0.2)
                {
                    PackItem(new ArcaneGem());
                }

                if (Utility.RandomDouble() < 0.25)
                {
                    PackItem(new Gears());
                }
            }

            ControlSlots = 3;
        }

        public override string CorpseName => "труп голема";

        public override bool IsScaredOfScaryThings => false;
        public override bool IsScaryToPets => true;

        public override bool IsBondable => false;

        public override FoodType FavoriteFood => FoodType.None;

        public override bool CanBeDistracted => false;

        public override string DefaultName => "голем";

        public override bool DeleteOnRelease => true;

        public override bool AutoDispel => !Controlled;
        public override bool BleedImmune => true;

        public override bool BardImmune => !Core.AOS || Controlled;
        public override Poison PoisonImmune => Poison.Lethal;

        private static MonsterAbility[] _abilities = { MonsterAbilities.ColossalBlow };
        public override MonsterAbility[] GetMonsterAbilities() => _abilities;

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (Utility.RandomDouble() < 0.05)
            {
                if (!IsParagon)
                {
                    if (Utility.RandomDouble() < 0.75)
                    {
                        c.DropItem(DawnsMusicGear.RandomCommon);
                    }
                    else
                    {
                        c.DropItem(DawnsMusicGear.RandomUncommon);
                    }
                }
                else
                {
                    c.DropItem(DawnsMusicGear.RandomRare);
                }
            }
        }

        public override int GetAngerSound() => 541;

        public override int GetIdleSound() => !Controlled ? 542 : base.GetIdleSound();

        public override int GetDeathSound() => !Controlled ? 545 : base.GetDeathSound();

        public override int GetAttackSound() => 562;

        public override int GetHurtSound() => Controlled ? 320 : base.GetHurtSound();

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            if (Controlled || Summoned)
            {
                var master = ControlMaster ?? SummonMaster;

                if (master?.Player == true && master.Map == Map && master.InRange(Location, 20))
                {
                    if (master.Mana >= amount)
                    {
                        master.Mana -= amount;
                    }
                    else
                    {
                        amount -= master.Mana;
                        master.Mana = 0;
                        master.Damage(amount);
                    }
                }
            }

            base.OnDamage(amount, from, willKill);
        }
    }
}
