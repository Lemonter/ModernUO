using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Базовый BaseEngravingTool в этом репозитории отсутствует, поэтому клеймо реализовано
///     самостоятельно: выбор питомца, ввод нового имени и расход заряда. Перезарядка через
///     NPC-тинкерера не портирована.
/// </summary>
[SerializationGenerator(0, false)]
public partial class PetBrandingIron : Item, IUsesRemaining
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    [InvalidateProperties]
    private int _usesRemaining;

    [Constructible]
    public PetBrandingIron(int charges = 30) : base(0x9E87)
    {
        Weight = 1.0;
        Hue = 0x48D;
        LootType = LootType.Blessed;
        _usesRemaining = charges;
    }

    public override int LabelNumber => 1157314;

    public bool ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_usesRemaining > 0)
        {
            from.SendLocalizedMessage(1157379); // Select a pet to brand.
            from.Target = new InternalTarget(this);
        }
        else
        {
            // There are no charges left on this branding iron.
            from.SendLocalizedMessage(1157377);
        }
    }

    private class InternalTarget : Target
    {
        private readonly PetBrandingIron _iron;

        public InternalTarget(PetBrandingIron iron) : base(3, false, TargetFlags.None) => _iron = iron;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_iron.Deleted)
            {
                return;
            }

            if (targeted is not BaseCreature creature)
            {
                from.SendLocalizedMessage(1157375); // The selected pet cannot be branded by this branding iron.
                return;
            }

            if (!creature.Controlled || creature.ControlMaster != from)
            {
                from.SendLocalizedMessage(1157376); // The selected pet is not accessible to brand.
                return;
            }

            from.SendGump(new BrandPromptGump(_iron, creature));
        }
    }

    private class BrandPromptGump : StaticGump<BrandPromptGump>
    {
        private readonly PetBrandingIron _iron;
        private readonly BaseCreature _creature;

        public override bool Singleton => true;

        public BrandPromptGump(PetBrandingIron iron, BaseCreature creature) : base(50, 50)
        {
            _iron = iron;
            _creature = creature;
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(50, 50, 400, 200, 0xA28);
            builder.AddHtmlLocalized(50, 70, 400, 20, 1157374, 0x0); // <CENTER>Pet Branding Iron</CENTER>
            builder.AddLabel(75, 95, 0x481, "Введите новое имя питомца:");
            builder.AddImageTiled(75, 125, 350, 40, 0xDB0);
            builder.AddTextEntry(75, 125, 350, 40, 0x0, 0, "");

            builder.AddButton(125, 205, 0x81A, 0x81B, 1); // Okay
            builder.AddButton(320, 205, 0x819, 0x818, 0); // Cancel
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            if (_creature.Deleted || _iron.Deleted)
            {
                return;
            }

            if (info.ButtonID != 1)
            {
                return;
            }

            var text = info.GetTextEntry(0);

            if (string.IsNullOrWhiteSpace(text) || text.Length > 16)
            {
                from.SendLocalizedMessage(1157375); // The selected pet cannot be branded by this branding iron.
                return;
            }

            _creature.Name = text.Trim();
            _iron.UsesRemaining -= 1;

            from.SendLocalizedMessage(1157382); // You brand the pet.
        }
    }
}
