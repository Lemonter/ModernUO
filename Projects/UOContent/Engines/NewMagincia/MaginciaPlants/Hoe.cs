/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/MaginciaPlants/Hoe.cs).
///
///     Мотыга: вскапывает участок земли на Новой Маджинции и ставит туда <see cref="MaginciaPlantItem" />.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             <c>Mobile.Animate(AnimationType.Attack, 3)</c> в этом репозитории нет — используется обычная
///             шестипараметрическая форма с действием 11 (копающая анимация), как в горнодобыче.
///         </item>
///         <item>
///             <c>Timer.DelayCall(TimeSpan, TimerStateCallback, object[])</c> → <c>Timer.StartTimer(TimeSpan, Action)</c>
///             с замыканием на созданную кучу земли.
///         </item>
///         <item>
///             <c>Region.IsPartOf</c> сохранён; <c>LandTarget.Name</c> в этом репозитории — свойство.
///         </item>
///     </list>
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Engines.NewMagincia;
using Server.Engines.Plants;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Hoe : BaseAxe
{
    [Constructible]
    public Hoe() : base(0xE86)
    {
        Hue = 2524;
        Weight = 11.0;
        UsesRemaining = 50;
        ShowUsesRemaining = true;
    }

    public override int LabelNumber => 1150482; // hoe

    public override WeaponAbility PrimaryAbility => WeaponAbility.DoubleStrike;

    public override WeaponAbility SecondaryAbility => WeaponAbility.Disarm;

    public override int AosStrengthReq => 50;

    public override int AosMinDamage => 12;

    public override int AosMaxDamage => 16;

    public override int AosSpeed => 35;

    public override float MlSpeed => 3.00f;

    public override int InitMinHits => 31;

    public override int InitMaxHits => 60;

    public override WeaponAnimation DefAnimation => WeaponAnimation.Slash1H;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your backpack for you to use it.
            return;
        }

        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private readonly Hoe _hoe;

        public InternalTarget(Hoe hoe) : base(2, true, TargetFlags.None) => _hoe = hoe;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_hoe.Deleted || !_hoe.IsChildOf(from.Backpack))
            {
                return;
            }

            if (!MaginciaPlantSystem.Enabled)
            {
                from.SendMessage("Размещение растений на Новой Маджинции сейчас отключено.");
                return;
            }

            if (targeted is not LandTarget landTarget || from.Map == null)
            {
                return;
            }

            var map = from.Map;
            var region = Region.Find(landTarget.Location, map);

            if (region == null || !region.IsPartOf("Magincia") ||
                landTarget.Name != "dirt" && landTarget.Name != "grass")
            {
                from.SendLocalizedMessage(1150457); // The ground here is not good for gardening.
                return;
            }

            if (!MaginciaPlantSystem.CanAddPlant(from, landTarget.Location))
            {
                return;
            }

            if (!MaginciaPlantSystem.CheckDelay(from))
            {
                return;
            }

            if (from.Mounted || from.Flying)
            {
                from.SendLocalizedMessage(501864); // You can't mine while riding.
                return;
            }

            if (from.IsBodyMod && !from.Body.IsHuman)
            {
                from.SendLocalizedMessage(501865); // You can't mine while polymorphed.
                return;
            }

            _hoe.UsesRemaining--;

            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1150492); // You till a small area to plant.
            from.Animate(11, 5, 1, true, false, 0);

            var dirt = new MaginciaPlantItem { Owner = from };
            dirt.StartTimer();

            MaginciaPlantSystem.OnPlantPlanted(from, map);

            var location = landTarget.Location;
            Timer.StartTimer(TimeSpan.FromSeconds(0.7), () => dirt.MoveToWorld(location, map));
        }
    }
}
