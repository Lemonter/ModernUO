using ModernUO.Serialization;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ShepherdsStaff : Item
{
    [Constructible]
    public ShepherdsStaff() : base(0x0E81)
    {
        Weight = 4.0;
        Name = "посох пастуха";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи своего питомца для тренировки.");
        from.Target = new ShepherdsStaffTarget();
    }
}

public class ShepherdsStaffTarget : Target
{
    private const double TrainChance = 0.6;
    private const int StatGain = 1;
    private const int ResistGain = 1;
    private const double SkillGain = 0.5;

    public ShepherdsStaffTarget() : base(3, false, TargetFlags.None)
    {
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (targeted is not BaseCreature pet)
        {
            from.SendMessage("Это не питомец.");
            return;
        }

        if (pet.ControlMaster != from)
        {
            from.SendMessage("Тренировать можно только своего питомца.");
            return;
        }

        var backpack = from.Backpack;
        var food = backpack?.FindItemByType<Item>(true, item => pet.CheckFoodPreference(item));

        if (food == null)
        {
            from.SendMessage("Нужна еда, которую этот питомец ест, чтобы его тренировать.");
            return;
        }

        food.Consume();

        if (Utility.RandomDouble() >= TrainChance)
        {
            from.SendMessage(0x22, $"{pet.Name} не в настроении сегодня учиться.");
            return;
        }

        // One random improvement per attempt: a stat, a resistance, or a skill point.
        switch (Utility.Random(3))
        {
            case 0:
                var stat = Utility.RandomList(StatType.Str, StatType.Dex, StatType.Int);
                switch (stat)
                {
                    case StatType.Str: pet.RawStr += StatGain; break;
                    case StatType.Dex: pet.RawDex += StatGain; break;
                    case StatType.Int: pet.RawInt += StatGain; break;
                }

                from.SendMessage(0x59, $"{pet.Name} становится крепче.");
                break;

            case 1:
                var resist = Utility.RandomList(
                    ResistanceType.Physical, ResistanceType.Fire, ResistanceType.Cold,
                    ResistanceType.Poison, ResistanceType.Energy
                );
                pet.SetResistance(resist, pet.GetResistance(resist) + ResistGain);
                from.SendMessage(0x59, $"{pet.Name} становится выносливее.");
                break;

            case 2:
                var skill = pet.Skills[Utility.Random(pet.Skills.Length)];
                if (skill != null && skill.Base < skill.Cap)
                {
                    skill.Base += SkillGain;
                    from.SendMessage(0x59, $"{pet.Name} оттачивает навык «{skill.Name}».");
                }

                break;
        }
    }
}
