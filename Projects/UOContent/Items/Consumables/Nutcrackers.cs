using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. CardOfSemidar и CraftItem.Data в этом репозитории отсутствуют,
///     поэтому карта из развёртки не выпадает, а тип шоколада из рецепта не подставляется.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ChocolateNutcracker : CandyCane, ICraftable
{
    public enum ChocolateType
    {
        Milk = 0x461,
        Dark = 0x465,
        White = 0x47E
    }

    [Constructible]
    public ChocolateNutcracker() : this(ChocolateType.Dark)
    {
    }

    [Constructible]
    public ChocolateNutcracker(ChocolateType type) : base(39952)
    {
        _wrapped = true;
        Type = type;
    }

    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public ChocolateType Type
    {
        get => _type;
        set
        {
            _type = value;
            Hue = !_wrapped ? (int)_type : 0;
            this.MarkDirty();
            InvalidateProperties();
        }
    }

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Wrapped
    {
        get => _wrapped;
        set
        {
            _wrapped = value;
            InvalidateID();
            this.MarkDirty();
            InvalidateProperties();
        }
    }

    public override int LabelNumber => _type switch
    {
        ChocolateType.Dark  => _wrapped ? 1156387 : 1156390,
        ChocolateType.White => _wrapped ? 1156389 : 1156392,
        _                   => _wrapped ? 1156388 : 1156391
    };

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack) && _wrapped)
        {
            Unwrap(from);
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        if (!_wrapped)
        {
            base.GetContextMenuEntries(from, ref list);
        }
    }

    public int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool,
        CraftItem craftItem, int resHue
    ) => quality;

    private void InvalidateID()
    {
        if (_wrapped)
        {
            ItemID = 39952;
            Hue = 0;
        }
        else
        {
            ItemID = 39954;
            Hue = (int)_type;
        }
    }

    private void Unwrap(Mobile from)
    {
        // *You carefully peel back the wrapper...*
        from.PrivateOverheadMessage(MessageType.Regular, 1154, 1156393, from.NetState);
        from.PlaySound(Utility.Random(0x21F, 4));

        Timer.DelayCall(TimeSpan.FromSeconds(1), () => Wrapped = false);
    }
}
