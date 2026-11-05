using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Отброшено (отсутствует в репозитории): IsArtifact, атрибут
///     Alterable, проверка Siege.SiegeShard и кража у монстров (StealingHandler из
///     Server.Engines.CreatureStealing). Строка свойств «Monster Stealing» сохранена.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SmugglersEdge : ButcherKnife
{
    [Constructible]
    public SmugglersEdge()
    {
        Hue = 1461;

        WeaponAttributes.UseBestSkill = 1;
        Attributes.SpellChanneling = 1;
        Attributes.WeaponSpeed = 30;

        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1071499; // Smuggler's Edge
    public override bool CanFortify => false;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int AosMinDamage => 9;
    public override int AosMaxDamage => 11;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1071500); // Monster Stealing
    }
}

/// <summary>
///     Портировано из ServUO. Отброшено (отсутствует в репозитории): IsArtifact, атрибут
///     Alterable, проверка Siege.SiegeShard и кража у монстров (StealingHandler из
///     Server.Engines.CreatureStealing). Строка свойств «Monster Stealing» сохранена.
/// </summary>
[SerializationGenerator(0, false)]
public partial class GargishSmugglersEdge : GargishButcherKnife
{
    [Constructible]
    public GargishSmugglersEdge()
    {
        Hue = 1461;

        WeaponAttributes.UseBestSkill = 1;
        Attributes.SpellChanneling = 1;
        Attributes.WeaponSpeed = 30;

        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1071499; // Smuggler's Edge
    public override bool CanFortify => false;

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;
    public override int AosMinDamage => 9;
    public override int AosMaxDamage => 11;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1071500); // Monster Stealing
    }
}
