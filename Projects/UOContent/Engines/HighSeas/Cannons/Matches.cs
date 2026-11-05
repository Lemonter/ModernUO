/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Items/Cannons and Ammo/Matches.cs).
///
///     Спички: единственный «ручной» источник огня для фитиля пушки. Пока спичка горит, ею можно
///     поджечь запал (прицел на пушку) — или просто держать в рюкзаке, тогда <c>TryLightFuse</c> сам
///     найдёт её при попытке выстрелить. Горящая спичка через час догорает и исчезает.
///
///     Отличия от исходника:
///     <list type="bullet">
///         <item>
///             <c>Timer</c> заменён на <c>Timer.StartTimer(..., out TimerExecutionToken)</c>; токен
///             отменяется в <c>OnDelete</c> (в ServUO таймер жил сам по себе и мог дёрнуть удалённую
///             спичку).
///         </item>
///         <item>
///             Признак «горит» сохраняется. В ServUO горящая спичка при загрузке удалялась; здесь она
///             просто гаснет — игрок не теряет предмет из-за рестарта.
///         </item>
///         <item>
///             Русские сообщения и имя предмета; cliloc-и 1116112–1116115 не тронуты.
///         </item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Targeting;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class Matches : Item, ICommodity
{
    private static readonly TimeSpan LightDuration = TimeSpan.FromMinutes(60);

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isLit;

    private TimerExecutionToken _burnToken;

    [Constructible]
    public Matches() : this(1)
    {
    }

    [Constructible]
    public Matches(int amount) : base(3947)
    {
        Stackable = true;
        Layer = Layer.TwoHanded;
        Amount = amount;
        Name = "спички";
    }

    public override int LabelNumber => 1116112; // matches

    int ICommodity.DescriptionNumber => LabelNumber;

    bool ICommodity.IsDeedable => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (Amount > 1)
        {
            // Отсыпаем одну спичку из стопки и зажигаем её: в ServUO точно так же.
            var pack = from.Backpack;
            var match = new Matches();

            if (pack?.CheckHold(from, match, true) == true)
            {
                pack.DropItem(match);
                Amount--;

                match.Light(from);
            }
            else
            {
                match.Delete();
            }

            return;
        }

        if (!_isLit)
        {
            Light(from);
            return;
        }

        from.Target = new InternalTarget(this);
        from.SendLocalizedMessage(1116113); // Target the cannon whose fuse you wish to light.
    }

    /// <summary>Зажигает спичку и запускает отсчёт до её прогорания.</summary>
    public void Light(Mobile from)
    {
        ItemID = 2578;
        _isLit = true;

        from?.SendSound(0x047);
        from?.SendLocalizedMessage(1116114); // You ignite the match.

        _burnToken.Cancel();
        Timer.StartTimer(LightDuration, BurnOut, out _burnToken);
    }

    public void BurnOut()
    {
        if (RootParent is Mobile mobile)
        {
            mobile.SendLocalizedMessage(1116115); // Your match splutters and dies.
        }

        Delete();
    }

    public override void OnDelete()
    {
        _burnToken.Cancel();

        base.OnDelete();
    }

    /// <summary>Гасит спичку при загрузке: срок горения не переживает рестарт.</summary>
    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (!_isLit)
        {
            return;
        }

        _isLit = false;
        ItemID = 3947;
    }

    private class InternalTarget : Target
    {
        private readonly Matches _match;

        public InternalTarget(Matches match) : base(3, false, TargetFlags.None) => _match = match;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_match.Deleted)
            {
                return;
            }

            if (targeted is not IShipCannon cannon)
            {
                return;
            }

            if (!cannon.CanLight)
            {
                from.SendLocalizedMessage(1116078); // There is no fuse to light! Prime the cannon first.
                return;
            }

            cannon.LightFuse(from);
        }
    }
}
