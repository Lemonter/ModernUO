using Server.Mobiles;

namespace Server.Systems.MahaonUndead;

/// <summary>
///     Shared helper for the three undead variant colors — applied by each concrete
///     variant subclass (e.g. BlackSkeleton, BloodyZombie) right after base() finishes
///     building the normal creature, so the multipliers stack on top of whatever that
///     specific creature's own base stats already are, rather than hardcoding absolute
///     numbers per creature type.
/// </summary>
public static class MahaonUndeadVariants
{
    public const int BlackHue = 0x0001; // solid black dye hue
    public const int BloodyHue = 0x0021; // deep blood-red hue

    public static void MakeBlack(BaseCreature c)
    {
        c.Hue = BlackHue;
        c.HitsMaxSeed = c.HitsMaxSeed <= 0 ? c.HitsMaxSeed : c.HitsMaxSeed * 3;
        c.Hits = c.HitsMax;
        c.Name = "чёрный " + c.Name;
    }

    public static void MakeBloody(BaseCreature c)
    {
        c.Hue = BloodyHue;
        c.DamageMin *= 3;
        c.DamageMax *= 3;
        c.Name = "кровавый " + c.Name;
    }
}
