using System;
using System.Buffers;
using System.Text;
using Server.Network;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Lives in UOContent (not Server core, alongside OutgoingMahaonPackets.cs) because
///     it depends on MahaonSkillTree.SkillCategory — a UOContent type. Server core can't
///     reference UOContent (wrong build-order direction), so anything touching a
///     UOContent type has to live here even though it's still "sending packet 0xF9".
///     Same packet ID/subtype byte values as OutgoingMahaonPackets (duplicated as local
///     consts here rather than shared — they're just byte literals).
/// </summary>
public static class MahaonSkillTreePackets
{
    private const byte MahaonMultiPacketId = 0xF9;
    private const byte SubtypeSkillTree = 2;

    // subtype 2 — full skill tree sync. Nested wire format so future categories/
    // subcategories need zero protocol changes:
    //   [subtype:1][categoryCount:1]
    //   per category: [nameLen:1][nameUTF8][subCategoryCount:1]
    //     per subCategory: [nameLen:1][nameUTF8][isSelectable:1][categoryId:1][selectedId:1][isUsableSkillList:1][entryCount:1]
    //       per entry: [entryId:1][nameLen:1][nameUTF8][value:ushort BE, *10 scaled][cap:ushort BE, *10 scaled][descLen:ushort BE][descUTF8]
    // descLen is a ushort (not the byte length names use) — a real player-facing
    // description sentence in Cyrillic (multi-byte UTF8) can easily run past 255 bytes,
    // unlike the short name/label strings.
    public static void SendSkillTree(this NetState ns, MahaonSkillTree.SkillCategory[] categories)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        // Starting-capacity estimate only — NOT a hard cap. A flat worst-case-per-entry
        // guess here previously crashed (System.InvalidOperationException: Buffer is full
        // and resizing is disabled) the moment a real description sentence (Cyrillic runs
        // ~2 bytes/char — a couple of real sentences of style/specialization flavor text
        // easily clears 700+ bytes on its own) exceeded whatever flat number seemed
        // "generous" at the time. resize: true below means this number only affects how
        // many times the buffer has to grow, never correctness — get it wrong and the
        // packet still sends fine, just with one extra reallocation.
        var maxLength = 8;

        foreach (var category in categories)
        {
            maxLength += 90;

            foreach (var sub in category.SubCategories)
            {
                maxLength += 90 + sub.Entries.Length * 350;
            }
        }

        // Not `using` — WriteShortUtf8/WriteLongUtf8 below take the writer by ref, which
        // the compiler disallows for a using-declared local (CS1657).
        var writer = new SpanWriter(stackalloc byte[maxLength], resize: true);

        writer.Write(MahaonMultiPacketId);
        writer.Write((ushort)0); // length placeholder — overwritten by WritePacketLength()
        writer.Write(SubtypeSkillTree);
        writer.Write((byte)categories.Length);

        foreach (var category in categories)
        {
            WriteShortUtf8(ref writer, category.Name);
            writer.Write((byte)category.SubCategories.Length);

            foreach (var sub in category.SubCategories)
            {
                WriteShortUtf8(ref writer, sub.Name);
                writer.Write((byte)(sub.IsSelectable ? 1 : 0));
                writer.Write(sub.CategoryId);
                writer.Write(sub.SelectedId);
                writer.Write((byte)(sub.IsUsableSkillList ? 1 : 0));
                writer.Write((byte)sub.Entries.Length);

                foreach (var entry in sub.Entries)
                {
                    writer.Write(entry.EntryId);
                    WriteShortUtf8(ref writer, entry.Name);
                    writer.Write((ushort)Math.Round(entry.Value * 10)); // one decimal preserved
                    writer.Write((ushort)Math.Round(entry.Cap * 10));   // персональный потолок, тем же масштабом
                    WriteLongUtf8(ref writer, entry.Description ?? "");
                }
            }
        }

        writer.WritePacketLength();
        ns.Send(writer.Span);
        writer.Dispose();
    }

    private static void WriteShortUtf8(ref SpanWriter writer, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);

        if (byteCount > 255)
        {
            value = value[..40]; // defensive truncation — names here are always short
            byteCount = Encoding.UTF8.GetByteCount(value);
        }

        writer.Write((byte)byteCount);
        writer.Write(value, Encoding.UTF8);
    }

    private static void WriteLongUtf8(ref SpanWriter writer, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);

        if (byteCount > ushort.MaxValue)
        {
            value = value[..500]; // defensive truncation — descriptions are always short sentences
            byteCount = Encoding.UTF8.GetByteCount(value);
        }

        writer.Write((ushort)byteCount);
        writer.Write(value, Encoding.UTF8);
    }
}
