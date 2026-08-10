using System;
using System.Buffers;
using Server.Commands.Generic;
using Server.Network;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;
using System.Reflection;
using Server.Gumps;
using Server.Items;
using System.IO;
using Server.Collections;
using Server.Misc;

namespace Server.Engines.XmlSpawner2;

[AttributeUsage(AttributeTargets.Constructor)]
public class Attachable : Attribute
{
    public Attachable()
    {
    }
}

public class ASerial
{

    private int m_SerialValue;

    public int Value => m_SerialValue;

    public ASerial(int serial) => m_SerialValue = serial;

    private static int m_GlobalSerialValue;

    public static bool serialInitialized;

    public static ASerial NewSerial()
    {
        // it is possible for new attachments to be constructed before existing attachments are deserialized and the current m_globalserialvalue
        // restored.  This creates a possible serial conflict, so dont allow assignment of valid serials until proper deser of m_globalserialvalue
        // Resolve unassigned serials in initialization
        if (!serialInitialized)
        {
            return new ASerial(0);
        }

        if (m_GlobalSerialValue == int.MaxValue || m_GlobalSerialValue < 0)
        {
            m_GlobalSerialValue = 0;
        }

        // try the next serial number in the series
        int newserialno = m_GlobalSerialValue + 1;

        // check to make sure that it is not in use
        while (XmlAttach.AllAttachments.ContainsKey(newserialno))
        {
            newserialno++;
            if (newserialno == int.MaxValue || newserialno < 0)
            {
                newserialno = 1;
            }
        }

        m_GlobalSerialValue = newserialno;

        return new ASerial(m_GlobalSerialValue);
    }

    internal static void GlobalSerialize(IGenericWriter writer)
    {
        writer.Write(m_GlobalSerialValue);
    }

    internal static void GlobalDeserialize(IGenericReader reader)
    {
        m_GlobalSerialValue = reader.ReadInt();
    }
}

public class XmlAttach
{

    private static Type m_AttachableType = typeof(Attachable);

    public static bool IsAttachable(ConstructorInfo ctor) => ctor.IsDefined(m_AttachableType, false);


    public static void HashSerial(ASerial key, XmlAttachment o)
    {
        if (key.Value != 0)
        {
            AllAttachments.Add(key.Value, o);
        }
        else
        {
            UnassignedAttachments.Add(o);
        }
    }

    // each entry in the dictionary is a list of XmlAttachments that is keyed by an object.
    public static Dictionary<Item, List<XmlAttachment>> ItemAttachments = new();   // keyed by item
    public static Dictionary<Mobile, List<XmlAttachment>> MobileAttachments = new(); // keyed by mobile
    public static Dictionary<int, XmlAttachment> AllAttachments = new();    // keyed by attachment serial
    private static List<XmlAttachment> UnassignedAttachments = new();

    public static bool HasAttachments(object o)
    {
        if (o == null)
        {
            return false;
        }

        if (o is Item item && ItemAttachments.TryGetValue(item, out var itemList))
        {
            // see if the attachment list is empty
            if (itemList == null || itemList.Count == 0)
            {
                return false;
            }

            // check to see if there are any valid attachments in the list
            foreach (XmlAttachment a in itemList)
            {
                if (!a.Deleted)
                {
                    return true;
                }
            }

            return false;
        }

        if (o is Mobile mobile && MobileAttachments.TryGetValue(mobile, out var mobileList))
        {
            // see if the attachment list is empty
            if (mobileList == null || mobileList.Count == 0)
            {
                return false;
            }

            // check to see if there are any valid attachments in the list
            foreach (XmlAttachment a in mobileList)
            {
                if (!a.Deleted)
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    public static XmlAttachment[] Values
    {
        get
        {
            XmlAttachment[] valuearray = new XmlAttachment[AllAttachments.Count];
            AllAttachments.Values.CopyTo(valuearray, 0);
            return valuearray;
        }
    }

    public static void Configure()
    {
        EventSink.WorldLoad += Load;
        EventSink.WorldSave += Save;
    }

    public static void Initialize()
    {
        ASerial.serialInitialized = true;

        // resolve unassigned serials
        foreach (XmlAttachment a in UnassignedAttachments)
        {
            // get the next unique serial id
            ASerial serial = ASerial.NewSerial();
            a.Serial = serial;

            // register the attachment in the serial keyed hashtable
            HashSerial(serial, a);
        }

        // Register our speech handler
        EventSink.Speech += EventSink_Speech;

        // Register our movement handler
        EventSink.Movement += EventSink_Movement;

        //CommandSystem.Register("ItemAtt", AccessLevel.GameMaster, new CommandEventHandler(ListItemAttachments_OnCommand));
        //CommandSystem.Register("MobAtt", AccessLevel.GameMaster, new CommandEventHandler(ListMobileAttachments_OnCommand));
        CommandSystem.Register("GetAtt", AccessLevel.GameMaster, GetAttachments_OnCommand);
        //CommandSystem.Register("DelAtt", AccessLevel.GameMaster, new CommandEventHandler(DeleteAttachments_OnCommand));
        //CommandSystem.Register("TrigAtt", AccessLevel.GameMaster, new CommandEventHandler(ActivateAttachments_OnCommand));
        //CommandSystem.Register("AddAtt", AccessLevel.GameMaster, new CommandEventHandler(AddAttachment_OnCommand));
        TargetCommands.Register(new AddAttCommand());
        TargetCommands.Register(new DelAttCommand());
        CommandSystem.Register("AvailAtt", AccessLevel.GameMaster, ListAvailableAttachments_OnCommand);
    }

    public class AddAttCommand : BaseCommand
    {
        public AddAttCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.All;
            Commands = ["AddAtt"];
            ObjectTypes = ObjectTypes.Both;
            Usage = "AddAtt type [args]";
            Description = "Adds an attachment to the targeted object.";
            ListOptimized = true;
        }

        public override bool ValidateArgs(BaseCommandImplementor impl, CommandEventArgs e)
        {
            if (e.Arguments.Length >= 1)
            {
                return true;
            }

            e.Mobile.SendMessage($"Usage: {Usage}");
            return false;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (e != null && list != null && e.Length >= 1)
            {

                // create a new attachment and add it to the item
                int nargs = e.Arguments.Length - 1;

                string[] args = new string[nargs];

                for (int j = 0; j < nargs; j++)
                {
                    args[j] = e.Arguments[j + 1];
                }

                Type attachtype = AssemblyHandler.FindTypeByName(e.Arguments[0]);

                if (attachtype != null && attachtype.IsSubclassOf(typeof(XmlAttachment)))
                {

                    // go through all of the objects in the list
                    int count = 0;

                    for (int i = 0; i < list.Count; ++i)
                    {

                        XmlAttachment o = (XmlAttachment)XmlSpawner.CreateObject(attachtype, args, false);

                        if (o == null)
                        {
                            AddResponse($"Unable to construct {attachtype.Name} with specified args");
                            break;
                        }

                        if (AttachTo(null, list[i], o, true))
                        {
                            if (list.Count < 10)
                            {
                                AddResponse($"Added {attachtype.Name} to {list[i]}");
                            }
                            count++;
                        }
                        else
                        {
                            LogFailure($"Attachment {attachtype.Name} not added to {list[i]}");
                        }
                    }
                    if (count > 0)
                    {
                        AddResponse($"Attachment {attachtype.Name} has been added [{count}]");
                    }
                    else
                    {
                        AddResponse($"Attachment {attachtype.Name} not added");
                    }
                }
                else
                {
                    AddResponse($"Invalid attachment type {e.Arguments[0]}");
                }
            }
        }
    }

    public class DelAttCommand : BaseCommand
    {
        public DelAttCommand()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.All;
            Commands = ["DelAtt"];
            ObjectTypes = ObjectTypes.Both;
            Usage = "DelAtt type";
            Description = "Deletes an attachment on the targeted object.";
            ListOptimized = true;
        }

        public override bool ValidateArgs(BaseCommandImplementor impl, CommandEventArgs e)
        {
            if (e.Arguments.Length >= 1)
            {
                return true;
            }

            e.Mobile.SendMessage($"Usage: {Usage}");
            return false;
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            if (e != null && list != null && e.Length >= 1)
            {

                Type attachtype = AssemblyHandler.FindTypeByName(e.Arguments[0]);

                if (attachtype != null && attachtype.IsSubclassOf(typeof(XmlAttachment)))
                {

                    // go through all of the objects in the list
                    int count = 0;

                    for (int i = 0; i < list.Count; ++i)
                    {
                        List<XmlAttachment> alist = FindAttachments(list[i], attachtype);

                        if (alist != null)
                        {
                            // delete the attachments
                            foreach (XmlAttachment a in alist)
                            {
                                a.Delete();
                                if (list.Count < 10)
                                {
                                    AddResponse($"Deleted {attachtype.Name} from {list[i]}");
                                }
                                count++;
                            }
                        }
                    }

                    if (count > 0)
                    {
                        AddResponse($"Attachment {attachtype.Name} has been deleted [{count}]");
                    }
                    else
                    {
                        AddResponse($"Attachment {attachtype.Name} not deleted");
                    }
                }
                else
                {
                    AddResponse($"Invalid attachment type {e.Arguments[0]}");
                }
            }
        }
    }

    public static void CleanUp()
    {
        // clean up any unowned attachments
        foreach (XmlAttachment a in Values)
        {
            if (a.OwnedBy == null || a.OwnedBy is Mobile mobile && mobile.Deleted || a.OwnedBy is Item item && item.Deleted)
            {
                a.Delete();
            }
        }
    }

    public static void Save()
    {
        if (MobileAttachments == null && ItemAttachments == null)
        {
            return;
        }

        CleanUp();

        if (!Directory.Exists("Saves/Attachments"))
        {
            Directory.CreateDirectory("Saves/Attachments");
        }

        string filePath = Path.Combine("Saves/Attachments", "Attachments.bin"); // the attachment serializations
        string imaPath = Path.Combine("Saves/Attachments", "Attachments.ima");  // the item/mob attachment tables
        string fpiPath = Path.Combine("Saves/Attachments", "Attachments.fpi");  // the file position indices

        // MemoryMapFileWriter(FileStream, size) was removed from ModernUO — FileBufferWriter
        // is the direct modern equivalent and takes the destination path itself.
        using var writer = new FileBufferWriter(filePath);
        using var imawriter = new FileBufferWriter(imaPath);
        using var fpiwriter = new FileBufferWriter(fpiPath);

        // save the current global attachment serial state
        ASerial.GlobalSerialize(writer);

        // remove all deleted attachments
        FullDefrag();

        // save the attachments themselves
        if (AllAttachments != null)
        {
            writer.Write(AllAttachments.Count);

            foreach (var kvp in AllAttachments)
            {
                // write the key
                writer.Write(kvp.Key);

                XmlAttachment a = kvp.Value;

                // write the value type
                writer.Write(a?.GetType().ToString());

                // serialize the attachment itself
                a?.Serialize(writer);

                // save the fileposition index
                fpiwriter.Write(writer.Position);
            }
        }
        else
        {
            writer.Write(0);
        }

        // save the dictionary info for items and mobiles
        // mobile attachments
        if (MobileAttachments != null)
        {
            imawriter.Write(MobileAttachments.Count);

            foreach (var kvp in MobileAttachments)
            {
                // write the key
                imawriter.Write(kvp.Key);

                // write out the attachments
                List<XmlAttachment> alist = kvp.Value;

                imawriter.Write(alist.Count);
                foreach (XmlAttachment a in alist)
                {
                    // write the attachment serial
                    imawriter.Write(a.Serial.Value);

                    // write the value type
                    imawriter.Write(a.GetType().ToString());

                    // save the fileposition index
                    fpiwriter.Write(imawriter.Position);
                }
            }
        }
        else
        {
            // no mobile attachments
            imawriter.Write(0);
        }

        // item attachments
        if (ItemAttachments != null)
        {
            imawriter.Write(ItemAttachments.Count);

            foreach (var kvp in ItemAttachments)
            {
                // write the key
                imawriter.Write(kvp.Key);

                // write out the attachments
                List<XmlAttachment> alist = kvp.Value;

                imawriter.Write(alist.Count);
                foreach (XmlAttachment a in alist)
                {
                    // write the attachment serial
                    imawriter.Write(a.Serial.Value);

                    // write the value type
                    imawriter.Write(a.GetType().ToString());

                    // save the fileposition index
                    fpiwriter.Write(imawriter.Position);
                }
            }
        }
        else
        {
            // no item attachments
            imawriter.Write(0);
        }
    }

    public static void Load()
    {
        string filePath = Path.Combine("Saves/Attachments", "Attachments.bin"); // the attachment serializations
        string imaPath = Path.Combine("Saves/Attachments", "Attachments.ima");  // the item/mob attachment tables
        string fpiPath = Path.Combine("Saves/Attachments", "Attachments.fpi");  // the file position indices

        if (!File.Exists(filePath))
        {
            return;
        }

        using var reader = new BinaryFileReader(filePath);
        using var imareader = new BinaryFileReader(imaPath);
        using var fpireader= new BinaryFileReader(fpiPath);

        // restore the current global attachment serial state
        try
        {
            ASerial.GlobalDeserialize(reader);
        }
        catch (Exception e)
        {
            ErrorReporter.GenerateErrorReport(e.ToString());
            return;
        }

        ASerial.serialInitialized = true;

        // read in the serial attachment hash table information
        int count;
        try
        {
            count = reader.ReadInt();
        }
        catch (Exception e)
        {
            ErrorReporter.GenerateErrorReport(e.ToString());
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // read the serial
            ASerial serialno = null;
            try
            {
                serialno = new ASerial(reader.ReadInt());
            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            // read the attachment type
            string valuetype = null;
            try
            {
                valuetype = reader.ReadString();
            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            // read the position of the beginning of the next attachment deser within the .bin file
            long position = 0;
            try
            {
                position = fpireader.ReadLong();

            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            bool skip = false;

            XmlAttachment o = null;
            try
            {
                o = (XmlAttachment)Activator.CreateInstance(Type.GetType(valuetype), new object[] { serialno });
            }
            catch
            {
                skip = true;
            }

            if (skip)
            {
                if (!AlreadyReported(valuetype))
                {
                    Console.WriteLine("\nError deserializing attachments {0}.\nMissing a serial constructor?\n", valuetype);
                    ReportDeserError(valuetype, "Missing a serial constructor?");
                }

                // position the .ima file at the next deser point
                try
                {
                    reader.Seek(position, SeekOrigin.Begin);
                }
                catch
                {
                    ErrorReporter.GenerateErrorReport(
                        "Error deserializing. Attachments save file corrupted. Attachment load aborted."
                  );
                    return;
                }

                continue;
            }

            try
            {
                o?.Deserialize(reader);
            }
            catch
            {
                skip = true;
            }

            // confirm the read position
            if (reader.Position != position || skip)
            {
                if (!AlreadyReported(valuetype))
                {
                    Console.WriteLine("\nError deserializing attachments {0}\n", valuetype);
                    ReportDeserError(valuetype, "save file corruption or incorrect Serialize/Deserialize methods?");
                }

                // position the .ima file at the next deser point
                try
                {
                    reader.Seek(position, SeekOrigin.Begin);
                }
                catch
                {
                    ErrorReporter.GenerateErrorReport(
                        "Error deserializing. Attachments save file corrupted. Attachment load aborted."
                  );
                    return;
                }

                continue;
            }

            // add it to the hash table
            try
            {
                AllAttachments.Add(serialno.Value, o);
            }
            catch
            {
                ErrorReporter.GenerateErrorReport(
                    $"\nError deserializing {valuetype} serialno {serialno.Value}. Attachments save file corrupted. Attachment load aborted.\n"
              );
                return;
            }
        }

        // read in the mobile attachment hash table information
        try
        {
            count = imareader.ReadInt();
        }
        catch (Exception e)
        {
            ErrorReporter.GenerateErrorReport(e.ToString());
            return;
        }

        for (int i = 0; i < count; i++)
        {

            Mobile key = null;
            try
            {
                key = imareader.ReadEntity<Mobile>();
            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            int nattach = 0;
            try
            {
                nattach = imareader.ReadInt();
            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            for (int j = 0; j < nattach; j++)
            {
                // and serial
                ASerial serialno = null;
                try
                {
                    serialno = new ASerial(imareader.ReadInt());
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                // read the attachment type
                string valuetype = null;
                try
                {
                    valuetype = imareader.ReadString();
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                // read the position of the beginning of the next attachment deser within the .bin file
                long position = 0;
                try
                {
                    position = fpireader.ReadLong();
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                XmlAttachment o = FindAttachmentBySerial(serialno.Value);

                if (o == null || imareader.Position != position)
                {
                    if (!AlreadyReported(valuetype))
                    {
                        Console.WriteLine("\nError deserializing attachments of type {0}.\n", valuetype);
                        ReportDeserError(valuetype, "save file corruption or incorrect Serialize/Deserialize methods?");
                    }

                    // position the .ima file at the next deser point
                    try
                    {
                        imareader.Seek(position, SeekOrigin.Begin);
                    }
                    catch
                    {
                        ErrorReporter.GenerateErrorReport(
                            "Error deserializing. Attachments save file corrupted. Attachment load aborted."
                      );
                        return;
                    }

                    continue;
                }

                // attachment successfully deserialized so attach it
                AttachTo(key, o, false);
            }
        }

        // read in the item attachment hash table information
        try
        {
            count = imareader.ReadInt();
        }
        catch (Exception e)
        {
            ErrorReporter.GenerateErrorReport(e.ToString());
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Item key = null;
            try
            {
                key = imareader.ReadEntity<Item>();
            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            int nattach = 0;
            try
            {
                nattach = imareader.ReadInt();
            }
            catch (Exception e)
            {
                ErrorReporter.GenerateErrorReport(e.ToString());
                return;
            }

            for (int j = 0; j < nattach; j++)
            {
                // and serial
                ASerial serialno = null;
                try
                {
                    serialno = new ASerial(imareader.ReadInt());
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                // read the attachment type
                string valuetype = null;
                try
                {
                    valuetype = imareader.ReadString();
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                // read the position of the beginning of the next attachment deser within the .bin file
                long position = 0;
                try
                {
                    position = fpireader.ReadLong();
                }
                catch (Exception e)
                {
                    ErrorReporter.GenerateErrorReport(e.ToString());
                    return;
                }

                XmlAttachment o = FindAttachmentBySerial(serialno.Value);

                if (o == null || imareader.Position != position)
                {
                    if (!AlreadyReported(valuetype))
                    {
                        Console.WriteLine("\nError deserializing attachments of type {0}.\n", valuetype);
                        ReportDeserError(valuetype, "save file corruption or incorrect Serialize/Deserialize methods?");
                    }

                    // position the .ima file at the next deser point
                    try
                    {
                        imareader.Seek(position, SeekOrigin.Begin);
                    }
                    catch
                    {
                        ErrorReporter.GenerateErrorReport(
                            "Error deserializing. Attachments save file corrupted. Attachment load aborted."
                      );
                        return;
                    }

                    continue;
                }

                // attachment successfully deserialized so attach it
                AttachTo(key, o, false);
            }
        }

        if (desererror != null)
        {
            ErrorReporter.GenerateErrorReport("Error deserializing particular attachments.");
        }
    }

    private class DeserErrorDetails
    {
        public string Type;
        public string Details;

        public DeserErrorDetails(string type, string details)
        {
            Type = type;
            Details = details;
        }

    }
    private static List<DeserErrorDetails> desererror;
    private static void ReportDeserError(string typestr, string detailstr)
    {
        desererror ??= new List<DeserErrorDetails>();

        desererror.Add(new DeserErrorDetails(typestr, detailstr));
    }
    private static bool AlreadyReported(string typestr)
    {
        if (desererror == null)
        {
            return false;
        }

        foreach (DeserErrorDetails s in desererror)
        {
            if (s.Type == typestr)
            {
                return true;
            }
        }
        return false;
    }

    public static void CheckOnBeforeKill(Mobile m_killed, Mobile m_killer)
    {

        // do not register creature vs creature kills, nor any kills involving staff
        //            if (m_killer == null || m_killed == null || !(m_killer.Player || m_killed.Player) /*|| (m_killer.AccessLevel > AccessLevel.Player) || (m_killed.AccessLevel > AccessLevel.Player) */)
        //				return;

        if (m_killer != null)
        {
            // check the killer
            List<XmlAttachment> alist = FindAttachments(m_killer);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.HandlesOnKill)
                    {
                        a.OnBeforeKill(m_killed, m_killer);
                    }
                }
            }

            // check any equipped items
            List<Item> equiplist = m_killer.Items;
            if (equiplist != null)
            {
                foreach (Item i in equiplist)
                {
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    alist = FindAttachments(i);
                    if (alist != null)
                    {
                        foreach (XmlAttachment a in alist)
                        {
                            if (a != null && !a.Deleted && a.CanActivateEquipped && a.HandlesOnKill)
                            {
                                a.OnBeforeKill(m_killed, m_killer);
                            }
                        }
                    }
                }
            }
        }

        if (m_killed != null)
        {
            // check the killed
            List<XmlAttachment> alist = FindAttachments(m_killed);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.HandlesOnKilled)
                    {
                        a.OnBeforeKilled(m_killed, m_killer);
                    }
                }
            }
        }
    }


    public static void CheckOnKill(Mobile m_killed, Mobile m_killer)
    {

        // do not register creature vs creature kills, nor any kills involving staff
        //            if (m_killer == null || m_killed == null || !(m_killer.Player || m_killed.Player) /*|| (m_killer.AccessLevel > AccessLevel.Player) || (m_killed.AccessLevel > AccessLevel.Player) */)
        //				return;

        if (m_killer != null)
        {
            // check the killer
            List<XmlAttachment> alist = FindAttachments(m_killer);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.HandlesOnKill)
                    {
                        a.OnKill(m_killed, m_killer);
                    }
                }
            }

            // check any equipped items
            List<Item> equiplist = m_killer.Items;
            if (equiplist != null)
            {
                foreach (Item i in equiplist)
                {
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    alist = FindAttachments(i);
                    if (alist != null)
                    {
                        foreach (XmlAttachment a in alist)
                        {
                            if (a != null && !a.Deleted && a.CanActivateEquipped && a.HandlesOnKill)
                            {
                                a.OnKill(m_killed, m_killer);
                            }
                        }
                    }
                }
            }
        }

        if (m_killed != null)
        {
            // check the killed
            List<XmlAttachment> alist = FindAttachments(m_killed);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.HandlesOnKilled)
                    {
                        a.OnKilled(m_killed, m_killer);
                    }
                }
            }
        }
    }

    public static void EventSink_Movement(MovementEventArgs args)
    {
        Mobile from = args.Mobile;

        if (!from.Player /* || from.AccessLevel > AccessLevel.Player */)
        {
            return;
        }

        // check for any items in the same sector
        if (from.Map != null)
        {
            using var queue = PooledRefQueue<XmlAttachment>.Create();
            foreach (var i in from.Map.GetItemsInRange(from.Location, Map.SectorSize))
            {
                if (i == null || i.Deleted)
                {
                    continue;
                }

                List<XmlAttachment> alist = FindAttachments(i);
                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && a.HandlesOnMovement)
                        {
                            queue.Enqueue(a);
                        }
                    }
                }
            }

            // check for mobiles
            foreach (var m in from.Map.GetMobilesInRange(from.Location, Map.SectorSize))
            {
                // dont respond to self motion
                if (m == from)
                {
                    continue;
                }

                List<XmlAttachment> alist = FindAttachments(m);
                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && a.HandlesOnMovement)
                        {
                            queue.Enqueue(a);
                        }
                    }
                }
            }

            while (queue.Count > 0)
            {
                queue.Dequeue().OnMovement(args);
            }
        }
    }

    public static void EventSink_Speech(SpeechEventArgs args)
    {
        Mobile from = args.Mobile;

        if (from == null || from.Map == null /*|| from.AccessLevel > AccessLevel.Player */)
        {
            return;
        }

        // check the mob for any attachments that might handle speech
        List<XmlAttachment> alist = FindAttachments(from);
        if (alist != null)
        {
            foreach (XmlAttachment a in alist)
            {
                if (a != null && !a.Deleted && a.HandlesOnSpeech)
                {
                    a.OnSpeech(args);
                }
            }
        }

        // check for any nearby items
        using var queue = PooledRefQueue<XmlAttachment>.Create();
        foreach (Item i in from.Map.GetItemsInRange(from.Location, Map.SectorSize))
        {
            alist = FindAttachments(i);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.CanActivateInWorld && a.HandlesOnSpeech)
                    {
                        queue.Enqueue(a);
                    }
                }
            }
        }

        // check for any nearby mobs
        foreach (Mobile i in from.Map.GetMobilesInRange(from.Location, Map.SectorSize))
        {
            if (i == null || i.Deleted)
            {
                continue;
            }

            alist = FindAttachments(i);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.HandlesOnSpeech)
                    {
                        queue.Enqueue(a);
                    }
                }
            }
        }

        // also check for any items in the mobs toplevel backpack
        if (from.Backpack != null)
        {
            foreach (Item i in from.Backpack.Items)
            {
                alist = FindAttachments(i);
                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && a.CanActivateInBackpack && a.HandlesOnSpeech)
                        {
                            queue.Enqueue(a);
                        }
                    }
                }
            }
        }

        // check any equipped items
        foreach (Item i in from.Items)
        {
            if (i == null || i.Deleted)
            {
                continue;
            }

            alist = FindAttachments(i);
            if (alist != null)
            {
                foreach (XmlAttachment a in alist)
                {
                    if (a != null && !a.Deleted && a.CanActivateEquipped && a.HandlesOnSpeech)
                    {
                        queue.Enqueue(a);
                    }
                }
            }
        }

        while (queue.Count > 0)
        {
            queue.Dequeue().OnSpeech(args);
        }
    }

    public static XmlAttachment FindAttachmentOnMobile(Mobile from, Type type, string name)
    {
        if (from == null)
        {
            return null;
        }

        // check the mob for any attachments
        List<XmlAttachment> alist = FindAttachments(from);
        if (alist != null)
        {
            foreach (XmlAttachment a in alist)
            {
                if (a != null && !a.Deleted && (type == null || a.GetType() == type || a.GetType().IsSubclassOf(type)) && (name == null || name == a.Name))
                {
                    return a;
                }
            }
        }


        // also check for any items in the mobs toplevel backpack
        if (from.Backpack != null)
        {
            List<Item> itemlist = from.Backpack.Items;
            if (itemlist != null)
            {
                foreach (Item i in itemlist)
                {
                    if (i == null || i.Deleted)
                    {
                        continue;
                    }

                    alist = FindAttachments(i);
                    if (alist != null)
                    {
                        foreach (XmlAttachment a in alist)
                        {
                            if (a != null && !a.Deleted && (type == null || a.GetType() == type || a.GetType().IsSubclassOf(type)) && (name == null || name == a.Name))
                            {
                                return a;
                            }
                        }
                    }
                }
            }
        }

        // check any equipped items
        List<Item> equiplist = from.Items;
        if (equiplist != null)
        {
            foreach (Item i in equiplist)
            {
                if (i == null || i.Deleted)
                {
                    continue;
                }

                alist = FindAttachments(i);

                if (alist != null)
                {
                    foreach (XmlAttachment a in alist)
                    {
                        if (a != null && !a.Deleted && (type == null || a.GetType() == type || a.GetType().IsSubclassOf(type)) && (name == null || name == a.Name))
                        {
                            return a;
                        }
                    }
                }
            }
        }
        return null;
    }

    private class AttachTarget : Target
    {
        private CommandEventArgs m_e;
        private string m_set;

        public AttachTarget(CommandEventArgs e, string set)
            : base(30, false, TargetFlags.None)
        {
            m_e = e;
            m_set = set;
        }
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from == null || targeted == null)
            {
                return;
            }

            Type type = null;
            string name = null;

            if (m_e.Arguments.Length > 0)
            {

                type = AssemblyHandler.FindTypeByName(m_e.Arguments[0]);

            }
            if (m_e.Arguments.Length > 1)
            {
                name = m_e.Arguments[1];
            }

            Defrag(targeted);

            List<XmlAttachment> plist = FindAttachments(targeted, type);

            if (plist == null && m_set != "add")
            {
                from.SendMessage("No attachments");
                return;
            }

            switch (m_set)
            {
                case "add":
                    {
                        if (m_e.Arguments.Length < 1)
                        {
                            from.SendMessage("Must specify an attachment type.");
                            return;
                        }

                        // create a new attachment and add it to the item
                        int nargs = m_e.Arguments.Length - 1;

                        string[] args = new string[nargs];

                        for (int j = 0; j < nargs; j++)
                        {
                            args[j] = m_e.Arguments[j + 1];
                        }


                        XmlAttachment o = null;

                        Type attachtype = AssemblyHandler.FindTypeByName(m_e.Arguments[0]);

                        if (attachtype != null && attachtype.IsSubclassOf(typeof(XmlAttachment)))
                        {

                            o = (XmlAttachment)XmlSpawner.CreateObject(attachtype, args, false);
                        }

                        if (o != null)
                        {
                            //o.Name = aname;
                            if (AttachTo(from, targeted, o, true))
                            {
                                from.SendMessage($"Added attachment {o.Serial.Value} : {m_e.Arguments[0]} to {targeted}");
                            }
                            else
                            {
                                from.SendMessage($"Attachment not added: {m_e.Arguments[0]}");
                            }
                        }
                        else
                        {
                            from.SendMessage($"Unable to construct attachment {m_e.Arguments[0]}");
                        }

                        break;
                    }

                case "get":
                    {
                        /*
                        foreach(XmlAttachment p in plist)
                        {
                            if (p == null || p.Deleted || (name != null && name != p.Name) || (type != null && type != p.GetType())) continue;

                            from.SendMessage("Found attachment {3} : {0} : {1} : {2}",p.GetType().Name,p.Name,p.OnIdentify(from), p.Serial.Value);

                        }
                        */
                        from.SendGump(new XmlGetAttGump(from, targeted, 0, 0));

                        break;
                    }
                case "delete":
                    {
                        /*
                        foreach(XmlAttachment p in plist)
                        {
                            if (p == null || p.Deleted || (name != null && name != p.Name) || (type != null && type != p.GetType())) continue;

                            from.SendMessage("Deleting attachment {3} : {0} : {1} : {2}",p.GetType().Name,p.Name,p.OnIdentify(from), p.Serial.Value);
                            p.Delete();
                        }
                        */
                        from.SendGump(new XmlGetAttGump(from, targeted, 0, 0));

                        break;
                    }
                case "activate":
                    {
                        foreach (XmlAttachment p in plist)
                        {
                            if (p == null || p.Deleted || name != null && name != p.Name || type != null && type != p.GetType())
                            {
                                continue;
                            }

                            from.SendMessage($"Activating attachment {p.Serial.Value} : {p.GetType().Name} : {p.Name} : {p.OnIdentify(from)}");
                            p.OnTrigger(null, from);
                        }

                        break;
                    }
            }
        }
    }

    [Usage("GetAtt [type/serialno [name]]")]
    [Description("Returns descriptions of the attachments on the targeted object.")]
    public static void GetAttachments_OnCommand(CommandEventArgs e)
    {
        int ser = -1;
        if (e.Arguments.Length > 0)
        {
            // is this a numeric arg?
            char c = e.Arguments[0][0];
            if (c >= '0' && c <= '9')
            {
                try
                {
                    ser = int.Parse(e.Arguments[0]);
                }
                catch { }
                XmlAttachment a = FindAttachmentBySerial(ser);
                if (a != null)
                {
                    // open up the props gump on the attachment
                    e.Mobile.SendGump(new PropertiesGump(e.Mobile, a));

                }
                else
                {
                    e.Mobile.SendMessage($"Attachment {ser} does not exist");
                }
            }
        }

        if (ser == -1)
        {
            e.Mobile.Target = new AttachTarget(e, "get");
        }
    }

    [Usage("AddAtt type [args]")]
    [Description("Adds an attachment to the targeted object.")]
    public static void AddAttachment_OnCommand(CommandEventArgs e)
    {
        e.Mobile.Target = new AttachTarget(e, "add");
    }

    [Usage("DelAtt [type/serialno [name]]")]
    [Description("Deletes attachments on the targeted object.")]
    public static void DeleteAttachments_OnCommand(CommandEventArgs e)
    {
        int ser = -1;
        if (e.Arguments.Length > 0)
        {
            // is this a numeric arg?
            char c = e.Arguments[0][0];
            if (c >= '0' && c <= '9')
            {
                try
                {
                    ser = int.Parse(e.Arguments[0]);
                }
                catch { }
                XmlAttachment a = FindAttachmentBySerial(ser);
                if (a != null)
                {
                    e.Mobile.SendMessage($"Deleting attachment {ser} : {a}");
                    a.Delete();
                }
                else
                {
                    e.Mobile.SendMessage($"Attachment {ser} does not exist");
                }
            }
        }

        if (ser == -1)
        {
            e.Mobile.Target = new AttachTarget(e, "delete");
        }
    }

    [Usage("TrigAtt [type [name]]")]
    [Description("Triggers attachments on the targeted object.")]
    public static void ActivateAttachments_OnCommand(CommandEventArgs e)
    {
        e.Mobile.Target = new AttachTarget(e, "activate");
    }

    [Usage("ItemAtt")]
    [Description("Lists all item attachments.")]
    public static void ListItemAttachments_OnCommand(CommandEventArgs e)
    {
        if (ItemAttachments == null)
        {
            return;
        }

        FullDefrag(ItemAttachments);

        Item[] itemarray = new Item[ItemAttachments.Count];

        ItemAttachments.Keys.CopyTo(itemarray, 0);

        e.Mobile.SendMessage($"{ItemAttachments.Count} items with attachments");

        for (int i = 0; i < itemarray.Length; i++)
        {
            e.Mobile.SendMessage($"Attachments for {itemarray[i]} :");
            List<XmlAttachment> list = FindAttachments(itemarray[i]);

            if (list != null)
            {
                foreach (XmlAttachment a in list)
                {
                    if (a != null && !a.Deleted)
                    {
                        e.Mobile.SendMessage($"\t{a.GetType().Name} : {a.Name} : {a.OnIdentify(e.Mobile)}");
                    }
                }
            }
        }
    }
    [Usage("MobAtt")]
    [Description("Lists all mobile attachments.")]
    public static void ListMobileAttachments_OnCommand(CommandEventArgs e)
    {
        if (MobileAttachments == null)
        {
            return;
        }

        FullDefrag(MobileAttachments);

        Mobile[] mobilearray = new Mobile[MobileAttachments.Count];

        MobileAttachments.Keys.CopyTo(mobilearray, 0);

        e.Mobile.SendMessage($"{MobileAttachments.Count} mobiles with attachments");

        for (int i = 0; i < mobilearray.Length; i++)
        {
            e.Mobile.SendMessage($"Attachments for {mobilearray[i]} :");
            List<XmlAttachment> list = FindAttachments(mobilearray[i]);

            if (list != null)
            {
                foreach (XmlAttachment a in list)
                {
                    if (a != null && !a.Deleted)
                    {
                        e.Mobile.SendMessage($"\t{a.GetType().Name} : {a.Name} : {a.OnIdentify(e.Mobile)}");
                    }
                }
            }
        }
    }

    private static void Match(Type matchtype, Type[] types, List<Type> results)
    {
        if (matchtype == null)
        {
            return;
        }

        for (int i = 0; i < types.Length; ++i)
        {
            Type t = types[i];

            if (t.IsSubclassOf(matchtype))
            {
                results.Add(t);
            }
        }
    }


    private static List<Type> Match(Type matchtype)
    {
        List<Type> results = new List<Type>();
        Type[] types;

        Assembly[] asms = AssemblyHandler.Assemblies;

        for (int i = 0; i < asms.Length; ++i)
        {
            types = AssemblyHandler.GetTypeCache(asms[i]).Types;
            Match(matchtype, types, results);
        }

        types = AssemblyHandler.GetTypeCache(Core.Assembly).Types;
        Match(matchtype, types, results);

        results.Sort((a, b) => a.Name.CompareTo(b.Name));

        return results;
    }


    [Usage("AvailAtt")]
    [Description("Lists all available attachments.")]
    public static void ListAvailableAttachments_OnCommand(CommandEventArgs e)
    {

        List<Type> attachtypes = Match(typeof(XmlAttachment));

        string parmliststr = null;

        foreach (Type attachtype in attachtypes)
        {
            // get all constructors derived from the XmlAttachment class
            ConstructorInfo[] ctors = attachtype.GetConstructors();

            for (int i = 0; i < ctors.Length; ++i)
            {
                ConstructorInfo ctor = ctors[i];

                if (!IsAttachable(ctor))
                {
                    continue;
                }

                ParameterInfo[] paramList = ctor.GetParameters();

                if (paramList != null)
                {
                    string parms = attachtype.Name;


                    for (int j = 0; j < paramList.Length; j++)
                    {
                        parms += $", {paramList[j].Name}";
                    }

                    parmliststr += $"{parms}\n";
                }
            }
        }
        e.Mobile.SendGump(new ListAttachmentsGump(parmliststr, 20, 20));

    }

    private class ListAttachmentsGump : Gump
    {

        public ListAttachmentsGump(string attachmentlist, int X, int Y)
            : base(X, Y)
        {
            AddPage(0);

            AddBackground(20, 0, 330, 480, 5054);

            AddPage(1);

            AddImageTiled(20, 0, 330, 480, 0x52);

            AddLabel(27, 2, 0x384, "Available Attachments");
            AddHtml(25, 22, 320, 458, attachmentlist, false, true);
        }
    }

    private class DisplayAttachmentGump : Gump
    {
        public DisplayAttachmentGump(Mobile from, string text, int X, int Y)
            : base(X, Y)
        {
            // prepare the page
            AddPage(0);

            AddBackground(0, 0, 400, 150, 5054);
            AddAlphaRegion(0, 0, 400, 150);
            AddLabel(20, 2, 55, "Attachment Description(s)");

            AddHtml(20, 20, 360, 110, text, true, true);
        }
    }

    public static void RevealAttachments(Mobile from, object o)
    {
        if (from == null || o == null)
        {
            return;
        }

        List<XmlAttachment> plist = FindAttachments(o);

        if (plist == null)
        {
            return;
        }

        string msg = null;

        foreach (XmlAttachment p in plist)
        {
            if (p != null && !p.Deleted)
            {
                string pmsg = p.OnIdentify(from);
                if (pmsg != null)
                {
                    msg += $"\n{pmsg}\n";
                }
            }
        }
        if (msg != null)
        {
            from.CloseGump<DisplayAttachmentGump>();
            from.SendMessage("Hidden attributes revealed!");

            from.SendGump(new DisplayAttachmentGump(from, msg, 0, 0));
        }
    }

    public static bool AttachTo(object o, XmlAttachment attachment) => AttachTo(null, o, attachment, true);

    public static bool AttachTo(object from, object o, XmlAttachment attachment) => AttachTo(from, o, attachment, true);

    public static bool AttachTo(object o, XmlAttachment attachment, bool first) => AttachTo(null, o, attachment, first);

    private static bool AttachTo(object from, object o, XmlAttachment attachment, bool first)
    {
        if (attachment == null)
        {
            return false;
        }

        Defrag(o);

        List<XmlAttachment> attachmententry = null;

        if (o is Item item)
        {
            ItemAttachments ??= new Dictionary<Item, List<XmlAttachment>>();

            // see if there is already an attachment list for the object
            if (ItemAttachments.TryGetValue(item, out var existingList))
            {
                attachmententry = existingList;
            }
            else
            {
                // otherwise make a new entry list
                attachmententry = new List<XmlAttachment>(1);
                ItemAttachments.Add(item, attachmententry);
            }
        }
        else if (o is Mobile mobile)
        {
            MobileAttachments ??= new Dictionary<Mobile, List<XmlAttachment>>();

            // see if there is already an attachment list for the object
            if (MobileAttachments.TryGetValue(mobile, out var existingList))
            {
                attachmententry = existingList;
            }
            else
            {
                // otherwise make a new entry list
                attachmententry = new List<XmlAttachment>(1);
                MobileAttachments.Add(mobile, attachmententry);
            }
        }
        else
        {
            return false;
        }

        // check for duplicates and remove them
        foreach (XmlAttachment i in attachmententry)
        {
            // an attachment is considered a duplicate if both the type and name match
            if (i != null && !i.Deleted && i.GetType() == attachment.GetType() && i.Name == attachment.Name)
            {
                // duplicate found so replace it
                i.Delete();
            }
        }

        attachmententry.Add(attachment);

        attachment.AttachedTo = o;
        attachment.OwnedBy = o;

        if (from is Mobile m)
        {
            attachment.SetAttachedBy(m.Name);
        }
        else if (from is Item i)
        {
            attachment.SetAttachedBy(i.Name);
        }

        // if this is being attached for the first time, then call the OnAttach method
        // if it is being reattached due to deserialization then dont
        if (first)
        {
            attachment.OnAttach();
        }
        else
        {
            attachment.OnReattach();
        }

        return !attachment.Deleted;
    }

    public static List<XmlAttachment> FindAttachments(object o) => FindAttachments(o, null, null);

    public static List<XmlAttachment> FindAttachments(object o, Type type) => FindAttachments(o, type, null);

    public static List<XmlAttachment> FindAttachments(object o, Type type, string name)
    {
        if (o == null)
        {
            return null;
        }

        List<XmlAttachment> list = null;

        if (o is Item item)
        {
            if (item.Deleted)
            {
                return null;
            }
            ItemAttachments?.TryGetValue(item, out list);
        }
        else if (o is Mobile mobile)
        {
            if (mobile.Deleted)
            {
                return null;
            }
            MobileAttachments?.TryGetValue(mobile, out list);
        }

        return FilterAttachments(list, type, name);
    }

    // Internal helper to get original list (for internal use like AttachTo)
    private static List<XmlAttachment> FindAttachmentsOriginal(object o)
    {
        if (o is Item item && !item.Deleted && ItemAttachments != null)
        {
            ItemAttachments.TryGetValue(item, out var list);
            return list;
        }

        if (o is Mobile mobile && !mobile.Deleted && MobileAttachments != null)
        {
            MobileAttachments.TryGetValue(mobile, out var list);
            return list;
        }

        return null;
    }

    private static List<XmlAttachment> FilterAttachments(List<XmlAttachment> list, Type type, string name)
    {
        if (list == null)
        {
            return null;
        }

        // If no filter, return a copy
        if (type == null && name == null)
        {
            return new List<XmlAttachment>(list);
        }

        // Filter by type and/or name
        List<XmlAttachment> newlist = new List<XmlAttachment>();

        foreach (XmlAttachment i in list)
        {
            if (i == null || i.Deleted)
            {
                continue;
            }

            Type itype = i.GetType();

            if ((type == null || itype != null && (itype == type || itype.IsSubclassOf(type))) && (name == null || name == i.Name))
            {
                newlist.Add(i);
            }
        }

        return newlist;
    }

    public static XmlAttachment FindAttachment(object o) => FindAttachment(o, null, null);

    public static XmlAttachment FindAttachment(object o, Type type) => FindAttachment(o, type, null);

    public static XmlAttachment FindAttachment(object o, Type type, string name)
    {
        if (o == null)
        {
            return null;
        }

        List<XmlAttachment> list = null;

        if (o is Item item)
        {
            if (item.Deleted)
            {
                return null;
            }
            ItemAttachments?.TryGetValue(item, out list);
        }
        else if (o is Mobile mobile)
        {
            if (mobile.Deleted)
            {
                return null;
            }
            MobileAttachments?.TryGetValue(mobile, out list);
        }

        if (list == null || list.Count == 0)
        {
            return null;
        }

        if (type == null && name == null)
        {
            // return the first valid attachment
            foreach (XmlAttachment i in list)
            {
                if (i != null && !i.Deleted)
                {
                    return i;
                }
            }
        }
        else
        {
            // find one of a particular type and/or name
            foreach (XmlAttachment i in list)
            {
                if (i == null || i.Deleted)
                {
                    continue;
                }

                Type itype = i.GetType();

                if ((type == null || itype != null && (itype == type || itype.IsSubclassOf(type))) && (name == null || name == i.Name))
                {
                    return i;
                }
            }
        }
        return null;
    }

    public static XmlAttachment FindAttachmentBySerial(int serialno)
    {
        if (serialno <= 0)
        {
            return null;
        }

        AllAttachments.TryGetValue(serialno, out var attachment);
        return attachment;
    }


    private static void FullDefrag()
    {
        // defrag the item attachments
        FullDefrag(ItemAttachments);

        // defrag the mobile attachments
        FullDefrag(MobileAttachments);

        // defrag the serial table
        FullSerialDefrag();
    }

    private static void FullDefrag(Dictionary<Item, List<XmlAttachment>> attachments)
    {
        if (attachments == null)
        {
            return;
        }

        Item[] items = new Item[attachments.Count];
        attachments.Keys.CopyTo(items, 0);
        foreach (Item item in items)
        {
            DefragItem(item);
        }
    }

    private static void FullDefrag(Dictionary<Mobile, List<XmlAttachment>> attachments)
    {
        if (attachments == null)
        {
            return;
        }

        Mobile[] mobiles = new Mobile[attachments.Count];
        attachments.Keys.CopyTo(mobiles, 0);
        foreach (Mobile mobile in mobiles)
        {
            DefragMobile(mobile);
        }
    }

    private static void FullSerialDefrag()
    {
        if (AllAttachments == null)
        {
            return;
        }

        int[] keys = new int[AllAttachments.Count];
        AllAttachments.Keys.CopyTo(keys, 0);

        foreach (int key in keys)
        {
            if (AllAttachments.TryGetValue(key, out var attachment))
            {
                if (attachment == null || attachment.Deleted)
                {
                    AllAttachments.Remove(key);
                }
            }
        }
    }

    private static void SerialDefrag(XmlAttachment a)
    {
        if (a != null && a.Deleted)
        {
            AllAttachments.Remove(a.Serial.Value);
        }
    }

    public static void Defrag(object o)
    {
        if (o is Item item)
        {
            DefragItem(item);
        }
        else if (o is Mobile mobile)
        {
            DefragMobile(mobile);
        }
    }

    private static void DefragItem(Item item)
    {
        if (item == null || ItemAttachments == null)
        {
            return;
        }

        bool removeall = item.Deleted;

        if (!ItemAttachments.TryGetValue(item, out var list))
        {
            return;
        }

        if (list == null)
        {
            ItemAttachments.Remove(item);
            return;
        }

        List<XmlAttachment> defraglist = null;

        foreach (XmlAttachment i in list)
        {
            if (i == null || i.Deleted || removeall)
            {
                defraglist ??= new List<XmlAttachment>();
                defraglist.Add(i);
            }
        }

        if (defraglist != null)
        {
            foreach (XmlAttachment i in defraglist)
            {
                list.Remove(i);
            }
            if (list.Count == 0 || removeall)
            {
                ItemAttachments.Remove(item);
            }
        }
    }

    private static void DefragMobile(Mobile mobile)
    {
        if (mobile == null || MobileAttachments == null)
        {
            return;
        }

        bool removeall = mobile.Deleted;

        if (!MobileAttachments.TryGetValue(mobile, out var list))
        {
            return;
        }

        if (list == null)
        {
            MobileAttachments.Remove(mobile);
            return;
        }

        List<XmlAttachment> defraglist = null;

        foreach (XmlAttachment i in list)
        {
            if (i == null || i.Deleted || removeall)
            {
                defraglist ??= new List<XmlAttachment>();
                defraglist.Add(i);
            }
        }

        if (defraglist != null)
        {
            foreach (XmlAttachment i in defraglist)
            {
                list.Remove(i);
            }
            if (list.Count == 0 || removeall)
            {
                MobileAttachments.Remove(mobile);
            }
        }
    }

    public static bool CheckCanEquip(Item item, Mobile from)
    {
        // call the CanEquip method on any attachments on the item
        // look for attachments on the item
        List<XmlAttachment> attachments = FindAttachments(item);

        if (attachments != null)
        {
            foreach (XmlAttachment a in attachments)
            {
                if (a != null && !a.Deleted)
                {
                    if (!a.CanEquip(from))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    public static void CheckOnEquip(Item item, Mobile from)
    {
        // look for attachments on the item
        List<XmlAttachment> attachments = FindAttachments(item);

        if (attachments != null)
        {
            foreach (XmlAttachment a in attachments)
            {
                if (a != null && !a.Deleted)
                {
                    a.OnEquip(from);
                }
            }
        }
    }

    public static void CheckOnRemoved(Item item, object parent)
    {
        // look for attachments on the item
        List<XmlAttachment> attachments = FindAttachments(item);

        if (attachments != null)
        {
            foreach (XmlAttachment a in attachments)
            {
                if (a != null && !a.Deleted)
                {
                    a.OnRemoved(parent);
                }
            }
        }
    }

    public static void OnWeaponHit(BaseWeapon weapon, Mobile attacker, Mobile defender, int damage)
    {
        // look for attachments on the weapon
        List<XmlAttachment> attachments = FindAttachments(weapon);

        if (attachments != null)
        {
            foreach (XmlAttachment a in attachments)
            {
                if (a != null && !a.Deleted)
                {
                    a.OnWeaponHit(attacker, defender, weapon, damage);
                }
            }
        }

        // also support OnWeaponHit for the mobile owner
        attachments = FindAttachments(attacker);

        if (attachments != null)
        {
            foreach (XmlAttachment a in attachments)
            {
                if (a != null && !a.Deleted)
                {
                    a.OnWeaponHit(attacker, defender, weapon, damage);
                }
            }
        }
    }

    public static int OnArmorHit(Mobile attacker, Mobile defender, Item armor, BaseWeapon weapon, int damage)
    {
        int damageTaken = 0;

        // figure out who the attacker and defender are based upon who is carrying the armor/weapon

        // look for attachments on the armor
        if (armor != null)
        {
            List<XmlAttachment> attachments = FindAttachments(armor);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted)
                    {
                        damageTaken += a.OnArmorHit(attacker, defender, armor, weapon, damage);
                    }
                }
            }
        }

        return damageTaken;
    }

    public static void AddAttachmentProperties(object parent, IPropertyList list)
    {
        if (parent == null)
        {
            return;
        }

        string propstr = null;

        List<XmlAttachment> plist = FindAttachments(parent);
        if (plist != null && plist.Count > 0)
        {
            for (int i = 0; i < plist.Count; i++)
            {
                XmlAttachment a = plist[i];

                if (a != null && !a.Deleted)
                {
                    // give the attachment an opportunity to modify the properties list of the parent
                    a.AddProperties(list);

                    // get any displayed properties on the attachment
                    string str = a.DisplayedProperties(null);

                    if (str != null)
                    {
                        propstr += str;

                        if (i < plist.Count - 1)
                        {
                            propstr += "\n";
                        }
                    }

                }
            }
        }

        if (propstr != null && list != null)
        {
            list.Add(1062613, propstr);
        }
    }

    public static void UseReq(NetState state, SpanReader reader)
    {
        Mobile from = state.Mobile;

        if (from.AccessLevel >= AccessLevel.GameMaster || Core.TickCount - from.NextActionTime >= 0)
        {
            var value = reader.ReadUInt32();

            if ((value & ~0x7FFFFFFF) != 0)
            {
                from.OnPaperdollRequest();
            }
            else
            {
                Serial s = (Serial)value;

                bool blockdefaultonuse = false;

                if (s.IsMobile)
                {
                    Mobile m = World.FindMobile(s);

                    if (m != null && !m.Deleted)
                    {
                        // get attachments on the mobile doing the using
                        List<XmlAttachment> fromlist = FindAttachments(from);
                        if (fromlist != null)
                        {
                            foreach (XmlAttachment a in fromlist)
                            {
                                if (a != null && !a.Deleted)
                                {
                                    if (a.BlockDefaultOnUse(from, m))
                                    {
                                        blockdefaultonuse = true;
                                    }

                                    a.OnUser(m);
                                }
                            }
                        }

                        // get attachments on the mob
                        List<XmlAttachment> alist = FindAttachments(m);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted)
                                {
                                    if (a.BlockDefaultOnUse(from, m))
                                    {
                                        blockdefaultonuse = true;
                                    }

                                    a.OnUse(from);
                                }
                            }
                        }

                        if (!blockdefaultonuse && !m.Deleted)
                        {
                            from.Use(m);
                        }
                    }
                }
                else if (s.IsItem)
                {
                    Item item = World.FindItem(s);

                    if (item != null && !item.Deleted)
                    {
                        // get attachments on the mobile doing the using
                        List<XmlAttachment> fromlist = FindAttachments(from);
                        if (fromlist != null)
                        {
                            foreach (XmlAttachment a in fromlist)
                            {
                                if (a != null && !a.Deleted)
                                {
                                    if (a.BlockDefaultOnUse(from, item))
                                    {
                                        blockdefaultonuse = true;
                                    }

                                    a.OnUser(item);
                                }
                            }
                        }

                        // get attachments on the mob
                        List<XmlAttachment> alist = FindAttachments(item);
                        if (alist != null)
                        {
                            foreach (XmlAttachment a in alist)
                            {
                                if (a != null && !a.Deleted)
                                {
                                    if (a.BlockDefaultOnUse(from, item))
                                    {
                                        blockdefaultonuse = true;
                                    }

                                    a.OnUse(from);
                                }
                            }
                        }
                        // need to check the item again in case it was modified in the OnUse or OnUser method
                        if (!blockdefaultonuse && !item.Deleted)
                        {
                            from.Use(item);
                        }
                    }
                }
            }

        }
        else
        {
            from.SendActionMessage();
        }

    }

    public static bool OnDragLift(Mobile from, Item item)
    {
        // look for attachments on the item
        if (item != null)
        {
            List<XmlAttachment> attachments = FindAttachments(item);

            if (attachments != null)
            {
                foreach (XmlAttachment a in attachments)
                {
                    if (a != null && !a.Deleted && !a.OnDragLift(from, item))
                    {
                        return false;
                    }
                }
            }
        }

        // allow lifts by default
        return true;
    }

    public class ErrorReporter
    {
        private static string GetRoot()
        {
            try
            {
                return Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            }
            catch
            {
                return "";
            }
        }

        private static string Combine(string path1, string path2)
        {
            if (path1 == "")
            {
                return path2;
            }

            return Path.Combine(path1, path2);
        }


        private static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void CreateDirectory(string path1, string path2)
        {
            CreateDirectory(Combine(path1, path2));
        }

        private static void CopyFile(string rootOrigin, string rootBackup, string path)
        {
            string originPath = Combine(rootOrigin, path);
            string backupPath = Combine(rootBackup, path);

            try
            {
                if (File.Exists(originPath))
                {
                    File.Copy(originPath, backupPath);
                }
            }
            catch
            {
            }
        }

        public static void GenerateErrorReport(string error)
        {
            Console.Write("\nXmlSpawner2 Attachment Error:\n{0}\nGenerating report...", error);

            try
            {
                string timeStamp = GetTimeStamp();
                string fileName = $"Attachment Error {timeStamp}.log";

                string root = GetRoot();
                string filePath = Combine(root, fileName);

                using (StreamWriter op = new StreamWriter(filePath))
                {
                    Version ver = Core.Assembly.GetName().Version!;

                    op.WriteLine("XmlSpawner2 Attachment Error Report");
                    op.WriteLine("===================");
                    op.WriteLine();
                    op.WriteLine("ModernUO Version {0}.{1}.{3}, Build {2}", ver.Major, ver.Minor, ver.Revision, ver.Build);
                    op.WriteLine("Operating System: {0}", Environment.OSVersion);
                    op.WriteLine(".NET Framework: {0}", Environment.Version);
                    op.WriteLine("XmlSpawner2: {0}", XmlSpawner.Version);
                    op.WriteLine("Time: {0}", DateTime.Now);

                    op.WriteLine();

                    op.WriteLine("Error:");
                    op.WriteLine(error);

                    op.WriteLine();
                    op.WriteLine("Specific Attachment Errors:");
                    foreach (DeserErrorDetails s in desererror)
                    {
                        op.WriteLine("{0} - {1}", s.Type, s.Details);
                    }
                }

                Console.WriteLine("done");
                Email.SendCrashEmail(filePath);
            }
            catch
            {
                Console.WriteLine("failed");
            }
        }

        private static string GetTimeStamp()
        {
            DateTime now = DateTime.Now;

            return $"{now.Day}-{now.Month}-{now.Year}-{now.Hour}-{now.Minute}-{now.Second}";
        }
    }
}
