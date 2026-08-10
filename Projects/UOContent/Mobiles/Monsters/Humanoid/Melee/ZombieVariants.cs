using ModernUO.Serialization;
using Server.Systems.MahaonUndead;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class BlackZombie : Zombie
    {
        [Constructible]
        public BlackZombie()
        {
            MahaonUndeadVariants.MakeBlack(this);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class BloodyZombie : Zombie
    {
        [Constructible]
        public BloodyZombie()
        {
            MahaonUndeadVariants.MakeBloody(this);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PoisonousZombie : Zombie
    {
        [Constructible]
        public PoisonousZombie()
        {
            Hue = 0x0483;
            Name = "ядовитый " + Name;
        }

        public override Poison HitPoison => Poison.Regular;
        public override double HitPoisonChance => 0.5;
    }
}
