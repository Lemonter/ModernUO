/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/FuseCord.cs).
///
///     Запальный шнур — последний расходник перезарядки: без него ствол не «взведён» (<c>Primed</c>),
///     и фитиль поджигать нечего. Тратится в <c>BaseShipCannon.TryPrime</c> / <c>BaseCannon.Prime</c>.
///     Складывается в стопку, товар.
///
///     Отличия: <c>TypeAlias("Server.Items.Fusecord")</c> не портирован — старых сейвов этого движка
///     с таким именем нет; <c>Name</c> по-русски, cliloc 1116305 не тронут.
/// </summary>
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class FuseCord : Item, ICommodity
{
    [Constructible]
    public FuseCord() : this(1)
    {
    }

    [Constructible]
    public FuseCord(int amount) : base(0x1420)
    {
        Stackable = true;
        Hue = 1164;
        Amount = amount;
        Name = "запальный шнур";
    }

    public override int LabelNumber => 1116305; // fuse cord

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;
}
