using ModernUO.Serialization;
using System;
using System.Collections.Generic;
using Server.Collections;
using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Network;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Addons/AddonComponent.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class InstrumentedAddonComponent : AddonComponent
{
    [CommandProperty(AccessLevel.GameMaster)]
    public int SuccessSound { get; set; }

    public InstrumentedAddonComponent(int itemID, int wellSound)
        : base(itemID)
    {
        SuccessSound = wellSound;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (from.BeginAction(typeof(InstrumentedAddonComponent)))
        {
            Timer.DelayCall(TimeSpan.FromMilliseconds(1000), () =>
            {
                from.EndAction(typeof(InstrumentedAddonComponent));
            });

            from.PlaySound(SuccessSound);
        }
        else
        {
            from.SendLocalizedMessage(500119); // You must wait to perform another action
        }
    }
}
