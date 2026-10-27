using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Дождевая лужа. Живёт, пока идёт дождь, и высыхает через минуту после него — этим
///     распоряжается PuddleSystem, поэтому своего срока у лужи нет.
///
///     Почему предмет, а не правка карты: лужа временная, а журнал правок (MahaonMapEdits)
///     вечный и append-only. Каждая гроза дописывала бы в него тысячи записей, которые
///     назавтра уже ничего не значат. Предмет исчезает бесследно.
///
///     Графика взята проходимая (см. PuddleSystem), так что по луже ходят, а не спотыкаются
///     об неё.
/// </summary>
[SerializationGenerator(0, false)]
public partial class MahaonPuddle : Item
{
    public MahaonPuddle(int itemId) : base(itemId)
    {
        Movable = false;
        Name = "лужа";
    }

    // Перезапуск сервера лужи не переживают: дождь после загрузки — уже другой дождь, а
    // учёт PuddleSystem живёт в памяти и про эти лужи ничего не знает. Поэтому старые
    // убираем вскоре после загрузки, вместо того чтобы оставлять их лежать навсегда.
    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Timer.DelayCall(TimeSpan.FromMinutes(1), Delete);
    }

    public override bool OnMoveOver(Mobile m) => true;
}
