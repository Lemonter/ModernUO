using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Consumables/InfusedAlchemistsGem.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class InfusedAlchemistsGem : Item
{
    [Constructible]
    public InfusedAlchemistsGem()
        : base(0x1EA7)
    {
        Weight = 1.0;
    }

    public override int LabelNumber => 1113006;

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);

        list.Add(1070722, "Alchemy Skill Increaser + 1");
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.Skills[SkillName.Alchemy].Base += 1;
        from.SendMessage("Ваш навык алхимии повысился на 1!");
        this.Delete();
    }
}
