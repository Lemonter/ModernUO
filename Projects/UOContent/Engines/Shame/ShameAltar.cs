using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.ShameRevamped;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (ShameAltar.cs) — the altar that sells a level's boss
///     encounter: pay crystals, the guardian spawns at the far end, you get one hour to kill it.
///
///     Adapted: the crystal balance is the player's crystal stack in the pack rather than the points
///     framework ServUO used (see ShameCrystal), spent with ConsumeTotal. The one-second deadline poll is
///     a single timer armed for the deadline; it is cancelled when the altar goes away.
/// </summary>
[SerializationGenerator(0, false)]
public partial class ShameAltar : Item
{
    public const int CoolDownMinutes = 10;
    public const bool AllowParties = false;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _active;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _summonCost;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _teleporterLocation;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _teleporterDestination;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Point3D _spawnLocation;

    [SerializableField(5)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _guardianTypeId;

    [SerializableField(6)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Mobile _summoner;

    [SerializableField(7)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ShameGuardian _guardian;

    [SerializableField(8)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _deadLine;

    [SerializableField(9)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextSummon;

    [SerializableField(10, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ShameTeleporter _teleporter;

    private TimerExecutionToken _deadlineToken;
    private bool _deadlineRunning;

    public override int LabelNumber => 1151636; // Guardian's Altar
    public override bool ForceShowProperties => true;

    /// <summary>Guardian types the altars can sell. Types are not persistable here (this codebase has no
    /// runtime script compiler), so the altar stores an index into this table instead.</summary>
    private static readonly Type[] GuardianTypes =
    {
        typeof(QuartzElemental),
        typeof(FlameElemental),
        typeof(WindElemental)
    };

    public Type GuardianType => _guardianTypeId >= 0 && _guardianTypeId < GuardianTypes.Length
        ? GuardianTypes[_guardianTypeId]
        : null;

    public ShameAltar(Type type, Point3D teleLoc, Point3D teleDest, Point3D spawnLoc, int cost, bool active = true)
        : base(13801)
    {
        Movable = false;
        Hue = 2619;

        _guardianTypeId = Array.IndexOf(GuardianTypes, type);
        _teleporterLocation = teleLoc;
        _teleporterDestination = teleDest;
        _spawnLocation = spawnLoc;
        _summonCost = cost;

        Timer.DelayCall(TimeSpan.FromSeconds(1), SpawnTeleporter);

        _active = active;
        _deadLine = DateTime.MinValue;
        _nextSummon = Core.Now;
    }

    public override void OnMapChange()
    {
        base.OnMapChange();

        if (_teleporter != null)
        {
            _teleporter.Map = Map;
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_active && from.InRange(Location, 3))
        {
            CheckSummon(from);
        }
    }

    public void CheckSummon(Mobile from)
    {
        if (from.Backpack == null || from.Backpack.GetAmount(typeof(ShameCrystal)) < _summonCost)
        {
            from.SendLocalizedMessage(1151623); // You are not yet worthy of challenging the champion.
            return;
        }

        if (_guardian != null)
        {
            from.SendLocalizedMessage(1151621); // The champion for this dungeon level has already been summoned.
            return;
        }

        if (_nextSummon > Core.Now)
        {
            // The champion has recently been defeated, and cannot be summoned again for a few minutes.
            from.SendLocalizedMessage(1151622);
            return;
        }

        SpawnGuardian();
        _summoner = from;

        from.Backpack.ConsumeTotal(new[] { typeof(ShameCrystal) }, new[] { _summonCost });

        if (_teleporter == null || _teleporter.Deleted)
        {
            SpawnTeleporter();
        }

        from.SendLocalizedMessage(1151620); // The champion accepts your challenge. You have one hour to find and defeat him!
        from.SendLocalizedMessage(1151625, _summonCost.ToString()); // Summoning ... costs you ~1_COST~ Crystals of Shame.

        StartDeadlineTimer();
    }

    public void SpawnGuardian()
    {
        if (GuardianType == null)
        {
            return;
        }

        _guardian = Activator.CreateInstance(GuardianType) as ShameGuardian;

        if (_guardian == null)
        {
            return;
        }

        _guardian.Altar = this;
        _guardian.MoveToWorld(_spawnLocation, Map);

        _guardian.Home = _spawnLocation;
        _guardian.RangeHome = 8;

        _deadLine = Core.Now + TimeSpan.FromHours(1);
    }

    public void OnGuardianKilled()
    {
        _guardian = null;

        EndDeadlineTimer();
        _nextSummon = Core.Now + TimeSpan.FromMinutes(CoolDownMinutes);
        _summoner = null;
    }

    public void StartDeadlineTimer()
    {
        EndDeadlineTimer();

        _deadlineRunning = true;
        Timer.StartTimer(_deadLine - Core.Now, CheckDeadLine, out _deadlineToken);
    }

    public void EndDeadlineTimer()
    {
        if (_deadlineRunning)
        {
            _deadlineRunning = false;
            _deadlineToken.Cancel();
        }

        if (_guardian is { Alive: true })
        {
            _guardian.Delete();

            _summoner?.SendLocalizedMessage(1151628); // You failed to defeat the champion in time.

            _nextSummon = Core.Now;
        }
    }

    public void CheckDeadLine()
    {
        _deadlineRunning = false;

        if (_deadLine < Core.Now)
        {
            EndDeadlineTimer();
        }
    }

    private void SpawnTeleporter()
    {
        _teleporter?.Delete();

        if (Map == null || Map == Map.Internal)
        {
            return;
        }

        foreach (var item in Map.GetItemsInRange(_teleporterLocation, 0))
        {
            if (item is ShameTeleporter)
            {
                item.Delete();
            }
        }

        _teleporter = new ShameTeleporter(_teleporterDestination, Map);
        _teleporter.MoveToWorld(_teleporterLocation, Map);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();
        _deadlineToken.Cancel();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {


        if (_deadLine > Core.Now)
        {
            if (_guardian != null)
            {
                _guardian.Altar = this;
                StartDeadlineTimer();
            }
            else
            {
                _deadLine = DateTime.MinValue;
            }
        }
        else if (_guardian != null)
        {
            _guardian.Delete();
            _guardian = null;

            _nextSummon = Core.Now;
        }
    }
}
