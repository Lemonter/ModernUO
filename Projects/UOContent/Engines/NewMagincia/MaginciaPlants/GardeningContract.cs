/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/MaginciaPlants/GardeningContract.cs).
///
///     Контракт садовника: применяется к растению Новой Маджинции и на две недели снимает необходимость
///     ухаживать за ним. Повторно использовать контракт можно не чаще раза в два месяца.
///
///     Замены: <c>DateTime.UtcNow</c> → <see cref="Core.Now" />. Больше ничего не потребовалось.
/// </summary>
using ModernUO.Serialization;
using Server.Engines.NewMagincia;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GardeningContract : Item
{
    [Constructible]
    public GardeningContract() : base(0x14F0) => Weight = 1.0;

    public override int LabelNumber => 1155764; // Gardening Contract

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1080058); // This must be in your backpack to use it.
            return;
        }

        // Target the New Magincia plant you wish to have a gardener tend to...
        from.SendLocalizedMessage(1155765);
        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private readonly Item _contract;

        public InternalTarget(Item contract) : base(2, true, TargetFlags.None) => _contract = contract;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_contract.Deleted)
            {
                return;
            }

            if (targeted is not MaginciaPlantItem plant)
            {
                // This item can only be used on New Magincia Gardening Plants.
                from.SendLocalizedMessage(1155759);
                return;
            }

            if (plant.IsContract)
            {
                from.SendLocalizedMessage(1155761); // A Gardener is already tending to this plant.
                return;
            }

            if (plant.ContractTime.Month == Core.Now.Month)
            {
                from.SendLocalizedMessage(1155760); // You may do this once every other month.
                return;
            }

            plant.ContractTime = Core.Now;
            // You have hired a gardener to tend to your plant...
            from.SendLocalizedMessage(1155762);
            _contract.Delete();
        }
    }
}
