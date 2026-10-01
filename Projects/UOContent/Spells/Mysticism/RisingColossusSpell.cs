using System;
using Server.Mobiles;

namespace Server.Spells.Mysticism;

/// <summary>Mysticism 692, the school's 8th circle. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/RisingColossusSpell.cs); registration was
/// already in Spells/Initializer.cs, commented out — without it the school had no capstone.
///
/// Follows AnimatedWeaponSpell.cs next door for the local ground-target/summon idiom
/// (SpellTarget instead of ServUO's InternalTarget, BaseCreature.Summon rather than a
/// hand-rolled summon). The colossus itself is Mobiles/Monsters/Misc/Melee/RisingColossus.cs.</summary>
public class RisingColossusSpell : MysticSpell, ITargetingSpell<IPoint3D>
{
    private static readonly SpellInfo _info = new(
        "Rising Colossus",
        "Kal Vas Xen Corp Ylem",
        230,
        9022,
        Reagent.DaemonBone,
        Reagent.DragonsBlood,
        Reagent.FertileDirt,
        Reagent.Nightshade
    );

    public RisingColossusSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.Eighth;

    public int TargetRange => 12;

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Followers + 5 > Caster.FollowersMax)
        {
            Caster.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
            return false;
        }

        return true;
    }

    public void Target(IPoint3D p)
    {
        var map = Caster.Map;

        SpellHelper.GetSurfaceTop(ref p);

        if (map == null || Caster.Player && !map.CanSpawnMobile(p.X, p.Y, p.Z))
        {
            Caster.SendLocalizedMessage(501942); // That location is blocked.
        }
        else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
        {
            var baseSkill = GetBaseSkill(Caster);
            var boostSkill = GetDamageSkill(Caster);

            var duration = TimeSpan.FromSeconds((baseSkill + boostSkill) / 3);

            var summon = new RisingColossus(Caster, baseSkill, boostSkill);
            BaseCreature.Summon(summon, false, Caster, new Point3D(p), 0x656, duration);

            Effects.SendTargetParticles(summon, 0x3728, 10, 10, 0x13AA, (EffectLayer)255);
        }

        FinishSequence();
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);
    }
}
