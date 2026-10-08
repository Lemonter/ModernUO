using ModernUO.Serialization;
using Server.Systems.MahaonUndead;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class BlackSkeleton : Skeleton
    {
        [Constructible]
        public BlackSkeleton()
        {
            MahaonUndeadVariants.MakeBlack(this);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class BloodySkeleton : Skeleton
    {
        [Constructible]
        public BloodySkeleton()
        {
            MahaonUndeadVariants.MakeBloody(this);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PoisonousSkeleton : Skeleton
    {
        [Constructible]
        public PoisonousSkeleton()
        {
            Hue = 0x0483; // sickly green — matches the poison theme
            Name = "ядовитый " + Name;
        }

        public override Poison HitPoison => Poison.Regular;
        public override double HitPoisonChance => 0.5;
    }
}
