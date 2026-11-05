using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Multis;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Улей из награды Clean Up Britannia: раз в четыре часа наполняется мёдом, пока в нём
///     меньше десяти горшочков.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Beehive : Item, ISecurable
{
    private static readonly TimeSpan SpawnTime = TimeSpan.FromHours(4.0);

    private TimerExecutionToken _spawnTimerToken;

    [SerializableField(0, allowFieldChange: nameof(ValidateHoneypots), fieldChanged: nameof(OnHoneypotsChanged))]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _honeypots;

    [AnchoredDateTime]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextSpawnTime;

    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public Beehive() : base(2330)
    {
        Weight = 10.0;

        _honeypots = 0;
        StartSpawnTimer(TimeSpan.FromMinutes(1.0));
    }

    public override bool ForceShowProperties => true;

    public override int LabelNumber => 1080263; // Beehive

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1080202, $"{_honeypots}"); // Honeypots: ~1_COUNT~
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    private bool ValidateHoneypots(ref int value)
    {
        value = Math.Clamp(value, 0, 10);

        return true;
    }

    private void OnHoneypotsChanged(int oldValue, int newValue)
    {
        // Достигнув десяти горшочков, улей перестаёт наполняться.
        if (newValue >= 10)
        {
            _spawnTimerToken.Cancel();
        }
        else
        {
            StartSpawnTimer(SpawnTime);
        }

        InvalidateProperties();
    }

    private void StartSpawnTimer(TimeSpan delay)
    {
        _spawnTimerToken.Cancel();

        _nextSpawnTime = Core.Now + delay;

        Timer.StartTimer(delay, OnSpawnTick, out _spawnTimerToken);
    }

    private void OnSpawnTick()
    {
        if (Deleted)
        {
            return;
        }

        Honeypots++;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_honeypots >= 10)
        {
            return;
        }

        var delay = _nextSpawnTime - Core.Now;

        StartSpawnTimer(delay < TimeSpan.Zero ? TimeSpan.Zero : delay);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (_honeypots > 0)
        {
            from.AddToBackpack(new JarHoney());
            Honeypots--;
        }
    }

    public override void OnAfterDelete()
    {
        _spawnTimerToken.Cancel();

        base.OnAfterDelete();
    }
}
