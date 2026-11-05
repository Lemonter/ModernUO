using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     ServUO-токен кладёт предмет в банк; свойство PlaceInBank в этом репозитории отсутствует,
///     поэтому предмет всегда попадает в банковский ящик базового PromotionalToken.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CharacterReincarnationToken : PromotionalToken
{
    [Constructible]
    public CharacterReincarnationToken()
    {
        LootType = LootType.Blessed;
    }

    public override TextDefinition ItemName => 1156612; // Character Reincarnation
    public override TextDefinition ItemReceiveMessage => null;
    public override TextDefinition ItemGumpName => "<center>Character Reincarnation</center>";

    public override Item CreateItemFor(Mobile from)
    {
        var bag = new Bag();

        bag.DropItem(new GenderChangeToken());
        bag.DropItem(new NameChangeToken());
        bag.DropItem(new RaceChangeToken());

        return bag;
    }
}
