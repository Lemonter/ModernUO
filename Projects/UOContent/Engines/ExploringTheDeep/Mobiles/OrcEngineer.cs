using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.ExploringTheDeep;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/OrcEngineer.cs). The orc in the Orc
///     Cave who carries the drilling machine schematics Josef Skimmons wants; spawned once per region
///     entry and removed after ten minutes.
/// </summary>
[SerializationGenerator(0, false)]
public partial class OrcEngineer : Orc
{
    private static readonly List<OrcEngineer> _instances = new();

    private TimerExecutionToken _selfDelete;

    [Constructible]
    public OrcEngineer()
    {
        Title = "орк-инженер";

        SetStr(500, 510);
        SetDex(200, 210);
        SetInt(200, 210);

        SetHits(3500, 3700);

        SetDamage(8, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 65, 70);
        SetResistance(ResistanceType.Fire, 65, 70);
        SetResistance(ResistanceType.Cold, 65, 70);
        SetResistance(ResistanceType.Poison, 60);
        SetResistance(ResistanceType.Energy, 65, 70);

        SetSkill(SkillName.Parry, 100.0, 110.0);
        SetSkill(SkillName.MagicResist, 70.0, 80.0);
        SetSkill(SkillName.Tactics, 110.0, 120.0);
        SetSkill(SkillName.Wrestling, 120.0, 130.0);
        SetSkill(SkillName.DetectHidden, 100.0, 110.0);

        Fame = 1500;
        Karma = -1500;

        _instances.Add(this);

        Timer.StartTimer(TimeSpan.FromMinutes(10.0), Delete, out _selfDelete);
    }

    public static IReadOnlyList<OrcEngineer> Instances => _instances;

    public override string CorpseName => "труп орка";
    public override string DefaultName => "орк-инженер";

    public static OrcEngineer Spawn(Point3D platLoc, Map platMap)
    {
        if (_instances.Count > 0)
        {
            return null;
        }

        var creature = new OrcEngineer
        {
            Home = platLoc,
            RangeHome = 4
        };

        creature.MoveToWorld(platLoc, platMap);

        return creature;
    }

    public override void OnDeath(Container c)
    {
        ExploringDeepDrops.Give(this, ExploringDeepChain.ComponentQuest, static () => new OrcishSchematics());

        base.OnDeath(c);
    }

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        _instances.Remove(this);

        base.OnAfterDelete();
    }
}
