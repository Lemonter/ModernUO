using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     ServUO-интерфейс IPromotionalToken и ChangeHairstyleGump с обратным вызовом здесь
///     отсутствуют: смена причёски сделана собственным гуммом токена. Списки причёсок
///     эльфов и горгулий не портированы (в ChangeHairstyleEntry их нет).
/// </summary>
[SerializationGenerator(0, false)]
public partial class GenderChangeToken : Item
{
    [SerializableField(0)]
    private int _hairID;

    [SerializableField(1)]
    private int _beardID;

    [Constructible]
    public GenderChangeToken() : base(0x2AAA)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1070997; // a promotional token

    public TextDefinition ItemName => 1075252; // gender change

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1070998, ItemName.GetValue()); // Use this to redeem<br>Your ~1_PROMO~ : gender change
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        if (from.IsBodyMod || from.HueMod > 0 || !from.CanBeginAction(typeof(Server.Spells.Fifth.IncognitoSpell)))
        {
            from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
            return;
        }

        if (from is not PlayerMobile pm)
        {
            return;
        }

        _hairID = 0;
        _beardID = 0;

        from.SendGump(new GenderChangeConfirmGump(pm, this));
    }

    public void BeginHairSelection(Mobile from) => GenderHairGump.DisplayTo(from, this, false);

    public void OnChangeHairstyle(Mobile from, bool facialHair, int itemID)
    {
        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        if (from.IsBodyMod || from.HueMod > 0 || !from.CanBeginAction(typeof(Server.Spells.Fifth.IncognitoSpell)))
        {
            from.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
            return;
        }

        if (facialHair)
        {
            _beardID = itemID;
        }
        else
        {
            _hairID = itemID;
        }

        if (!from.Female || from.Race == Race.Elf || facialHair)
        {
            EndGenderChange(from);
        }
        else
        {
            GenderHairGump.DisplayTo(from, this, true);
        }
    }

    public void OnFailedHairstyle(Mobile from, bool facialHair)
    {
        if (!IsChildOf(from.Backpack))
        {
            return;
        }

        if (facialHair || from.Female && from.Race != Race.Elf)
        {
            GenderHairGump.DisplayTo(from, this, !facialHair);
            return;
        }

        EndGenderChange(from);
    }

    private void EndGenderChange(Mobile from)
    {
        if (from.Female)
        {
            from.Body = from.Race.MaleBody;
            from.Female = false;
        }
        else
        {
            from.Body = from.Race.FemaleBody;
            from.Female = true;
        }

        if ((from.Female || from.Race == Race.Elf) && _beardID != 0)
        {
            _beardID = 0;
        }

        from.FacialHairItemID = _beardID;
        from.HairItemID = _hairID;

        from.SendMessage(from.Female ? "Теперь вы женщина." : "Теперь вы мужчина.");
        Delete();
    }

    private class GenderHairGump : DynamicGump
    {
        private readonly GenderChangeToken _token;
        private readonly Mobile _from;
        private readonly bool _facialHair;
        private readonly ChangeHairstyleEntry[] _entries;

        public override bool Singleton => true;

        private GenderHairGump(GenderChangeToken token, Mobile from, bool facialHair) : base(50, 50)
        {
            _token = token;
            _from = from;
            _facialHair = facialHair;
            _entries = facialHair ? ChangeHairstyleEntry.BeardEntries : ChangeHairstyleEntry.HairEntries;
        }

        public static void DisplayTo(Mobile from, GenderChangeToken token, bool facialHair)
        {
            if (from?.NetState == null || token.Deleted)
            {
                return;
            }

            from.SendGump(new GenderHairGump(token, from, facialHair));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var tableWidth = _facialHair ? 2 : 3;
            var tableHeight = (_entries.Length + tableWidth - (_facialHair ? 1 : 2)) / tableWidth;
            const int offsetWidth = 123;
            var offsetHeight = _facialHair ? 70 : 65;
            var tableWidthOffset = 81 + tableWidth * offsetWidth;
            var tableHeightOffset = 45 + tableHeight * offsetHeight;

            builder.AddPage();

            builder.AddBackground(0, 0, tableWidthOffset, 60 + tableHeightOffset, 2600);

            builder.AddButton(45, tableHeightOffset, 4005, 4007, 1);
            builder.AddHtmlLocalized(77, tableHeightOffset, 90, 35, 1006044); // Ok

            builder.AddButton(tableWidthOffset - 180, tableHeightOffset, 4005, 4007, 0);
            builder.AddHtmlLocalized(tableWidthOffset - 148, tableHeightOffset, 90, 35, 1006045); // Cancel

            if (_facialHair)
            {
                builder.AddHtmlLocalized(55, 15, 200, 20, 1018354); // <center>New Beard</center>
            }
            else
            {
                builder.AddHtmlLocalized(50, 15, 350, 20, 1018353); // <center>New Hairstyle</center>
            }

            for (var i = 0; i < _entries.Length; ++i)
            {
                var yTable = Math.DivRem(i, tableWidth, out var xTable);
                var xOffset = xTable * offsetWidth;
                var yOffset = yTable * offsetHeight;
                var entry = _entries[i];

                if (entry.GumpID != 0)
                {
                    builder.AddRadio(40 + xOffset, 70 + yOffset, 208, 209, false, i);
                    builder.AddBackground(87 + xOffset, 50 + yOffset, 50, 50, 2620);
                    builder.AddImage(87 + xOffset + entry.X, 50 + yOffset + entry.Y, entry.GumpID);
                }
                else if (!_facialHair)
                {
                    builder.AddRadio(40 + (xTable + 1) * offsetWidth, 240, 208, 209, false, i);
                    builder.AddHtmlLocalized(60 + (xTable + 1) * offsetWidth, 240, 85, 35, 1011064); // Bald
                }
                else
                {
                    builder.AddRadio(40 + xOffset, 70 + yOffset, 208, 209, false, i);
                    builder.AddHtmlLocalized(60 + xOffset, 70 + yOffset, 85, 35, 1011064); // Bald
                }
            }
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_token.Deleted)
            {
                return;
            }

            var switches = info.Switches;

            if (info.ButtonID != 1 || switches.Length <= 0)
            {
                _token.OnFailedHairstyle(_from, _facialHair);
                return;
            }

            var index = switches[0];

            if (index < 0 || index >= _entries.Length)
            {
                return;
            }

            _token.OnChangeHairstyle(_from, _facialHair, _entries[index].ItemID);
        }
    }
}

public class GenderChangeConfirmGump : StaticGump<GenderChangeConfirmGump>
{
    private readonly GenderChangeToken _token;
    private readonly PlayerMobile _pm;

    public override bool Singleton => true;

    public GenderChangeConfirmGump(PlayerMobile pm, GenderChangeToken token) : base(100, 100)
    {
        _pm = pm;
        _token = token;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 291, 159, 9200);
        builder.AddImageTiled(5, 5, 281, 20, 2702);
        builder.AddImageTiled(5, 30, 281, 100, 2702);

        builder.AddHtmlLocalized(8, 5, 279, 20, 1075249, 0x7FFF); // Change your character's gender.

        builder.AddButton(5, 132, 0xFB1, 0xFB3, 0);
        builder.AddHtmlLocalized(40, 132, 100, 20, 1011012, 0x7FFF); // CANCEL

        // Click OK to change your gender...
        builder.AddHtmlLocalized(8, 30, 279, 124, _pm.Female ? 1075254 : 1075253, 0x7FFF);

        builder.AddButton(126, 132, 0xFB7, 0xFB9, 1);
        builder.AddHtmlLocalized(160, 132, 120, 20, _pm.Female ? 1075251 : 1075250, 0x7FFF);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID != 1 || !_token.IsChildOf(_pm.Backpack))
        {
            return;
        }

        if (_pm.IsBodyMod || _pm.HueMod > 0 || !_pm.CanBeginAction(typeof(Server.Spells.Fifth.IncognitoSpell)))
        {
            _pm.SendLocalizedMessage(1073648); // You may only proceed while in your original state...
            return;
        }

        _token.BeginHairSelection(_pm);
    }
}
