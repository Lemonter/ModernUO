using System;
using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class CoralTheOwl : Item, IRewardItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    private TownCrierEntry _news;
    private int _newsIndex;
    private TimerExecutionToken _newsToken;

    [Constructible]
    public CoralTheOwl() : base(0x9A9B)
    {
        LootType = LootType.Blessed;
    }

    public override int LabelNumber => 1123603; // Coral the Owl

    public override bool HandlesOnSpeech => true;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076217); // 1st Year Veteran Reward
        }
    }

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (!IsLockedDown || !e.HasKeyword(0x30) || !e.Mobile.Alive || !e.Mobile.InLOS(Location) ||
            !e.Mobile.InRange(this, 12))
        {
            return;
        }

        var entry = GlobalTownCrierEntryList.Instance.GetRandomEntry();

        if (entry == null)
        {
            PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005643); // I have no news at this time.
            return;
        }

        _news = entry;
        _newsIndex = 0;

        Timer.StartTimer(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(3.0), ShoutNews, out _newsToken);

        PublicOverheadMessage(MessageType.Regular, 0x3B2, 502978); // Some of the latest news!
    }

    private void ShoutNews()
    {
        if (_news == null || _newsIndex < 0 || _newsIndex >= _news.Lines.Length)
        {
            _newsToken.Cancel();
            _news = null;
            return;
        }

        PublicOverheadMessage(MessageType.Regular, 0x3B2, false, _news.Lines[_newsIndex]);
        _newsIndex++;
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
    }

    public override void OnAfterDelete()
    {
        _newsToken.Cancel();
        _news = null;

        base.OnAfterDelete();
    }
}
