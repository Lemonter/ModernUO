using System;
using ModernUO.Serialization;
using System.Collections.Generic;
using Server.Systems.MahaonSoulStones;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class VialOfBlood : Item
{
    [Constructible]
    public VialOfBlood(int amount = 1) : base(0x0E24)
    {
        Stackable = true;
        Amount = amount;
        Name = "флакон крови";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        from.SendMessage("Укажи самоцвет, который свяжешь с кровью.");
        from.Target = new BloodGemSelectTarget(this);
    }

    // Gem -> (skill, bonus). Flat +10 per socket — gems don't have a "size" the way soul
    // stones do, so no scaling tiers here.
    public static readonly Dictionary<Type, SkillName> GemSkillMap = new()
    {
        [typeof(StarSapphire)] = SkillName.Meditation,
        [typeof(Emerald)] = SkillName.Poisoning,
        [typeof(Sapphire)] = SkillName.Magery,
        [typeof(Ruby)] = SkillName.Tactics,
        [typeof(Citrine)] = SkillName.Mining,
        [typeof(Amethyst)] = SkillName.SpiritSpeak,
        [typeof(Tourmaline)] = SkillName.Lumberjacking,
        [typeof(Amber)] = SkillName.Fencing,
        [typeof(Diamond)] = SkillName.Anatomy
    };
}

public class BloodGemSelectTarget : Target
{
    private readonly VialOfBlood _blood;

    public BloodGemSelectTarget(VialOfBlood blood) : base(2, false, TargetFlags.None) => _blood = blood;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_blood.Deleted || !_blood.IsChildOf(from.Backpack))
        {
            return;
        }

        if (targeted is not Item gem || !gem.IsChildOf(from.Backpack) ||
            !VialOfBlood.GemSkillMap.TryGetValue(gem.GetType(), out var skill))
        {
            from.SendMessage("Этот самоцвет нельзя связать с кровью.");
            return;
        }

        from.SendMessage("Теперь укажи оружие, броню или украшение, куда вставить самоцвет.");
        from.Target = new BloodGemSocketTarget(_blood, gem, skill);
    }
}

public class BloodGemSocketTarget : Target
{
    private readonly VialOfBlood _blood;
    private readonly Item _gem;
    private readonly SkillName _skill;

    public BloodGemSocketTarget(VialOfBlood blood, Item gem, SkillName skill) : base(2, false, TargetFlags.None)
    {
        _blood = blood;
        _gem = gem;
        _skill = skill;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_blood.Deleted || _gem.Deleted || !_gem.IsChildOf(from.Backpack))
        {
            from.SendMessage("Самоцвет или флакон крови больше недоступны.");
            return;
        }

        if (targeted is not Item item || item is not (BaseWeapon or BaseArmor or BaseJewel))
        {
            from.SendMessage("Сюда нельзя вставить — только оружие, броня и украшения.");
            return;
        }

        if (!item.IsChildOf(from.Backpack) && item.Parent != from)
        {
            from.SendMessage("Предмет должен быть у тебя в руках или надет.");
            return;
        }

        if (SoulStoneSocketing.SocketCount(item) >= 3)
        {
            from.SendMessage("В этот предмет больше не вставить самоцветов — все слоты заняты.");
            return;
        }

        if (SoulStoneSocketing.TrySocket(item, new (SkillName? skill, StatType? stat, double bonus)[] { (_skill, null, 10) }))
        {
            _gem.Consume();
            _blood.Consume();
            from.SendMessage(0x59, $"Кровавый самоцвет приживается, даруя навык «{_skill.SkillNameRu()}».");
        }
        else
        {
            from.SendMessage("Не получилось.");
        }
    }
}
public static class SkillNameRuHelper
{
    public static string SkillNameRu(this SkillName skill) => skill switch
    {
        SkillName.Meditation     => "Медитация",
        SkillName.Poisoning      => "Отравление",
        SkillName.Magery         => "Магия",
        SkillName.Tactics        => "Тактика",
        SkillName.Mining         => "Горное дело",
        SkillName.SpiritSpeak    => "Спиритизм",
        SkillName.Lumberjacking  => "Лесное дело",
        SkillName.Fencing        => "Фехтование",
        SkillName.Anatomy        => "Анатомия",
        _                        => skill.ToString()
    };
}
