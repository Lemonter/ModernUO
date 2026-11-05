using System;
using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/ExploringTheDeep/Mobiles/SalvageHookMonsters.cs). The four
///     creatures a fouled salvage hook pulls up; all of them remove themselves after thirty minutes.
///     The original timed that with a <c>Timer.DelayCall</c> in the constructor and then deleted any such
///     creature on load, which made them disposable but also lost the remainder of their lifetime across a
///     save; here the timer is re-armed from the constructor and on deserialization.
/// </summary>
[SerializationGenerator(0, false)]
public partial class HPKraken : Kraken
{
    private TimerExecutionToken _selfDelete;

    [Constructible]
    public HPKraken() => StartSelfDelete();

    [AfterDeserialization]
    private void AfterDeserialization() => StartSelfDelete();

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        base.OnAfterDelete();
    }

    private void StartSelfDelete() => Timer.StartTimer(TimeSpan.FromMinutes(30.0), Delete, out _selfDelete);
}

[SerializationGenerator(0, false)]
public partial class HPDeepSeaSerpent : DeepSeaSerpent
{
    private TimerExecutionToken _selfDelete;

    [Constructible]
    public HPDeepSeaSerpent() => StartSelfDelete();

    [AfterDeserialization]
    private void AfterDeserialization() => StartSelfDelete();

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        base.OnAfterDelete();
    }

    private void StartSelfDelete() => Timer.StartTimer(TimeSpan.FromMinutes(30.0), Delete, out _selfDelete);
}

[SerializationGenerator(0, false)]
public partial class HPSeaSerpent : SeaSerpent
{
    private TimerExecutionToken _selfDelete;

    [Constructible]
    public HPSeaSerpent() => StartSelfDelete();

    [AfterDeserialization]
    private void AfterDeserialization() => StartSelfDelete();

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        base.OnAfterDelete();
    }

    private void StartSelfDelete() => Timer.StartTimer(TimeSpan.FromMinutes(30.0), Delete, out _selfDelete);
}

[SerializationGenerator(0, false)]
public partial class HPWaterElemental : WaterElemental
{
    private TimerExecutionToken _selfDelete;

    [Constructible]
    public HPWaterElemental() => StartSelfDelete();

    [AfterDeserialization]
    private void AfterDeserialization() => StartSelfDelete();

    public override void OnAfterDelete()
    {
        _selfDelete.Cancel();
        base.OnAfterDelete();
    }

    private void StartSelfDelete() => Timer.StartTimer(TimeSpan.FromMinutes(30.0), Delete, out _selfDelete);
}
