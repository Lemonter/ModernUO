using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Items/JackInTheBox.cs).
///     ServUO's unused <c>Server.Events.Halloween</c> using is dropped (that namespace is not present here).
/// </summary>
[SerializationGenerator(0, false)]
public partial class JackInTheBox : Item
{
    private DateTime _lastUse;
    private TimerExecutionToken _openToken;
    private TimerExecutionToken _closeToken;

    public override int LabelNumber => 1157655; // Jack in the Box

    [Constructible]
    public JackInTheBox() : base(0x9F64)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_lastUse + TimeSpan.FromSeconds(20) >= Core.Now)
        {
            from.SendLocalizedMessage(501789); // You must wait before trying again.
            return;
        }

        ItemID = 0x9F64;

        Effects.PlaySound(GetWorldLocation(), Map, 1661);

        Timer.StartTimer(TimeSpan.FromSeconds(9), Open, out _openToken);

        _lastUse = Core.Now;
    }

    private void Open()
    {
        ItemID = 0x9F65;

        Timer.StartTimer(TimeSpan.FromSeconds(5), Close, out _closeToken);
    }

    private void Close()
    {
        ItemID = 0x9F64;
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _openToken.Cancel();
        _closeToken.Cancel();
    }
}
