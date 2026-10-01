using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Harvest;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class FishingPole : Item
{
    /// <summary>Magic properties on the pole. ServUO's FishingPole carries its own AosAttributes
    /// (Scripts/Items/Tools/FishingPole.cs); this codebase's had none, so an artifact pole such
    /// as Xenrr's — which grants Spell Channeling and faster casting — had nowhere to put them.
    /// Added with the Underworld quest line.</summary>
    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private AosAttributes _attributes;

    [Constructible]
    public FishingPole() : base(0x0DC0)
    {
        Layer = Layer.TwoHanded;
        _attributes = new AosAttributes(this);
    }

    public override double DefaultWeight => 8.0;

    /// <summary>A pole with Spell Channeling doesn't have to be put away to cast.</summary>
    public override bool AllowEquippedCast(Mobile from) =>
        base.AllowEquippedCast(from) || _attributes.SpellChanneling != 0;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        _attributes?.GetProperties(list);
    }

    public override void OnAdded(IEntity parent)
    {
        base.OnAdded(parent);

        if (parent is not Mobile m)
        {
            return;
        }

        var modName = Serial.ToString();

        if (_attributes.BonusStr != 0)
        {
            m.AddStatMod(new StatMod(StatType.Str, $"{modName}Str", _attributes.BonusStr, TimeSpan.Zero));
        }

        if (_attributes.BonusDex != 0)
        {
            m.AddStatMod(new StatMod(StatType.Dex, $"{modName}Dex", _attributes.BonusDex, TimeSpan.Zero));
        }

        if (_attributes.BonusInt != 0)
        {
            m.AddStatMod(new StatMod(StatType.Int, $"{modName}Int", _attributes.BonusInt, TimeSpan.Zero));
        }

        m.CheckStatTimers();
    }

    public override void OnRemoved(IEntity parent)
    {
        base.OnRemoved(parent);

        if (parent is not Mobile m)
        {
            return;
        }

        var modName = Serial.ToString();

        m.RemoveStatMod($"{modName}Str");
        m.RemoveStatMod($"{modName}Dex");
        m.RemoveStatMod($"{modName}Int");

        m.CheckStatTimers();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _attributes ??= new AosAttributes(this);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from))
        {
            var loc = GetWorldLocation();

            if (!from.InLOS(loc) || !from.InRange(loc, 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3E9, 1019045); // I can't reach that
                return;
            }
        }

        Fishing.System.BeginHarvesting(from, this);
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        BaseHarvestTool.AddContextMenuEntries(from, this, ref list, Fishing.System);
    }

    public override bool CheckConflictingLayer(Mobile m, Item item, Layer layer)
    {
        if (base.CheckConflictingLayer(m, item, layer))
        {
            return true;
        }

        if (layer == Layer.OneHanded)
        {
            m.SendLocalizedMessage(500214); // You already have something in both hands.
            return true;
        }

        return false;
    }
}
