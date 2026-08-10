using Server.Commands;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Gumps;

public class XmlSetTimeSpanGump : Gump
{
    private readonly PropertyInfo m_Property;
    private readonly Mobile m_Mobile;
    private readonly object m_Object;
    private readonly Stack<StackEntry> m_Stack;
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
    private const int TotalHeight = OffsetSize + 7 * (EntryHeight + OffsetSize);

    private const int BackWidth = BorderSize + TotalWidth + BorderSize;
    private const int BackHeight = BorderSize + TotalHeight + BorderSize;

    public XmlSetTimeSpanGump(PropertyInfo prop, Mobile mobile, object o, Stack<StackEntry> stack, int page, List<object> list) : base(GumpOffsetX, GumpOffsetY)
    {
        m_Property = prop;
        m_Mobile = mobile;
        m_Object = o;
        m_Stack = stack;
        m_Page = page;
        m_List = list;

        TimeSpan ts = (TimeSpan)prop.GetValue(o, null);

        AddPage(0);

        AddBackground(0, 0, BackWidth, BackHeight, BackGumpID);
        AddImageTiled(BorderSize, BorderSize, TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0), TotalHeight, OffsetGumpID);

        AddRect(0, prop.Name, 0, -1);
        AddRect(1, ts.ToString(), 0, -1);
        AddRect(2, "Zero", 1, -1);
        AddRect(3, "From H:M:S", 2, -1);
        AddRect(4, "H:", 3, 0);
        AddRect(5, "M:", 4, 1);
        AddRect(6, "S:", 5, 2);
    }

    private void AddRect(int index, string str, int button, int text)
    {
        int x = BorderSize + OffsetSize;
        int y = BorderSize + OffsetSize + index * (EntryHeight + OffsetSize);

        AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
        AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, str);

        if (text != -1)
        {
            AddTextEntry(x + 16 + TextOffsetX, y, EntryWidth - TextOffsetX - 16, EntryHeight, TextHue, text, "");
        }

        x += EntryWidth + OffsetSize;

        if (SetGumpID != 0)
        {
            AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
        }

        if (button != 0)
        {
            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, button);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        TimeSpan toSet;
        bool shouldSet, shouldSend;

        var h = info.GetTextEntry(0);
        var m = info.GetTextEntry(1);
        var s = info.GetTextEntry(2);

        switch (info.ButtonID)
        {
            case 1: // Zero
                {
                    toSet = TimeSpan.Zero;
                    shouldSet = true;
                    shouldSend = true;

                    break;
                }
            case 2: // From H:M:S
                {
                    if (h != null && m != null && s != null)
                    {
                        try
                        {
                            toSet = TimeSpan.Parse($"{h}:{m}:{s}");
                            shouldSet = true;
                            shouldSend = true;

                            break;
                        }
                        catch
                        {
                        }
                    }

                    toSet = TimeSpan.Zero;
                    shouldSet = false;
                    shouldSend = false;

                    break;
                }
            case 3: // From H
                {
                    if (h != null)
                    {
                        try
                        {
                            toSet = TimeSpan.FromHours(Utility.ToDouble(h));
                            shouldSet = true;
                            shouldSend = true;

                            break;
                        }
                        catch
                        {
                        }
                    }

                    toSet = TimeSpan.Zero;
                    shouldSet = false;
                    shouldSend = false;

                    break;
                }
            case 4: // From M
                {
                    if (m != null)
                    {
                        try
                        {
                            toSet = TimeSpan.FromMinutes(Utility.ToDouble(m));
                            shouldSet = true;
                            shouldSend = true;

                            break;
                        }
                        catch
                        {
                        }
                    }

                    toSet = TimeSpan.Zero;
                    shouldSet = false;
                    shouldSend = false;

                    break;
                }
            case 5: // From S
                {
                    if (s != null)
                    {
                        try
                        {
                            toSet = TimeSpan.FromSeconds(Utility.ToDouble(s));
                            shouldSet = true;
                            shouldSend = true;

                            break;
                        }
                        catch
                        {
                        }
                    }

                    toSet = TimeSpan.Zero;
                    shouldSet = false;
                    shouldSend = false;

                    break;
                }
            default:
                {
                    toSet = TimeSpan.Zero;
                    shouldSet = false;
                    shouldSend = true;

                    break;
                }
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

        if (shouldSend)
        {
            m_Mobile.SendGump(new XmlPropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
        }
    }
}
