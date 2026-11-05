using ModernUO.Serialization;
using Server.Gumps;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Свойства BlessedBy у снаряжения в этом репозитории нет, поэтому
///     грамота выставляет только BlessedFor и персональную привязку предмета.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PersonalBlessDeed : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _owner;

    [Constructible]
    public PersonalBlessDeed() : base(0x14F0)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    [Constructible]
    public PersonalBlessDeed(Mobile owner) : this() => _owner = owner;

    public override bool DisplayLootType => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var name = _owner != null && !_owner.Deleted ? _owner.Name : "игрока";

        list.Add(1062195, name); // personal bless deed for ~1_NAME~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from != _owner)
        {
            from.SendLocalizedMessage(1062201, _owner != null ? _owner.Name : "другого игрока");
            // Only the owner, ~1_NAME~, can use this deed.
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        from.SendGump(
            new WarningGump(1060635, 30720, 1062202, 32512, 420, 280, okay =>
            {
                if (!okay)
                {
                    return;
                }

                from.SendLocalizedMessage(500506); // Which item do you wish to bless?
                from.Target = new PersonalBlessTarget(this);
            })
        );
    }
}

public class PersonalBlessTarget : Target
{
    private readonly PersonalBlessDeed _deed;

    public PersonalBlessTarget(PersonalBlessDeed deed) : base(-1, false, TargetFlags.None) => _deed = deed;

    protected override void OnTarget(Mobile from, object target)
    {
        if (target is Mobile)
        {
            from.SendLocalizedMessage(500507); // You can only bless an item!
            return;
        }

        if (target is not Item item)
        {
            from.SendLocalizedMessage(500509); // You can't bless this object!
            return;
        }

        if (item.LootType == LootType.Blessed || item.BlessedFor != null || item.Insured)
        {
            from.SendLocalizedMessage(1045113); // That item is already blessed
        }
        else if (item.LootType != LootType.Regular)
        {
            from.SendLocalizedMessage(500509); // You can't bless this object!
        }
        else if (item.RootParent != from)
        {
            from.SendLocalizedMessage(500508); // You may only bless objects that you are carrying.
        }
        else if (_deed.RootParent != from)
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
        }
        else
        {
            item.BlessedFor = from;
            from.SendLocalizedMessage(1062204); // You personally bless the item for this character.
            from.PlaySound(0x202);
            _deed.Delete();
        }
    }
}
