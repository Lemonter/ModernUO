using ModernUO.Serialization;
using Server.Systems.MahaonUndead;

namespace Server.Mobiles
{
    [SerializationGenerator(0, false)]
    public partial class BlackSkeletalKnight : SkeletalKnight
    {
        [Constructible]
        public BlackSkeletalKnight()
        {
            MahaonUndeadVariants.MakeBlack(this);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class BloodySkeletalKnight : SkeletalKnight
    {
        [Constructible]
        public BloodySkeletalKnight()
        {
            MahaonUndeadVariants.MakeBloody(this);
        }
    }

    [SerializationGenerator(0, false)]
    public partial class PoisonousSkeletalKnight : SkeletalKnight
    {
        [Constructible]
        public PoisonousSkeletalKnight()
        {
            Hue = 0x0483;
            Name = "ядовитый " + Name;
        }

        public override Poison HitPoison => Poison.Regular;
        public override double HitPoisonChance => 0.5;
    }
}
