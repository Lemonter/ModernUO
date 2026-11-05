using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     The eight Eodon tribe titles, ported from ServUO
///     (Scripts/Services/Expansions/Time Of Legends/Items/EodonRewardTitleScroll.cs).
///
///     Dropped: <c>EodonTribeRewardTitleToken</c>. ServUO's token derives from
///     <c>BaseRewardTitleToken</c> → <c>BaseRewardGump</c>, a title-picker gump framework that has no
///     equivalent here — this codebase only ships <see cref="BaseRewardTitleDeed" />, which applies a
///     title directly. The token is therefore not ported; the eight deeds below remain available as
///     loot/reward drops. ServUO's token used cliloc ids 1156691-1156698, but the base deed here turns
///     a cliloc into an announcement only, so the deeds carry the titles as Russian text instead and
///     set <see cref="Mobile.Title" /> for real.
/// </summary>
[SerializationGenerator(0, false)]
public partial class DefenderOfEodonTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Защитник Эодона";

    [Constructible]
    public DefenderOfEodonTitleDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class DefenderOfTheMyrmidexTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Защитник мирмидексов";

    [Constructible]
    public DefenderOfTheMyrmidexTitleDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class FlameOfTheJukariTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Пламя Джукари";

    [Constructible]
    public FlameOfTheJukariTitleDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class AmbusherOfTheKurakTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Засадник Курака";

    [Constructible]
    public AmbusherOfTheKurakTitleDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class TrooperOfTheBarakoTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Солдат Барако";

    [Constructible]
    public TrooperOfTheBarakoTitleDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ThunderOfTheUraliTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Гром Урали";

    [Constructible]
    public ThunderOfTheUraliTitleDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class HerderOfTheSakkhraTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Пастух Сакхра";

    [Constructible]
    public HerderOfTheSakkhraTitleDeed()
    {
    }
}

[SerializationGenerator(0, false)]
public partial class ColonizerOfTheBarrabTitleDeed : BaseRewardTitleDeed
{
    public override TextDefinition Title => "Колонизатор Барраба";

    [Constructible]
    public ColonizerOfTheBarrabTitleDeed()
    {
    }
}
