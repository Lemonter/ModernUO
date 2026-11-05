using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO (Scripts/Items/Artifacts/Decorative/ImprisonedDog.cs) — статуэтка-темница,
///     из которой выпускают травестийного пса. Пёс умеет на минуту принимать облик встретившегося
///     игрока, копируя его тело, волосы и надетые вещи (копии — <see cref="ClonedItem" />, они не
///     наследуют свойства, только внешний вид).
///
///     Отличия от оригинала: у вложенного <see cref="ClonedItem" /> не было ни атрибута генератора, ни
///     конструктора — при загрузке мира он ругался «No serialization constructor». Теперь это обычный
///     сериализуемый предмет. Имя и таймер следующей попытки морфа сохранены как поля сериализации,
///     чтобы превращение переживало перезапуск; ручные Serialize/Deserialize из ServUO удалены.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ImprisonedDog : BaseImprisonedMobile
{
    [Constructible]
    public ImprisonedDog() : base(0x1F1C)
    {
        Weight = 1.0;
        Hue = 0x485;
    }

    public override int LabelNumber => 1075091; // An Imprisoned Dog

    public override BaseCreature Summon => new TravestyDog();
}

[SerializationGenerator(0, false)]
public partial class TravestyDog : Dog
{
    [SerializableField(0)]
    private string _originalName;

    [SerializableField(1)]
    private DateTime _nextAttempt;

    private TimerExecutionToken _morphToken;

    [Constructible]
    public TravestyDog()
    {
        Hue = 2301;

        _originalName = null;
        _nextAttempt = Core.Now;
    }

    public override bool DeleteOnRelease => true;

    public bool Morphed => _originalName != null;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1049646); // (summoned)
    }

    public void DeleteItems()
    {
        for (var i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i] is ClonedItem)
            {
                Items[i].Delete();
            }
        }
    }

    public void BeginMorph(Mobile to)
    {
        if (to == null || !Alive || Morphed)
        {
            return;
        }

        _originalName = Name;

        Body = to.Body;
        Hue = to.Hue;
        Name = to.Name;
        Female = to.Female;
        Title = to.Title;
        HairItemID = to.HairItemID;
        HairHue = to.HairHue;
        FacialHairItemID = to.FacialHairItemID;
        FacialHairHue = to.FacialHairHue;

        for (var i = to.Items.Count - 1; i >= 0; i--)
        {
            var item = to.Items[i];

            if (item.Layer != Layer.Backpack && item.Layer != Layer.Mount)
            {
                AddItem(new ClonedItem(item));
            }
        }

        PlaySound(0x511);
        FixedParticles(0x376A, 1, 14, 5045, EffectLayer.Waist);

        _morphToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(60), EndMorph, out _morphToken);
    }

    public void EndMorph()
    {
        DeleteItems();

        Body = 0xD9;
        Hue = 2301;
        Name = _originalName;
        Female = false;
        Title = null;
        HairItemID = 0;
        HairHue = 0;
        FacialHairItemID = 0;
        FacialHairHue = 0;

        _originalName = null;

        PlaySound(0x511);
        FixedParticles(0x376A, 1, 14, 5045, EffectLayer.Waist);
    }

    protected override bool OnMove(Direction d)
    {
        if (!Morphed && _nextAttempt <= Core.Now)
        {
            foreach (var m in GetMobilesInRange(6))
            {
                if (!m.Hidden && m.Alive && Utility.RandomDouble() < 0.25)
                {
                    BeginMorph(m);
                    break;
                }
            }

            _nextAttempt = Core.Now + TimeSpan.FromSeconds(90);
        }

        return base.OnMove(d);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _morphToken.Cancel();
    }
}

/// <summary>Внешняя копия надетой вещи: только графика, оттенок и слой — без свойств оригинала.</summary>
[SerializationGenerator(0, false)]
public partial class ClonedItem : Item
{
    [Constructible]
    public ClonedItem() : base(0x1F1C)
    {
    }

    public ClonedItem(Item item) : base(item.ItemID)
    {
        Name = item.Name;
        Weight = item.Weight;
        Hue = item.Hue;
        Layer = item.Layer;
    }

    public override DeathMoveResult OnParentDeath(Mobile parent)
    {
        Delete();

        return DeathMoveResult.RemainEquipped;
    }
}
