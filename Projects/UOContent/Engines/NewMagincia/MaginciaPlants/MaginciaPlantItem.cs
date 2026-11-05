/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/MaginciaPlants/MaginciaPlantItem.cs).
///
///     Куча земли на острове Новая Маджинция: сажается лопатой на землю, принадлежит конкретному игроку и
///     растёт по общим правилам <see cref="PlantItem" />.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Хуки <c>MaginciaPlant</c>/<c>BowlOfDirtID</c>/<c>GreenBowlID</c>/<c>ContainerLocalization</c>/
///             <c>OnPlantLocalization</c>/<c>CantUseLocalization</c> добавлены в <see cref="PlantItem" />
///             виртуальными членами со значениями ServUO по умолчанию — поведение обычных растений не менялось.
///             Там же <c>PlantSeed</c> стал <c>virtual bool</c>, <c>Die</c>/<c>IsUsableBy</c>/
///             <c>ValidGrowthLocation</c> — виртуальными, а <c>GetLocalizedContainerType</c> перестал быть
///             статическим (он возвращает <c>ContainerLocalization</c>).
///         </item>
///         <item>
///             <c>DateTime.UtcNow</c> → <see cref="Core.Now" />, <c>Timer.DelayCall(…, TimerCallback)</c> →
///             <c>Timer.StartTimer(…, Action)</c>; таймер отменяется в <see cref="OnAfterDelete" />.
///         </item>
///         <item>
///             Уведомление системы растений перенесено из <c>Delete()</c> в <see cref="OnAfterDelete" />: там
///             <c>PlantStatus</c> ещё доступен, а ссылки уже пора отпускать.
///         </item>
///         <item>
///             <c>IPooledEnumerable</c> больше нет — пространственный запрос обходится <c>foreach</c> без
///             <c>Free()</c>.
///         </item>
///         <item>
///             Часовой пояс «Eastern Standard Time» ищется один раз и через try/catch: на Linux такого
///             идентификатора нет, а свойство лишь украшает подсказку.
///         </item>
///         <item><c>ObjectPropertyList</c> → <see cref="IPropertyList" />.</item>
///         <item>
///             Версия 1 ServUO существовала ради добавленного позже <c>ContractTime</c>; здесь класс новый,
///             поэтому все четыре поля пишутся сразу в версии 0.
///         </item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Engines.Plants;
using Server.Items;

namespace Server.Engines.NewMagincia;

[SerializationGenerator(0, false)]
public partial class MaginciaPlantItem : PlantItem
{
    private static readonly TimeZoneInfo _easternTimeZone = ResolveEasternTimeZone();

    private TimerExecutionToken _deleteTimerToken;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Mobile _owner;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private DateTime _planted;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private DateTime _contractTime;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private DateTime _setToDecorative;

    [Constructible]
    public MaginciaPlantItem() : this(false)
    {
    }

    [Constructible]
    public MaginciaPlantItem(bool fertile) : base(fertile)
    {
        Movable = false;
        ItemID = BowlOfDirtID;
        Planted = Core.Now;
    }

    public override bool MaginciaPlant => true;

    public override int BowlOfDirtID => 2323; // mound of dirt

    public override int GreenBowlID => PlantStatus <= PlantStatus.Stage3 ? 0xC7E : 0xC62;

    public override int ContainerLocalization => 1150436; // mound of dirt

    public override int OnPlantLocalization => 1150442; // You plant the seed in the mound of dirt.

    public override int CantUseLocalization => 501648; // You cannot use this unless you are the owner.

    public override int LabelNumber
    {
        get
        {
            var label = base.LabelNumber;

            // 1029913 — «plant bowl»; у кучи земли своя подпись.
            return label == 1029913 ? 1022321 : label;
        }
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime ContractEndTime => ContractTime + TimeSpan.FromDays(14);

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsContract => ContractEndTime > Core.Now;

    [CommandProperty(AccessLevel.GameMaster)]
    public override bool ValidGrowthLocation => RootParent == null && !Movable;

    public override void OnDoubleClick(Mobile from)
    {
        if (PlantStatus >= PlantStatus.DecorativePlant)
        {
            return;
        }

        var location = GetWorldLocation();

        if (!from.InLOS(location) || !from.InRange(location, 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that.
            return;
        }

        if (!IsUsableBy(from))
        {
            LabelTo(from, CantUseLocalization);
            return;
        }

        MainPlantGump.DisplayTo(from, this);
    }

    public override bool IsUsableBy(Mobile from) =>
        RootParent == null && !Movable && Owner == from && IsAccessibleTo(from);

    public override void Die()
    {
        base.Die();

        Timer.StartTimer(TimeSpan.FromMinutes(Utility.RandomMinMax(2, 5)), Delete, out _deleteTimerToken);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (Owner == null)
        {
            return;
        }

        // Planted in ~1_val~ by: ~2_val~
        list.Add(1150474, $"#1011345\t{Owner.Name}");
        list.Add(1150478, _planted.ToShortDateString());

        if (IsContract)
        {
            // Gardening Contract Expires: ~1_TIME~
            list.Add(1155763, FormatEasternTime(ContractEndTime));
        }

        if (PlantStatus == PlantStatus.DecorativePlant)
        {
            list.Add(1150490, _setToDecorative.ToShortDateString()); // Date harvested: ~1_val~
        }
    }

    /// <summary>Ограничитель жизни кучи земли, пока в неё не посадили семя.</summary>
    public void StartTimer()
    {
        _deleteTimerToken.Cancel();
        Timer.StartTimer(TimeSpan.FromMinutes(2), Delete, out _deleteTimerToken);
    }

    public override bool PlantSeed(Mobile from, Seed seed)
    {
        if (!CheckLocation(from, seed) || !base.PlantSeed(from, seed))
        {
            return false;
        }

        _deleteTimerToken.Cancel();

        return true;
    }

    public override void OnAfterDelete()
    {
        _deleteTimerToken.Cancel();

        if (_owner != null && PlantStatus < PlantStatus.DecorativePlant)
        {
            MaginciaPlantSystem.OnPlantDelete(_owner, Map);
        }

        base.OnAfterDelete();
    }

    public bool BlocksMovement()
    {
        if (PlantStatus is PlantStatus.BowlOfDirt or PlantStatus.DeadTwigs)
        {
            return false;
        }

        var info = PlantTypeInfo.GetInfo(PlantType);
        var data = TileData.ItemTable[info.ItemID & TileData.MaxItemValue];

        return (data.Flags & TileFlag.Impassable) > 0;
    }

    public static bool BlocksMovement(Seed seed)
    {
        var info = PlantTypeInfo.GetInfo(seed.PlantType);
        var data = TileData.ItemTable[info.ItemID & TileData.MaxItemValue];

        return (data.Flags & TileFlag.Impassable) > 0;
    }

    [AfterDeserialization(false)]
    private void OnAfterDeserialization()
    {
        // Куча земли без содержимого после загрузки бессмысленна: её удаляли и в ServUO.
        if (PlantStatus == PlantStatus.BowlOfDirt)
        {
            Delete();
        }
    }

    private bool CheckLocation(Mobile from, Seed seed)
    {
        if (!BlocksMovement(seed))
        {
            return true;
        }

        foreach (var item in Map.GetItemsInRange(Location, 1))
        {
            if (item == this || item is not MaginciaPlantItem plant || !plant.BlocksMovement())
            {
                continue;
            }

            // Plants that block movement cannot be planted next to other plants that block movement.
            from.SendLocalizedMessage(1150434);
            return false;
        }

        return true;
    }

    private static string FormatEasternTime(DateTime utc)
    {
        if (_easternTimeZone == null)
        {
            return utc.ToString("MM-dd-yyyy HH:mm 'ET'");
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utc, _easternTimeZone).ToString("MM-dd-yyyy HH:mm 'ET'");
    }

    private static TimeZoneInfo ResolveEasternTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}
