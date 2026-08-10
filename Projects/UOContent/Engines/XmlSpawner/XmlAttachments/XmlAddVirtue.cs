using System;
using Server.Engines.Virtues;
using Server.Mobiles;

namespace Server.Engines.XmlSpawner2;

public class XmlAddVirtue : XmlAttachment
{
    private int m_DataValue; // default data
    private string m_Virtue;

    [CommandProperty(AccessLevel.GameMaster)]
    public int Value
    {
        get => m_DataValue;
        set => m_DataValue = value;
    }
    public string Virtue
    {
        get => m_Virtue;
        set => m_Virtue = value;
    }

    // These are the various ways in which the message attachment can be constructed.
    // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
    // Other overloads could be defined to handle other types of arguments

    // a serial constructor is REQUIRED
    public XmlAddVirtue(ASerial serial) : base(serial)
    {
    }

    [Attachable]
    public XmlAddVirtue(string virtue, int value)
    {
        Value = value;
        Virtue = virtue;
    }


    public override void Serialize(IGenericWriter writer)
    {
        base.Serialize(writer);

        writer.Write(0);
        // version 0
        writer.Write(m_DataValue);
        writer.Write(m_Virtue);

    }

    public override void Deserialize(IGenericReader reader)
    {
        base.Deserialize(reader);

        int version = reader.ReadInt();
        // version 0
        m_DataValue = reader.ReadInt();
        m_Virtue = reader.ReadString();
    }

    public override void OnAttach()
    {
        base.OnAttach();

        // apply the mod
        if (AttachedTo is PlayerMobile mobile)
        {
            // for players just add it immediately
            // lookup the virtue type
            VirtueName g = 0;
            bool valid = true;
            bool gainedPath = false;
            try
            {
                g = (VirtueName)Enum.Parse(typeof(VirtueName), Virtue, true);
            }
            catch
            {
                valid = false;
            }

            if (valid && AttachedTo is PlayerMobile pm)
            {
                VirtueSystem.Award(pm, g, Value, ref gainedPath);

                mobile.SendMessage($"Receive {OnIdentify(mobile)}");

                if (gainedPath)
                {
                    mobile.SendMessage($"You have gained a path in {Virtue}");
                }
            }
            else
            {
                mobile.SendMessage($"{Virtue}: no such Virtue");
            }
            // and then remove the attachment
            Timer.DelayCall(TimeSpan.Zero, Delete);
            //Delete();
        }
        else if (AttachedTo is Item)
        {
            // dont allow item attachments
            Delete();
        }

    }

    public override bool HandlesOnKilled => true;

    public override void OnKilled(Mobile killed, Mobile killer)
    {
        base.OnKilled(killed, killer);

        if (killer == null)
        {
            return;
        }

        VirtueName g = 0;
        bool valid = true;
        bool gainedPath = false;
        try
        {
            g = (VirtueName)Enum.Parse(typeof(VirtueName), Virtue, true);
        }
        catch
        {
            valid = false;
        }

        if (valid && killer is PlayerMobile pm)
        {
            // give the killer the Virtue

            VirtueSystem.Award(pm, g, Value, ref gainedPath);

            if (gainedPath)
            {
                killer.SendMessage($"You have gained a path in {Virtue}");
            }

            killer.SendMessage($"Receive {OnIdentify(killer)}");
        }
    }


    public override string OnIdentify(Mobile from) => $"{Value} {Virtue} Virtue points";
}
