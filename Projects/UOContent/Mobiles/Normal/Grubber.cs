using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Grubber.cs). The Combatant setter override
/// becomes OnCombatantChange, the hook ModernUO calls on the same transition.</summary>
[SerializationGenerator(0, false)]
public partial class Grubber : BaseCreature
{
    [Constructible]
    public Grubber() : base(AIType.AI_Animal, FightMode.None, 10, 1)
    {
        Body = 270;

        SetStr(15);
        SetDex(2000);
        SetInt(1000);

        SetHits(200);
        SetStam(500);
        SetMana(0);

        SetDamage(1);

        SetDamageType(ResistanceType.Physical, 100);

        SetSkill(SkillName.MagicResist, 200.0);
        SetSkill(SkillName.Tactics, 5.0);
        SetSkill(SkillName.Wrestling, 5.0);

        Fame = 1000;
        Karma = 0;

        VirtualArmor = 4;
    }

    public override string CorpseName => "труп грызуна";
    public override string DefaultName => "грызун";

    public override int Meat => 1;
    public override int Hides => 1;

    public override void OnCombatantChange()
    {
        base.OnCombatantChange();

        if (0.10 > Utility.RandomDouble())
        {
            StopFlee();
        }
        else if (!CheckFlee())
        {
            BeginFlee(TimeSpan.FromSeconds(10));
        }
    }

    public override int GetAttackSound() => 0xC9;
    public override int GetHurtSound() => 0xCA;
    public override int GetDeathSound() => 0xCB;
}
