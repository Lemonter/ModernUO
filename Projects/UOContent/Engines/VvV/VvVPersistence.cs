using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Saves ViceVsVirtueSystem's runtime state — player entries, guild stats, exempt cities, the
// active battle — since there's no `PointsSystem`-based save format here (see
// ViceVsVirtueSystem.cs header). Same `GenericPersistence` pattern as
// Engines/Shadowguard/ShadowguardPersistence.cs.
public class VvVPersistence : GenericPersistence
{
    public VvVPersistence() : base("ViceVsVirtue", 1)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var sys = ViceVsVirtueSystem.Instance;

        writer.Write(sys.HasGenerated);

        writer.Write(sys.ExemptCities.Count);
        foreach (var city in sys.ExemptCities)
        {
            writer.Write((int)city);
        }

        writer.Write(sys.PlayerEntries.Count);
        foreach (var (pm, entry) in sys.PlayerEntries)
        {
            writer.Write(pm);
            writer.Write(entry.Points);
            writer.Write(entry.TotalKills);
            writer.Write(entry.TotalDeaths);
            writer.Write(entry.Active);
            writer.Write(entry.Score);
            writer.Write(entry.Kills);
            writer.Write(entry.Deaths);
            writer.Write(entry.Assists);
            writer.Write(entry.ReturnedSigils);
            writer.Write(entry.DisarmedTraps);
            writer.Write(entry.StolenSigils);
            writer.Write(entry.ResignExpiration);
        }

        writer.Write(sys.GuildStats.Count);
        foreach (var (guild, stats) in sys.GuildStats)
        {
            writer.Write(guild);
            writer.Write(stats.Score);
            writer.Write(stats.Kills);
            writer.Write(stats.ReturnedSigils);
        }

        sys.Battle.Serialize(writer);
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var sys = ViceVsVirtueSystem.Instance;

        sys.HasGenerated = reader.ReadBool();

        var exemptCount = reader.ReadInt();
        for (var i = 0; i < exemptCount; i++)
        {
            sys.ExemptCities.Add((VvVCity)reader.ReadInt());
        }

        var entryCount = reader.ReadInt();
        for (var i = 0; i < entryCount; i++)
        {
            var pm = reader.ReadEntity<Mobile>() as PlayerMobile;

            var points = reader.ReadInt();
            var totalKills = reader.ReadInt();
            var totalDeaths = reader.ReadInt();
            var active = reader.ReadBool();
            var score = reader.ReadInt();
            var kills = reader.ReadInt();
            var deaths = reader.ReadInt();
            var assists = reader.ReadInt();
            var returnedSigils = reader.ReadInt();
            var disarmedTraps = reader.ReadInt();
            var stolenSigils = reader.ReadInt();
            var resignExpiration = reader.ReadDateTime();

            if (pm == null)
            {
                continue;
            }

            var entry = new VvVPlayerEntry(pm)
            {
                Points = points,
                TotalKills = totalKills,
                TotalDeaths = totalDeaths,
                Active = active,
                Score = score,
                Kills = kills,
                Deaths = deaths,
                Assists = assists,
                ReturnedSigils = returnedSigils,
                DisarmedTraps = disarmedTraps,
                StolenSigils = stolenSigils,
                ResignExpiration = resignExpiration
            };

            sys.PlayerEntries[pm] = entry;
        }

        var guildStatCount = reader.ReadInt();
        for (var i = 0; i < guildStatCount; i++)
        {
            var g = reader.ReadEntity<BaseGuild>() as Guild;
            var score = reader.ReadInt();
            var kills = reader.ReadInt();
            var returnedSigils = reader.ReadInt();

            if (g == null)
            {
                continue;
            }

            sys.GuildStats[g] = new VvVGuildStats(g)
            {
                Score = score,
                Kills = kills,
                ReturnedSigils = returnedSigils
            };
        }

        sys.Battle.Deserialize(reader);
    }
}
