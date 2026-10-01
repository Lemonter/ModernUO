using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Targeting;

namespace Server.Spells.SkillMasteries;

// Ported from real OSI/ServUO content (Scripts/Spells/Skill Masteries/Conduit.cs) —
// Necromancy mastery: marks a 6x6 zone with skulls; other targeted Necromancy spells cast
// on a target inside the zone splash to everyone else inside it. CheckAffected is the
// integration point other Necromancy spells would call (not wired into every existing
// Necromancy spell file this pass — see the session writeup).
public class ConduitSpell : SkillMasterySpell
{
    private static readonly SpellInfo Info = new(
        "Conduit", "Uus Corp Grav", 204, 9061,
        typeof(NoxCrystal), typeof(BatWing), typeof(GraveDust)
    );

    public override double RequiredSkill => 90;
    public override double UpKeep => 0;
    public override int RequiredMana => 40;
    public override bool PartyEffects => false;

    public override SkillName CastSkill => SkillName.Necromancy;
    public override SkillName DamageSkill => SkillName.SpiritSpeak;

    public int Strength { get; set; }
    public List<Item> Skulls { get; set; }
    public Rectangle2D Zone { get; set; }

    public ConduitSpell(Mobile caster, Item scroll) : base(caster, scroll, Info)
    {
    }

    public override void OnBeginCast()
    {
        base.OnBeginCast();
        Effects.SendLocationParticles(EffectItem.Create(Caster.Location, Caster.Map, EffectItem.DefaultDuration), 0x36CB, 1, 14, 0x55C, 7, 9915, 0);
    }

    public override void OnCast() => Caster.Target = new MasteryTarget(this, 10, true, TargetFlags.None);

    protected override void OnTarget(object o)
    {
        if (o is not IPoint3D p || !CheckSequence())
        {
            return;
        }

        var rec = new Rectangle2D(p.X - 3, p.Y - 3, 6, 6);
        Skulls = new List<Item>();

        void PlaceSkull(int x, int y)
        {
            var skull = new ConduitSkull();
            skull.MoveToWorld(new Point3D(x, y, Caster.Map.GetAverageZ(x, y)), Caster.Map);
            Skulls.Add(skull);
        }

        PlaceSkull(rec.X, rec.Y);
        PlaceSkull(rec.X + rec.Width, rec.Y + rec.Height);
        PlaceSkull(rec.X + rec.Width, rec.Y);
        PlaceSkull(rec.X, rec.Y + rec.Height);
        PlaceSkull(rec.X + rec.Width / 2, rec.Y + rec.Height / 2);

        Zone = rec;
        Strength = (int)((Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value + GetMasteryLevel() * 20) / 3.75);
        Expires = Core.Now + TimeSpan.FromSeconds(6);

        if (Caster is Mobiles.PlayerMobile pm)
        {
            // Targeted Necromancy spells used on a target within the Conduit field will
            // affect all valid targets within the field at ~1_PERCT~% strength.
            pm.AddBuff(new BuffInfo(BuffIcon.Conduit, 1155901, 1156053, default, Strength.ToString()));
        }

        BeginTimer();
    }

    public override void EndEffects()
    {
        if (Skulls != null)
        {
            foreach (var i in Skulls.Where(i => i is { Deleted: false }))
            {
                i.Delete();
            }
        }

        if (Caster is Mobiles.PlayerMobile pm)
        {
            pm.RemoveBuff(BuffIcon.Conduit);
        }
    }

    public static bool CheckAffected(Mobile caster, Mobile victim, Action<Mobile, double> callback)
    {
        if (victim?.Map == null)
        {
            return false;
        }

        foreach (var spell in EnumerateSpells(caster, typeof(ConduitSpell)))
        {
            if (spell is not ConduitSpell conduit || !conduit.Zone.Contains(victim.Location))
            {
                continue;
            }

            List<Mobile> toAffect = null;

            foreach (var m in victim.Map.GetMobilesInBounds(conduit.Zone))
            {
                if (m != victim && conduit.Caster.CanBeHarmful(m))
                {
                    (toAffect ??= new List<Mobile>()).Add(m);
                }
            }

            if (toAffect == null || callback == null)
            {
                continue;
            }

            foreach (var m in toAffect)
            {
                callback(m, conduit.Strength / 100.0);
            }

            return true;
        }

        return false;
    }
}

[SerializationGenerator(0, false)]
public partial class ConduitSkull : Item
{
    [Constructible]
    public ConduitSkull() : base(Utility.RandomList(0x1853, 0x1858))
    {
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Delete(); // Conduit's zone markers never survive a restart, same as ServUO's own.
}
