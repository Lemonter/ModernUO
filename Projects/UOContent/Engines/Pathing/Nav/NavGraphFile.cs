using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Server.Logging;

namespace Server.Engines.Pathing.Nav;

/// <summary>
/// On-disk form of a <see cref="NavMapGraph"/>: a small header, then every array raw, the whole
/// body zlib-compressed. The header carries the step cache's fingerprint of the tile and map data,
/// so a graph baked before a client patch is rejected and rebaked.
///
///   u32 Magic 'NAVG' · u32 Version · i32 MapId · u64 Fingerprint
///   i32 Width · i32 Height · i32 ClusterCount · i32 RegionCount · i32 EdgeCount
///   zlib { arrays in declaration order }
/// </summary>
public static class NavGraphFile
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(NavGraphFile));

    private const uint Magic = 0x4756414E; // 'NAVG'
    private const uint Version = 1;

    public static void Write(string path, NavMapGraph graph, ulong fingerprint)
    {
        var temp = path + ".tmp";

        using (var file = File.Create(temp))
        {
            using var header = new BinaryWriter(file, System.Text.Encoding.UTF8, leaveOpen: true);
            header.Write(Magic);
            header.Write(Version);
            header.Write(graph.MapId);
            header.Write(fingerprint);
            header.Write(graph.Width);
            header.Write(graph.Height);
            header.Write(graph.ClusterRegionStart.Length);
            header.Write(graph.RegionCount);
            header.Write(graph.EdgeCount);
            header.Flush();

            using var zlib = new ZLibStream(file, CompressionLevel.Fastest, leaveOpen: true);
            WriteArray<int>(zlib, graph.ClusterRegionStart);
            WriteArray<ushort>(zlib, graph.RegionX);
            WriteArray<ushort>(zlib, graph.RegionY);
            WriteArray<sbyte>(zlib, graph.RegionZ);
            WriteArray<int>(zlib, graph.EdgeStart);
            WriteArray<int>(zlib, graph.EdgeTarget);
            WriteArray<ushort>(zlib, graph.EdgeCost);
            WriteArray<ushort>(zlib, graph.PortalFromX);
            WriteArray<ushort>(zlib, graph.PortalFromY);
            WriteArray<sbyte>(zlib, graph.PortalFromZ);
            WriteArray<ushort>(zlib, graph.PortalToX);
            WriteArray<ushort>(zlib, graph.PortalToY);
            WriteArray<sbyte>(zlib, graph.PortalToZ);
        }

        File.Move(temp, path, true);
    }

    /// <summary>The graph in <paramref name="path"/>, or null if it is missing, unreadable, for
    /// another map, or baked from different tile data.</summary>
    public static NavMapGraph Read(string path, int mapId, ulong fingerprint)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var file = File.OpenRead(path);
            using var header = new BinaryReader(file, System.Text.Encoding.UTF8, leaveOpen: true);

            if (header.ReadUInt32() != Magic || header.ReadUInt32() != Version || header.ReadInt32() != mapId)
            {
                return null;
            }

            if (header.ReadUInt64() != fingerprint)
            {
                logger.Information("Nav graph {Path} is stale (tile data changed); it will be rebaked.", path);
                return null;
            }

            var width = header.ReadInt32();
            var height = header.ReadInt32();
            var clusterCount = header.ReadInt32();
            var regionCount = header.ReadInt32();
            var edgeCount = header.ReadInt32();

            using var zlib = new ZLibStream(file, CompressionMode.Decompress);

            var clusterRegionStart = ReadArray<int>(zlib, clusterCount);
            var regionX = ReadArray<ushort>(zlib, regionCount);
            var regionY = ReadArray<ushort>(zlib, regionCount);
            var regionZ = ReadArray<sbyte>(zlib, regionCount);
            var edgeStart = ReadArray<int>(zlib, regionCount + 1);
            var edgeTarget = ReadArray<int>(zlib, edgeCount);
            var edgeCost = ReadArray<ushort>(zlib, edgeCount);
            var fromX = ReadArray<ushort>(zlib, edgeCount);
            var fromY = ReadArray<ushort>(zlib, edgeCount);
            var fromZ = ReadArray<sbyte>(zlib, edgeCount);
            var toX = ReadArray<ushort>(zlib, edgeCount);
            var toY = ReadArray<ushort>(zlib, edgeCount);
            var toZ = ReadArray<sbyte>(zlib, edgeCount);

            return new NavMapGraph(
                mapId, width, height, clusterRegionStart,
                regionX, regionY, regionZ,
                edgeStart, edgeTarget, edgeCost,
                fromX, fromY, fromZ, toX, toY, toZ
            );
        }
        catch (Exception e) when (e is IOException or InvalidDataException or EndOfStreamException)
        {
            logger.Warning(e, "Nav graph {Path} could not be read; it will be rebaked.", path);
            return null;
        }
    }

    private static void WriteArray<T>(Stream stream, T[] array) where T : unmanaged =>
        stream.Write(MemoryMarshal.AsBytes(array.AsSpan()));

    private static T[] ReadArray<T>(Stream stream, int count) where T : unmanaged
    {
        var array = new T[count];
        stream.ReadExactly(MemoryMarshal.AsBytes(array.AsSpan()));
        return array;
    }
}
