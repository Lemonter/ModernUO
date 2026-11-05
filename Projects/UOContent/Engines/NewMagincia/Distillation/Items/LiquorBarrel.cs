/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/Items/LiquorBarrel.cs).
///
///     Бочка, в которой настаивается самогон: наполняется через <see cref="DistillationGump" />, затем
///     выдерживается и разливается по бутылкам.
///
///     Замены:
///     <list type="bullet">
///         <item><c>DateTime.UtcNow</c> → <see cref="Core.Now" /> (единое игровое время, а не системные часы).</item>
///         <item><c>ObjectPropertyList</c> → <see cref="IPropertyList" />, <c>String.Format</c> → интерполяция в аргументе.</item>
///         <item><c>ITool</c> в <see cref="ICraftable.OnCraft" /> → <see cref="BaseTool" /> (в этом репозитории интерфейса нет).</item>
///         <item><c>craftItem.Resources.GetAt(0).ItemType</c> → <c>craftItem.Resources[0].ItemType</c> с проверкой на пустой список.</item>
///         <item>Опечатка ServUO <c>MutrationDuration</c> исправлена на <c>MaturationDuration</c>.</item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Engines.Distillation;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class LiquorBarrel : Item, ICraftable
{
    [SerializableField(0, fieldChanged: nameof(OnLiquorChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Liquor _liquor;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private DateTime _maturationBegin;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private TimeSpan _maturationDuration;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private string _label;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _isStrong;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private int _usesRemaining;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private bool _exceptional;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Mobile _crafter;

    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private Mobile _distiller;

    [Constructible]
    public LiquorBarrel() : base(4014)
    {
        _liquor = Liquor.None;
        _usesRemaining = 0;
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsMature =>
        _liquor != Liquor.None &&
        (_maturationDuration == TimeSpan.MinValue || _maturationBegin + _maturationDuration < Core.Now);

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsEmpty => _liquor == Liquor.None;

    // liquor barrel
    public override int LabelNumber => _usesRemaining == 0 || _liquor == Liquor.None ? 1150816 : 1150807;

    public override double DefaultWeight => 5.0;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack) || _usesRemaining <= 0)
        {
            return;
        }

        if (IsMature)
        {
            var bottle = new BottleOfLiquor(_liquor, _label, _isStrong, _distiller);

            if (from.Backpack == null || !from.Backpack.TryDropItem(from, bottle, false))
            {
                bottle.Delete();
                from.SendLocalizedMessage(500720); // You don't have enough room in your backpack!
                return;
            }

            from.PlaySound(0x240);
            from.SendMessage("Вы разливаете напиток по бутылкам и убираете их в сумку.");
            _usesRemaining--;
            InvalidateProperties();
            return;
        }

        // You need to wait until the liquor in the barrel has matured.
        from.SendLocalizedMessage(1150806);

        var readyAt = _maturationBegin + _maturationDuration;

        if (Core.Now >= readyAt)
        {
            return;
        }

        var remaining = readyAt - Core.Now;

        if (remaining.TotalDays > 0)
        {
            from.SendLocalizedMessage(1150814, $"{remaining.Days}\t{remaining.Hours}");
        }
        else
        {
            from.SendLocalizedMessage(1150813, $"{remaining.TotalHours}");
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_crafter != null)
        {
            list.Add(1050043, _crafter.Name); // Crafted By: ~1_Name~
        }

        if (_exceptional)
        {
            list.Add(1018303); // Exceptional
        }

        if (IsEmpty)
        {
            return;
        }

        if (IsMature)
        {
            list.Add(1060584, $"{_usesRemaining}"); // uses remaining: ~1_val~
        }

        list.Add(1150805, _maturationBegin.ToShortDateString()); // start date: ~1_NAME~

        var cliloc = IsMature ? 1150804 : 1150812; // matured: ~1_NAME~ / maturing: ~1_NAME~

        if (!string.IsNullOrEmpty(_label))
        {
            list.Add(cliloc, _label);
        }
        else
        {
            list.Add(cliloc, $"#{DistillationSystem.GetLabel(_liquor, _isStrong)}");
        }

        // Liquor Type: ~1_TYPE~
        list.Add(1150454, $"#{DistillationSystem.GetLabel(_liquor, _isStrong)}");

        if (_distiller != null)
        {
            list.Add(1150679, _distiller.Name); // Distiller: ~1_NAME~
        }
    }

    public void BeginDistillation(Liquor liquor)
    {
        var duration = liquor is Liquor.Spirytus or Liquor.Akvavit
            ? TimeSpan.MinValue
            : DistillationSystem.MaturationPeriod;

        BeginDistillation(liquor, duration, _label, _isStrong, _distiller);
    }

    public void BeginDistillation(Liquor liquor, TimeSpan duration, string label, bool isStrong, Mobile distiller)
    {
        Liquor = liquor;
        _maturationDuration = duration;
        _label = label;
        _isStrong = isStrong;
        _distiller = distiller;
        _maturationBegin = Core.Now;
        _usesRemaining = _exceptional ? 20 : 10;

        InvalidateProperties();
    }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    )
    {
        if (quality >= 2)
        {
            _exceptional = true;

            if (makersMark)
            {
                _crafter = from;
            }
        }

        if (typeRes == null && craftItem.Resources.Count > 0)
        {
            typeRes = craftItem.Resources[0].ItemType;
        }

        if (typeRes != null)
        {
            Hue = CraftResources.GetHue(CraftResources.GetFromType(typeRes));
        }

        return quality;
    }

    private void OnLiquorChanged(Liquor oldValue, Liquor newValue)
    {
        if (newValue != Liquor.None)
        {
            BeginDistillation(newValue);
        }
    }
}
