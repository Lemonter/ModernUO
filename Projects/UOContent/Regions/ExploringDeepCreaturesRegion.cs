using System;
using System.Text.Json.Serialization;
using Server.Engines.ExploringTheDeep;
using Server.Mobiles;

namespace Server.Regions;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Regions.cs,
///     <c>ExploringDeepCreaturesRegion</c>). One small sub-region per quest boss; stepping into it while on
///     the matching step of the Exploring the Deep chain spawns the boss once.
///
///     ServUO read <c>m.Region.Name</c> inside <c>OnEnter</c>, which is the region the mobile is *leaving*
///     at that point, and it kept the spawned creature in a field nobody read. Both are fixed: the region's
///     own <see cref="Region.Name" /> decides what to spawn, and the spawn is a plain call whose result the
///     creature classes own through their own single-instance bookkeeping.
/// </summary>
public class ExploringDeepCreaturesRegion : DungeonRegion
{
    [JsonConstructor] // Don't include parent, since it is special
    public ExploringDeepCreaturesRegion(string name, Map map, int priority, params Rectangle3D[] area) : base(name, map, priority, area)
    {
    }

    public ExploringDeepCreaturesRegion(string name, Map map, Region parent, params Rectangle3D[] area) : base(name, map, parent, area)
    {
    }

    public ExploringDeepCreaturesRegion(string name, Map map, Region parent, int priority, params Rectangle3D[] area) : base(name, map, parent, priority, area)
    {
    }

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);

        if (m is not PlayerMobile pm || !pm.Alive)
        {
            return;
        }

        switch (Name)
        {
            case "Ice Wyrm":
                {
                    if (ExploringDeepChain.IsDoing(pm, ExploringDeepChain.MasterTinkerQuest))
                    {
                        IceWyrm.Spawn(
                            new Point3D(
                                5805 + Utility.RandomMinMax(-5, 5), 240 + Utility.RandomMinMax(-5, 5), 0
                            ), Map.Trammel
                        );
                    }

                    break;
                }
            case "Mercutio The Unsavory":
                {
                    if (ExploringDeepChain.ComponentStage(pm))
                    {
                        MercutioTheUnsavory.Spawn(
                            new Point3D(
                                2582 + Utility.RandomMinMax(-5, 5), 1118 + Utility.RandomMinMax(-5, 5), 0
                            ), Map.Trammel
                        );
                    }

                    break;
                }
            case "Djinn":
                {
                    if (ExploringDeepChain.ComponentStage(pm))
                    {
                        Djinn.Spawn(
                            new Point3D(
                                1732 + Utility.RandomMinMax(-5, 5), 520 + Utility.RandomMinMax(-5, 5), 8
                            ), Map.Ilshenar
                        );
                    }

                    break;
                }
            case "Obsidian Wyvern":
                {
                    if (ExploringDeepChain.ComponentStage(pm))
                    {
                        ObsidianWyvern.Spawn(
                            new Point3D(
                                5136 + Utility.RandomMinMax(-5, 5), 966 + Utility.RandomMinMax(-5, 5), 0
                            ), Map.Trammel
                        );
                    }

                    break;
                }
            case "Orc Engineer":
                {
                    if (ExploringDeepChain.ComponentStage(pm))
                    {
                        OrcEngineer.Spawn(
                            new Point3D(
                                5311 + Utility.RandomMinMax(-5, 5), 1968 + Utility.RandomMinMax(-5, 5), 0
                            ), Map.Trammel
                        );
                    }

                    break;
                }
        }
    }
}
