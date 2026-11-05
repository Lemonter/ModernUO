using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/BattleChickenLizard.cs). The original
/// overrides the Combatant setter to roll a flee chance; here that is done in OnCombatantChange,
/// which is the hook ModernUO calls for the same transition. ServUO's OnAfterTame (speed reset,
/// stop fleeing) is dropped: speeds come from the speed table here, and there is no OnAfterTame
/// hook.</summary>
[SerializationGenerator(0, false)]
public partial class BattleChickenLizard : BaseCreature
{
    [Constructible]
    public BattleChickenLizard() : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1)
    {
        Body = 716;

        SetStr(94, 177);
        SetDex(78, 124);
        SetInt(6, 13);

        SetHits(94, 177);

        SetDamage(5, 15);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 15, 20);
        SetResistance(ResistanceType.Fire, 5, 15);

        SetSkill(SkillName.MagicResist, 30.0, 53.0);
        SetSkill(SkillName.Tactics, 50.0, 62.0);
        SetSkill(SkillName.Wrestling, 50.0, 62.0);

        Tamable = true;
        ControlSlots = 1;
        MinTameSkill = 0.0;
    }

    public override string CorpseName => "труп боевой куроящерицы";
    public override string DefaultName => "боевая куроящерица";

    public override int Meat => 3;
    public override MeatType MeatType => MeatType.Bird;
    public override FoodType FavoriteFood => FoodType.GrainsAndHay;

    public override int GetIdleSound() => 1511;
    public override int GetAngerSound() => 1508;
    public override int GetHurtSound() => 1510;
    public override int GetDeathSound() => 1509;

    public override void OnCombatantChange()
    {
        base.OnCombatantChange();

        if (Controlled)
        {
            return;
        }

        if (0.05 > Utility.RandomDouble())
        {
            StopFlee();
        }
        else if (!CheckFlee())
        {
            BeginFlee(TimeSpan.FromSeconds(30));
        }
    }

    public override bool CheckFlee()
    {
        if (Controlled)
        {
            return base.CheckFlee();
        }

        return Core.Now < EndFleeTime;
    }
}
