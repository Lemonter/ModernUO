using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Engines.ExploringTheDeep;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/IceWyrm.cs). The white wyrm Cousteau
///     Perron wants a scale from; it is spawned once by <see cref="Regions.ExploringDeepCreaturesRegion" />
///     when a player on that step enters the Ice Dungeon lair, and removes itself after an hour.
///
///     ServUO kept the single-instance bookkeeping in a static list that the constructor and
///     <c>Deserialize</c> both pushed into; here it is only touched on construction and deletion, which is
///     what actually keeps the "only one at a time" rule intact.
/// </summary>
[SerializationGenerator(0, false)]
public partial class IceWyrm : WhiteWyrm
{
    private static readonly List<IceWyrm> _instances = new();

    private TimerExecutionToken _selfDelete;

    [Constructible]
    public IceWyrm()
    {
        Hue = 2729;
        Body = 180;
        Tamable = false;

        SetResistance(ResistanceType.Cold, 100);

        Timer.StartTimer(TimeSpan.FromMinutes(60.0), Delete, out _selfDelete);

        _instances.Add(this);
    }

    public static IReadOnlyList<IceWyrm> Instances => _instances;

    public override string CorpseName => "труп ледяного вирма";
    public override string DefaultName => "ледяной вирм";

    public override bool ReacquireOnMovement => true;
    public override int TreasureMapLevel => 4;
    public override int Meat => 20;
    public override int Hides => 25;
    public override HideType HideType => HideType.Barbed;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override bool CanAngerOnTame => true;
    public override bool CanRummageCorpses => true;

    public static IceWyrm Spawn(Point3D platLoc, Map platMap)
    {
        if (_instances.Count > 0)
        {
            return null;
        }

        var creature = new IceWyrm
        {
            Home = platLoc,
            RangeHome = 4
        };

        creature.MoveToWorld(platLoc, platMap);

        return creature;
    }

    public override void OnDeath(Container c)
    {
        // The scale is handed to every player on the looter list who is currently at the Ice Wyrm step.
        ExploringDeepDrops.Give(
            this,
            ExploringDeepChain.MasterTinkerQuest,
            static () => new IceWyrmScale()
        );

        base.OnDeath(c);
    }

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        _instances.Remove(this);

        base.OnAfterDelete();
    }
}
