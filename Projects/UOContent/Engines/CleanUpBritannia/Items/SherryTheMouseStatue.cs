using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Статуэтка Шерри Мышка: пока закреплена в доме, по слову *news* вещает последние новости.
/// </summary>
[SerializationGenerator(0, false)]
public partial class SherryTheMouseStatue : Item
{
    private TimerExecutionToken _newsTimerToken;
    private TownCrierEntry _newsEntry;
    private int _newsIndex;

    [Constructible]
    public SherryTheMouseStatue() : base(0x20D0)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;
    }

    public override int LabelNumber => 1080171; // Sherry the Mouse Statue

    public override bool HandlesOnSpeech => true;

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (!IsLockedDown || !e.HasKeyword(0x30) || !e.Mobile.Alive || !e.Mobile.InLOS(Location) || !e.Mobile.InRange(this, 12))
        {
            return;
        }

        var tce = GlobalTownCrierEntryList.Instance.GetRandomEntry();

        if (tce == null)
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005643); // I have no news at this time.
            return;
        }

        PublicOverheadMessage(MessageType.Regular, 0x3B2, 502978); // Some of the latest news!

        _newsEntry = tce;
        _newsIndex = 0;

        _newsTimerToken.Cancel();
        Timer.StartTimer(TimeSpan.FromSeconds(3.0), TimeSpan.FromSeconds(3.0), ShoutNews, out _newsTimerToken);
    }

    private void ShoutNews()
    {
        if (_newsEntry == null || _newsIndex >= _newsEntry.Lines.Length)
        {
            _newsTimerToken.Cancel();
            _newsEntry = null;
            return;
        }

        PublicOverheadMessage(MessageType.Regular, 0x3B2, false, _newsEntry.Lines[_newsIndex]);
        _newsIndex++;
    }

    public override void OnAfterDelete()
    {
        _newsTimerToken.Cancel();
        _newsEntry = null;

        base.OnAfterDelete();
    }
}
