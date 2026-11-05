using System;
using ModernUO.Serialization;
using Server.Engines.MLQuests;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Флагов MondainsLegacy.Bedlam в репозитории нет, доступ проверяется
///     через MLQuestContext.BedlamAccess.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BedlamTele : Item
{
    [Constructible]
    public BedlamTele() : base(0x124D)
    {
        Movable = false;
    }

    public override int LabelNumber => 1074161; // Access to Bedlam by invitation only

    public override bool ForceShowProperties => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.NetState == null || !from.NetState.SupportsExpansion(Expansion.ML))
        {
            // You must upgrade to the Mondain's Legacy expansion in order to enter here.
            from.SendLocalizedMessage(1072608);
            return;
        }

        if (from is PlayerMobile player)
        {
            if (MLQuestSystem.GetContext(player)?.BedlamAccess == true)
            {
                var p = new Point3D(121, 1682, 0);
                BaseCreature.TeleportPets(player, p, Map);
                player.MoveToWorld(p, Map);
            }
            else
            {
                // You press and push on the iron maiden, but nothing happens.
                player.SendLocalizedMessage(1074276);
            }
        }
    }
}
