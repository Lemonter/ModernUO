using ModernUO.Serialization;
using System;
using System.Linq;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Equipment/Clothing/FirstAidBelt.cs).
///     Dropped (absent here): IsArtifact.
/// </summary>
[SerializationGenerator(0, false)]
public partial class FirstAidBelt : Container
{
    public override int LabelNumber => 1158681; // First Aid Belt

    public override int DefaultGumpID => 0x3C;
    public override int DefaultMaxItems => 1;
    public override int DefaultMaxWeight => 100;
    public override double DefaultWeight => 2.0;

    [SerializableField(0, fieldChanged: nameof(OnWeightReductionChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _weightReduction;

    [SerializableField(1, fieldChanged: nameof(OnHealingBonusChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _healingBonus;

    [SerializedIgnoreDupe]
    [SerializableField(2, setter: "private")]
    [SaveFlag(nameof(ShouldSerializeAttributes), nameof(AttributesDefaultValue))]
    [SerializedCommandProperty(AccessLevel.GameMaster, canModify: true)]
    private AosAttributes _attributes;

    private bool ShouldSerializeAttributes() => !_attributes.IsEmpty;

    private AosAttributes AttributesDefaultValue() => new(this);

    private void OnWeightReductionChanged(int oldValue, int newValue) => InvalidateProperties();

    private void OnHealingBonusChanged(int oldValue, int newValue) => InvalidateProperties();

    public Item Bandage => Items.Count > 0 ? Items[0] : null;
    public int MaxBandage => DefaultMaxWeight * 10;

    [Constructible]
    public FirstAidBelt()
        : base(0xA1F6)
    {
        Weight = 2.0;
        Layer = Layer.Waist;
        _attributes = new AosAttributes(this);
    }

    public override void OnAfterDuped(Item newItem)
    {
        var belt = newItem as FirstAidBelt;

        if (belt != null)
        {
            belt._attributes = new AosAttributes(newItem, _attributes);
        }

        base.OnAfterDuped(newItem);
    }

    public override bool DisplaysContent => false;

    public override void UpdateTotal(Item sender, TotalType type, int delta)
    {
        InvalidateProperties();

        base.UpdateTotal(sender, type, delta);
    }

    public override int GetTotal(TotalType type)
    {
        int total = base.GetTotal(type);

        if (type == TotalType.Weight)
            total -= total * _weightReduction / 100;

        return total;
    }

    private static readonly Type[] m_Bandage = new Type[]
    {
        typeof(Bandage), typeof(EnhancedBandage)
    };

    public bool CheckType(Item item)
    {
        Type type = item.GetType();
        Item bandage = Bandage;

        if (bandage != null)
        {
            if (bandage.GetType() == type)
                return true;
        }
        else
        {
            for (int i = 0; i < m_Bandage.Length; i++)
            {
                if (type == m_Bandage[i])
                    return true;
            }
        }

        return false;
    }

    public override bool CheckHold(Mobile m, Item item, bool message, bool checkItems, int plusItems, int plusWeight)
    {
        if (!Movable)
            return false;

        if (!CheckType(item))
        {
            if (message)
                m.SendLocalizedMessage(1074836); // The container can not hold that type of object.

            return false;
        }

        if (!checkItems || Items.Count < DefaultMaxItems)
        {
            int currentAmount = 0;

            Items.ForEach(i => currentAmount += i.Amount);

            if (item.Amount + currentAmount <= MaxBandage)
                return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
            else
                m.SendLocalizedMessage(1080017); // That container cannot hold more items.
        }

        return false;
    }

    public override void AddItem(Item dropped)
    {
        base.AddItem(dropped);

        InvalidateWeight();
    }

    public override void RemoveItem(Item dropped)
    {
        base.RemoveItem(dropped);

        InvalidateWeight();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_healingBonus > 0)
            list.Add(1158679, _healingBonus.ToString()); // ~1_VALUE~% Bandage Healing Bonus

        int prop;

        if ((prop = _attributes.DefendChance) != 0)
            list.Add(1060408, prop.ToString()); // defense chance increase ~1_val~%

        if ((prop = _attributes.BonusDex) != 0)
            list.Add(1060409, prop.ToString()); // dexterity bonus ~1_val~

        if ((prop = _attributes.EnhancePotions) != 0)
            list.Add(1060411, prop.ToString()); // enhance potions ~1_val~%

        if ((prop = _attributes.CastRecovery) != 0)
            list.Add(1060412, prop.ToString()); // faster cast recovery ~1_val~

        if ((prop = _attributes.CastSpeed) != 0)
            list.Add(1060413, prop.ToString()); // faster casting ~1_val~

        if ((prop = _attributes.AttackChance) != 0)
            list.Add(1060415, prop.ToString()); // hit chance increase ~1_val~%

        if ((prop = _attributes.BonusHits) != 0)
            list.Add(1060431, prop.ToString()); // hit point increase ~1_val~

        if ((prop = _attributes.BonusInt) != 0)
            list.Add(1060432, prop.ToString()); // intelligence bonus ~1_val~

        if ((prop = _attributes.LowerManaCost) != 0)
            list.Add(1060433, prop.ToString()); // lower mana cost ~1_val~%

        if ((prop = _attributes.LowerRegCost) != 0)
            list.Add(1060434, prop.ToString()); // lower reagent cost ~1_val~%

        if ((prop = _attributes.Luck) != 0)
            list.Add(1060436, prop.ToString()); // luck ~1_val~

        if ((prop = _attributes.BonusMana) != 0)
            list.Add(1060439, prop.ToString()); // mana increase ~1_val~

        if ((prop = _attributes.RegenMana) != 0)
            list.Add(1060440, prop.ToString()); // mana regeneration ~1_val~

        if ((prop = _attributes.NightSight) != 0)
            list.Add(1060441); // night sight

        if ((prop = _attributes.ReflectPhysical) != 0)
            list.Add(1060442, prop.ToString()); // reflect physical damage ~1_val~%

        if ((prop = _attributes.RegenStam) != 0)
            list.Add(1060443, prop.ToString()); // stamina regeneration ~1_val~

        if ((prop = _attributes.RegenHits) != 0)
            list.Add(1060444, prop.ToString()); // hit point regeneration ~1_val~

        if ((prop = _attributes.SpellDamage) != 0)
            list.Add(1060483, prop.ToString()); // spell damage increase ~1_val~%

        if ((prop = _attributes.BonusStam) != 0)
            list.Add(1060484, prop.ToString()); // stamina increase ~1_val~

        if ((prop = _attributes.BonusStr) != 0)
            list.Add(1060485, prop.ToString()); // strength bonus ~1_val~

        if ((prop = _attributes.WeaponSpeed) != 0)
            list.Add(1060486, prop.ToString()); // swing speed increase ~1_val~%

 // Lower Ammo Cost ~1_Percentage~%

        double weight = 0;

        if (Bandage != null)
            weight = Bandage.Weight * Bandage.Amount;

        list.Add(1072241, $"{Items.Count}	{DefaultMaxItems}	{(int)weight}	{DefaultMaxWeight}"); // Contents: ~1_COUNT~/~2_MAXCOUNT items, ~3_WEIGHT~/~4_MAXWEIGHT~ stones

        if ((prop = _weightReduction) != 0)
            list.Add(1072210, prop.ToString()); // Weight reduction: ~1_PERCENTAGE~%
    }

    public void InvalidateWeight()
    {
        if (RootParent is Mobile)
        {
            Mobile m = (Mobile)RootParent;

            m.UpdateTotals();
        }
    }
}
