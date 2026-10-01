/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CorpsePackets.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using System.IO;
using Server.Items;

namespace Server.Network;

public static class CorpsePackets
{
    public static void SendCorpseEquip(this NetState ns, Mobile beholder, Corpse beheld)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var list = beheld.EquipItems;

        var maxLength = 8 + (list.Count + 2) * 5;
        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x89);
        writer.Seek(2, SeekOrigin.Current);
        writer.Write(beheld.Serial);

        for (var i = 0; i < list.Count; ++i)
        {
            var item = list[i];

            // Пустая ячейка в списке снаряжения трупа роняла вход в игру целиком:
            // SendEverything шлёт содержимое каждого видимого трупа, и один null здесь
            // означал NullReferenceException в цикле логина — персонаж не мог войти,
            // пока труп не сгниёт. Откуда берётся null, см. Corpse.EquipItems.
            if (item?.Deleted == false && beholder?.CanSee(item) == true && item.Parent == beheld)
            {
                writer.Write((byte)(item.Layer + 1));
                writer.Write(item.Serial);
            }
        }

        if (beheld.Owner != null)
        {
            if (beheld.HairItemId > 0)
            {
                writer.Write((byte)(Layer.Hair + 1));
                writer.Write(beheld.HairSerial);
            }

            if (beheld.FacialHairItemId > 0)
            {
                writer.Write((byte)(Layer.FacialHair + 1));
                writer.Write(beheld.FacialHairSerial);
            }
        }

        writer.Write((byte)Layer.Invalid);

        writer.WritePacketLength();
        ns.Send(writer.Span);
    }

    public static void SendCorpseContent(this NetState ns, Mobile beholder, Corpse beheld)
    {
        if (ns.CannotSendPackets())
        {
            return;
        }

        var list = beheld.EquipItems;

        var count = list.Count;
        if (beheld.HairItemId > 0)
        {
            count++;
        }

        if (beheld.FacialHairItemId > 0)
        {
            count++;
        }

        var maxLength = 5 + count * (ns.ContainerGridLines ? 20 : 19);
        var writer = new SpanWriter(stackalloc byte[maxLength]);
        writer.Write((byte)0x3C);
        writer.Seek(4, SeekOrigin.Current); // Length and Count

        var written = 0;
        for (var i = 0; i < list.Count; ++i)
        {
            var child = list[i];

            // То же самое, что и в SendCorpseEquip выше: null в списке валит вход в игру.
            if (child?.Deleted == false && child.Parent == beheld && beholder?.CanSee(child) == true)
            {
                writer.Write(child.Serial);
                writer.Write((ushort)child.ItemID);
                writer.Write((byte)0); // signed, itemID offset
                writer.Write((ushort)child.Amount);
                writer.Write((short)child.X);
                writer.Write((short)child.Y);
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location?
                }
                writer.Write(beheld.Serial);
                writer.Write((ushort)child.Hue);

                ++written;
            }
        }

        if (beheld.Owner != null)
        {
            if (beheld.HairItemId > 0)
            {
                writer.Write(beheld.HairSerial);
                writer.Write((ushort)beheld.HairItemId);
                writer.Write((byte)0); // signed, itemID offset
                writer.Write((ushort)1);
                writer.Write(0); // X/Y
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location?
                }
                writer.Write(beheld.Serial);
                writer.Write((ushort)beheld.HairHue);

                ++written;
            }

            if (beheld.FacialHairItemId > 0)
            {
                writer.Write(beheld.FacialHairSerial);
                writer.Write((ushort)beheld.FacialHairItemId);
                writer.Write((byte)0); // signed, itemID offset
                writer.Write((ushort)1);
                writer.Write(0); // X/Y
                if (ns.ContainerGridLines)
                {
                    writer.Write((byte)0); // Grid Location?
                }
                writer.Write(beheld.Serial);
                writer.Write((ushort)beheld.FacialHairHue);

                ++written;
            }
        }

        writer.Seek(1, SeekOrigin.Begin);
        writer.Write((ushort)writer.BytesWritten);
        writer.Write((ushort)written);
        writer.Seek(0, SeekOrigin.End);
        ns.Send(writer.Span);
    }
}
