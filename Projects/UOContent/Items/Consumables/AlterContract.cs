using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. AlterItem в этом репозитории отсутствует, поэтому контракт не
///     запускает перековку: он только описывает заказанную работу и мастера.
/// </summary>
[SerializationGenerator(0, false)]
public partial class AlterContract : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private RepairSkillType _type;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _crafterName;

    [Constructible]
    public AlterContract(RepairSkillType type, Mobile crafter) : base(0x14F0)
    {
        _crafterName = crafter.Name;
        _type = type;

        Hue = 0x1BC;
        Weight = 1.0;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1094795, GetTitle()); // An alter service contract (~1_SKILL_NAME~)
        list.Add(1050043, _crafterName); // crafted by ~1_NAME~
        list.Add(1060636); // exceptional
    }

    public string GetTitle() => _type switch
    {
        RepairSkillType.Smithing   => "Blacksmithing",
        RepairSkillType.Carpentry  => "Carpentry",
        RepairSkillType.Tailoring  => "Tailoring",
        RepairSkillType.Tinkering  => "Tinkering",
        _                          => null
    };

    public CraftSystem GetCraftSystem() => _type switch
    {
        RepairSkillType.Smithing  => DefBlacksmithy.CraftSystem,
        RepairSkillType.Carpentry => DefCarpentry.CraftSystem,
        RepairSkillType.Tailoring => DefTailoring.CraftSystem,
        RepairSkillType.Tinkering => DefTinkering.CraftSystem,
        _                         => null
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            // The contract must be in your backpack to use it.
            from.SendLocalizedMessage(1047012);
        }
    }
}
