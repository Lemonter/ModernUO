using ModernUO.Serialization;
using Server.Systems.MahaonCombat;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class MahaonMagicHammer : Item
{
    [SerializableField(0)]
    private int _charges;

    [Constructible]
    public MahaonMagicHammer(int charges = 5) : base(0x0FB4)
    {
        Weight = 8.0;
        _charges = charges;
        Name = "магический молот";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (_charges <= 0)
        {
            from.SendMessage("У этого молота не осталось зарядов.");
            return;
        }

        from.SendMessage("Укажи оружие из высококачественной руды для зачарования.");
        from.Target = new MagicHammerEnchantTarget(this);
    }

    public bool ConsumeCharge()
    {
        if (_charges <= 0)
        {
            return false;
        }

        _charges--;

        if (_charges <= 0)
        {
            Delete();
        }

        return true;
    }
}

public class MagicHammerEnchantTarget : Target
{
    private readonly MahaonMagicHammer _hammer;

    public MagicHammerEnchantTarget(MahaonMagicHammer hammer) : base(2, false, TargetFlags.None) => _hammer = hammer;

    protected override void OnTarget(Mobile from, object targeted)
    {
        if (_hammer.Deleted)
        {
            return;
        }

        if (targeted is not BaseWeapon weapon || !weapon.IsChildOf(from.Backpack) && weapon.Parent != from)
        {
            from.SendMessage("Оружие должно быть у тебя в руках или надето, чтобы зачаровать его.");
            return;
        }

        if (weapon.Resource < CraftResource.Agapite)
        {
            from.SendMessage("Зачаровать можно только оружие из агапита, верита или валорита.");
            return;
        }

        if (WeaponEnchantment.IsEnchanted(weapon))
        {
            from.SendMessage("Это оружие уже зачаровано.");
            return;
        }

        if (!_hammer.ConsumeCharge())
        {
            from.SendMessage("У молота закончились заряды.");
            return;
        }

        WeaponEnchantment.Enchant(weapon);
        weapon.Hue = 0x480;
        from.SendMessage(0x59, "Искры трещат — оружие принимает потрескивающее зачарование.");
    }
}
