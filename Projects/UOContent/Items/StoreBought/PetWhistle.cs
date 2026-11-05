using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class PetWhistle : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextLinkedTime;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _account;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseCreature _petLinked;

    [Constructible]
    public PetWhistle() : base(0xA4E7)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1126239; // whistle

    [CommandProperty(AccessLevel.GameMaster)]
    public bool IsUsable => _nextLinkedTime < Core.Now;

    public override void OnDoubleClick(Mobile from)
    {
        if (!string.IsNullOrEmpty(_account) && from.Account?.Username != _account)
        {
            // This item is Account Bound, you are not permitted to take this action.
            from.SendLocalizedMessage(1151920);
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        if (_petLinked != null)
        {
            from.SendGump(new PetWhistleGump(this));
        }
        else
        {
            from.SendLocalizedMessage(1159390); // Target a bonded pet to link this whistle.
            from.Target = new InternalTarget(this);
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_petLinked != null)
        {
            list.Add(1159360, _petLinked.Name); // Pet Whistle for ~1_name~
        }

        if (!string.IsNullOrEmpty(_account))
        {
            list.Add(1155526); // Account Bound
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new LinkBondedPetEntry(this));
    }

    private class LinkBondedPetEntry : ContextMenuEntry
    {
        private readonly PetWhistle _item;

        public LinkBondedPetEntry(PetWhistle item) : base(1159393, 6) // Link Bonded Pet
        {
            _item = item;
            Enabled = _item.IsUsable;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (from == null || _item.Deleted)
            {
                return;
            }

            if (!_item.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                return;
            }

            from.SendLocalizedMessage(1159390); // Target a bonded pet to link this whistle.
            from.Target = new InternalTarget(_item);
        }
    }

    private class InternalTarget : Target
    {
        private readonly PetWhistle _item;

        public InternalTarget(PetWhistle item) : base(12, true, TargetFlags.None) => _item = item;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_item.Deleted)
            {
                return;
            }

            if (!_item.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
                return;
            }

            if (targeted is not BaseCreature creature || creature.ControlMaster != from)
            {
                from.SendLocalizedMessage(1149667); // Invalid target.
                return;
            }

            if (!creature.IsBonded)
            {
                from.SendLocalizedMessage(1159373); // This item can only be linked to a bonded pet.
                return;
            }

            if (!_item.IsUsable)
            {
                if ((Core.Now - _item._nextLinkedTime).Seconds > 60)
                {
                    // You must wait ~1_minutes~ minutes before you can link another pet to this whistle.
                    from.SendLocalizedMessage(1159391);
                }
                else
                {
                    // You must wait ~1_seconds~ seconds before you can link another pet to this whistle.
                    from.SendLocalizedMessage(1159392);
                }

                return;
            }

            _item._petLinked = creature;
            _item._nextLinkedTime = Core.Now + TimeSpan.FromDays(7);
            _item.InvalidateProperties();

            from.SendGump(new PetWhistleGump(_item));
        }
    }

    public class PetWhistleGump : StaticGump<PetWhistleGump>
    {
        private readonly PetWhistle _item;

        public override bool Singleton => true;

        public PetWhistleGump(PetWhistle item) : base(100, 100) => _item = item;

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 255, 260, 0x5B4);
            builder.AddAlphaRegion(0, 0, 255, 260);
            builder.AddItem(100, 39, 42215);

            builder.AddHtmlLocalized(81, 73, 157, 22, 1159405, 0x76F2);  // Attack
            builder.AddButton(41, 73, 0xFA5, 0xFA6, 1000);
            builder.AddHtmlLocalized(81, 97, 157, 22, 1159407, 0x76F2);  // Block
            builder.AddButton(41, 97, 0xFA5, 0xFA6, 1002);
            builder.AddHtmlLocalized(81, 121, 157, 22, 1159375, 0x76F2); // Play Dead
            builder.AddButton(41, 121, 0xFA5, 0xFA6, 1003);
            builder.AddHtmlLocalized(81, 145, 157, 22, 1159378, 0x76F2); // Settle Down
            builder.AddButton(41, 145, 0xFA5, 0xFA6, 1004);
            builder.AddHtmlLocalized(81, 169, 157, 22, 1159377, 0x76F2); // Wait
            builder.AddButton(41, 169, 0xFA5, 0xFA6, 1005);
            builder.AddHtmlLocalized(81, 193, 157, 22, 1159406, 0x76F2); // Eat
            builder.AddButton(41, 193, 0xFA5, 0xFA6, 1006);
            builder.AddHtmlLocalized(81, 217, 157, 22, 1159388, 0x76F2); // What's that?
            builder.AddButton(41, 217, 0xFA5, 0xFA6, 1008);

            if (_item._petLinked != null)
            {
                // Pet Whistle for ~1_name~
                builder.AddHtmlLocalized(40, 15, 200, 18, 1159360, _item._petLinked.Name, 0x6A05);
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_item.Deleted || _item._petLinked == null)
            {
                return;
            }

            var m = sender.Mobile;

            if (_item._petLinked.ControlOrder != OrderType.Stay || _item._petLinked.ControlMaster != m)
            {
                // You must command your pet to stay before using this item.
                m.SendLocalizedMessage(1159389);
            }

            Effects.PlaySound(m.Location, m.Map, 1665);

            // �������� ������� �������: ���� AnimationType � ����������� ���.
        }
    }
}
