using Server.Commands;
using Server.Commands.Generic;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Gumps;

public class XmlSetObjectGump : Gump
{
    private readonly PropertyInfo m_Property;
    private readonly Mobile m_Mobile;
    private readonly object m_Object;
    private readonly Stack<StackEntry> m_Stack;
    private readonly Type m_Type;
    private readonly int m_Page;
    private readonly List<object> m_List;

    public static readonly bool OldStyle = false; // PropsConfig.OldStyle removed from ModernUO

    public const int GumpOffsetX = PropsConfig.GumpOffsetX;
    public const int GumpOffsetY = PropsConfig.GumpOffsetY;

    public const int TextHue = PropsConfig.TextHue;
    public const int TextOffsetX = PropsConfig.TextOffsetX;

    public const int OffsetGumpID = PropsConfig.OffsetGumpID;
    public const int EntryGumpID = PropsConfig.EntryGumpID;
    public const int BackGumpID = PropsConfig.BackGumpID;
    public const int SetGumpID = PropsConfig.SetGumpID;

    public const int SetWidth = PropsConfig.SetWidth;
    public const int SetOffsetX = PropsConfig.SetOffsetX, SetOffsetY = PropsConfig.SetOffsetY;
    public const int SetButtonID1 = PropsConfig.SetButtonID1;
    public const int SetButtonID2 = PropsConfig.SetButtonID2;

    public const int OffsetSize = PropsConfig.OffsetSize;

    public const int EntryHeight = PropsConfig.EntryHeight;
    public const int BorderSize = PropsConfig.BorderSize;

    private const int EntryWidth = 212;

    private const int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
    private const int TotalHeight = OffsetSize + 5 * (EntryHeight + OffsetSize);

    private const int BackWidth = BorderSize + TotalWidth + BorderSize;
    private const int BackHeight = BorderSize + TotalHeight + BorderSize;

    public XmlSetObjectGump(PropertyInfo prop, Mobile mobile, object o, Stack<StackEntry> stack, Type type, int page, List<object> list) : base(GumpOffsetX, GumpOffsetY)
    {
        m_Property = prop;
        m_Mobile = mobile;
        m_Object = o;
        m_Stack = stack;
        m_Type = type;
        m_Page = page;
        m_List = list;

        string initialText = XmlPropertiesGump.ValueToString(o, prop);

        AddPage(0);

        AddBackground(0, 0, BackWidth, BackHeight, BackGumpID);
        AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), TotalHeight, OffsetGumpID);

        int x = BorderSize + OffsetSize;
        int y = BorderSize + OffsetSize;

        AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
        AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, prop.Name);
        x += EntryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
        }

        x = BorderSize + OffsetSize;
        y += EntryHeight + OffsetSize;

        AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
        AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, initialText);
        x += EntryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
        }

        AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1);

        x = BorderSize + OffsetSize;
        y += EntryHeight + OffsetSize;

        AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
        AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Change by Serial");
        x += EntryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
        }

        AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 2);

        x = BorderSize + OffsetSize;
        y += EntryHeight + OffsetSize;

        AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
        AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "Nullify");
        x += EntryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
        }

        AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 3);

        x = BorderSize + OffsetSize;
        y += EntryHeight + OffsetSize;

        AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
        AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, "View Properties");
        x += EntryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
        }

        AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 4);
    }

    private class InternalPrompt : Prompt
    {
        private readonly PropertyInfo m_Property;
        private readonly Mobile m_Mobile;
        private readonly object m_Object;
        private readonly Stack<StackEntry> m_Stack;
        private readonly Type m_Type;
        private readonly int m_Page;
        private readonly List<object> m_List;

        public InternalPrompt(PropertyInfo prop, Mobile mobile, object o, Stack<StackEntry> stack, Type type, int page, List<object> list)
        {
            m_Property = prop;
            m_Mobile = mobile;
            m_Object = o;
            m_Stack = stack;
            m_Type = type;
            m_Page = page;
            m_List = list;
        }

        public override void OnCancel(Mobile from)
        {
            m_Mobile.SendGump(new XmlSetObjectGump(m_Property, m_Mobile, m_Object, m_Stack, m_Type, m_Page, m_List));
        }

        public override void OnResponse(Mobile from, string text)
        {
            object toSet;
            bool shouldSet;

            try
            {
                var serial = Utility.ToUInt32(text);
                toSet = World.FindEntity((Serial)serial);

                if (toSet == null)
                {
                    shouldSet = false;
                    m_Mobile.SendMessage("No object with that serial was found.");
                }
                else if (!m_Type.IsInstanceOfType(toSet))
                {
                    toSet = null;
                    shouldSet = false;
                    m_Mobile.SendMessage($"The object with that serial could not be assigned to a property of type : {m_Type.Name}");
                }
                else
                {
                    shouldSet = true;
                }
            }
            catch
            {
                toSet = null;
                shouldSet = false;
                m_Mobile.SendMessage("Bad format");
            }

            if (shouldSet)
            {
                try
                {
                    CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, toSet.ToString());
                    m_Property.SetValue(m_Object, toSet, null);
                }
                catch
                {
                    m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
                }
            }

            m_Mobile.SendGump(new XmlSetObjectGump(m_Property, m_Mobile, m_Object, m_Stack, m_Type, m_Page, m_List));
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        bool shouldSet, shouldSend = true;
        object viewProps = null;

        switch (info.ButtonID)
        {
            case 0: // closed
                {
                    m_Mobile.SendGump(new XmlPropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
                    shouldSet = false;
                    shouldSend = false;

                    break;
                }
            case 1: // Change by Target
                {
                    m_Mobile.Target = new XmlSetObjectTarget(m_Property, m_Mobile, m_Object, m_Stack, m_Type, m_Page, m_List);
                    shouldSet = false;
                    shouldSend = false;
                    break;
                }
            case 2: // Change by Serial
                {
                    shouldSet = false;
                    shouldSend = false;
                    m_Mobile.SendMessage("Enter the serial you wish to find:");
                    m_Mobile.Prompt = new InternalPrompt(m_Property, m_Mobile, m_Object, m_Stack, m_Type, m_Page, m_List);

                    break;
                }
            case 3: // Nullify
                {
                    shouldSet = true;
                    break;
                }
            case 4: // View Properties
                {
                    shouldSet = false;

                    object obj = m_Property.GetValue(m_Object, null);

                    if (obj == null)
                    {
                        m_Mobile.SendMessage("The property is null and so you cannot view its properties.");
                    }
                    else if (!BaseCommand.IsAccessible(m_Mobile, obj))
                    {
                        m_Mobile.SendMessage("You may not view their properties.");
                    }
                    else
                    {
                        viewProps = obj;
                    }

                    break;
                }
            default:
                {
                    shouldSet = false;
                    break;
                }
        }

        if (shouldSet)
        {
            try
            {
                CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, "(null)");
                m_Property.SetValue(m_Object, null, null);
            }
            catch
            {
                m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
            }
        }

        if (shouldSend)
        {
            m_Mobile.SendGump(new XmlSetObjectGump(m_Property, m_Mobile, m_Object, m_Stack, m_Type, m_Page, m_List));
        }

        if (viewProps != null)
        {
            m_Mobile.SendGump(new XmlPropertiesGump(m_Mobile, viewProps));
        }
    }
}
