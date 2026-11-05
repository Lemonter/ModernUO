using System;
using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class BaseSliding : Item
{
    [SerializableField(0)]
    private int _openedID;

    [SerializableField(1)]
    private int _closedID;

    [Constructible]
    public BaseSliding(int closedID, int openedID) : base(closedID)
    {
        Movable = false;

        _openedID = openedID;
        _closedID = closedID;
    }

    public override void OnDoubleClick(Mobile from)
    {
        ItemID = _openedID;

        Timer.DelayCall(
            TimeSpan.FromSeconds(1.6),
            () =>
            {
                if (Deleted)
                {
                    return;
                }

                Z += -22;
                Visible = false;

                Timer.DelayCall(
                    TimeSpan.FromSeconds(5.0),
                    () =>
                    {
                        if (Deleted)
                        {
                            return;
                        }

                        ItemID = _closedID;
                        Visible = true;
                        Z += 22;
                    }
                );
            }
        );
    }
}
