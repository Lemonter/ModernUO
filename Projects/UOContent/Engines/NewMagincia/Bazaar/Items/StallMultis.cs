/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Magincia Bazaar/Items/StallMultis.cs).
///
///     Мульти-палатки прилавков: три товарных и три «животных» стиля. Каждый стиль — это <see cref="BaseMulti" />
///     плюс один-два декоративных статика, которые ездят вместе с палаткой и удаляются вместе с ней.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Ручной <c>Serialize/Deserialize</c> → <c>[SerializationGenerator]</c>; список декораций —
///             <c>[SerializableField]</c> (<c>List&lt;Item&gt;</c> генератор умеет), поэтому владение
///             восстанавливается после рестарта.
///         </item>
///         <item>
///             <c>OnLocationChange</c>/<c>OnMapChange</c> вызывают <c>base</c>: в ModernUO там сбрасывается
///             кэш проходимости мульти, и без <c>base</c> палатка осталась бы с устаревшей маской.
///         </item>
///         <item>
///             Сеттеры <c>Movable</c>/<c>ItemID</c> не трогались: <see cref="BaseMulti" /> сам держит
///             <c>Movable = false</c>.
///         </item>
///         <item>
///             Данные мульти берутся из клиентских <c>MultiCollection.uop</c>/<c>multi.mul</c>, которые сервер
///             читает в <see cref="MultiData" />. Правок в <c>Distribution/</c> не требуется: для неизвестного
///             идентификатора <see cref="MultiData.GetComponents" /> возвращает пустой список, поэтому палатка
///             всё равно отрисуется клиентом, просто не будет блокировать движение на сервере.
///         </item>
///     </list>
/// </summary>
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public abstract partial class BaseBazaarMulti : BaseMulti
{
    [SerializableField(0, setter: "private")]
    private List<Item> _fillers;

    protected BaseBazaarMulti(int id) : base(id) => _fillers = new List<Item>();

    public void AddComponent(Item item)
    {
        if (item != null)
        {
            _fillers.Add(item);
        }
    }

    public override void OnLocationChange(Point3D old)
    {
        foreach (var item in _fillers)
        {
            if (item?.Deleted == false)
            {
                item.Location = new Point3D(X + (item.X - old.X), Y + (item.Y - old.Y), Z + (item.Z - old.Z));
            }
        }

        base.OnLocationChange(old);
    }

    public override void OnMapChange()
    {
        foreach (var item in _fillers)
        {
            if (item?.Deleted == false)
            {
                item.Map = Map;
            }
        }

        base.OnMapChange();
    }

    public override void OnAfterDelete()
    {
        foreach (var item in _fillers)
        {
            if (item?.Deleted == false)
            {
                item.Delete();
            }
        }

        _fillers.Clear();

        base.OnAfterDelete();
    }
}

[SerializationGenerator(0, false)]
public partial class CommodityStyle1 : BaseBazaarMulti
{
    [Constructible]
    public CommodityStyle1() : base(0x1772)
    {
        var component = new Static(1801);
        component.MoveToWorld(new Point3D(X + 1, Y + 1, Z), Map);
        AddComponent(component);
    }
}

[SerializationGenerator(0, false)]
public partial class CommodityStyle2 : BaseBazaarMulti
{
    [Constructible]
    public CommodityStyle2() : base(0x1773)
    {
        var component = new Static(9272);
        component.MoveToWorld(new Point3D(X + 1, Y, Z), Map);
        AddComponent(component);
    }
}

[SerializationGenerator(0, false)]
public partial class CommodityStyle3 : BaseBazaarMulti
{
    [Constructible]
    public CommodityStyle3() : base(0x1774)
    {
        var component = new Static(16527);
        component.MoveToWorld(new Point3D(X, Y, Z), Map);
        AddComponent(component);
    }
}

[SerializationGenerator(0, false)]
public partial class PetStyle1 : BaseBazaarMulti
{
    [Constructible]
    public PetStyle1() : base(0x1775)
    {
        var component = new Static(1036);
        component.MoveToWorld(new Point3D(X - 1, Y, Z), Map);
        AddComponent(component);
    }
}

[SerializationGenerator(0, false)]
public partial class PetStyle2 : BaseBazaarMulti
{
    [Constructible]
    public PetStyle2() : base(0x1777)
    {
        var component = new Static(6013);
        component.MoveToWorld(new Point3D(X, Y - 1, Z), Map);
        AddComponent(component);

        component = new Static(6013);
        component.MoveToWorld(new Point3D(X, Y + 1, Z), Map);
        AddComponent(component);
    }
}

[SerializationGenerator(0, false)]
public partial class PetStyle3 : BaseBazaarMulti
{
    [Constructible]
    public PetStyle3() : base(0x177B)
    {
        var component = new Static(2324);
        component.MoveToWorld(new Point3D(X - 1, Y, Z), Map);
        AddComponent(component);
    }
}
