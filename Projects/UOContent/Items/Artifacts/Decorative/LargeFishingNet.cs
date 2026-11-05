using ModernUO.Serialization;
using System;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Decorative/LargeFishingNet.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class LargeFishingNet : Item
{
    public override int LabelNumber => 1149955;

    [Constructible]
    public LargeFishingNet()
        : base(7845)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendMessage("Выберите труп, который хотите накрыть сетью.");
            from.BeginTarget(10, false, TargetFlags.None, new TargetCallback(Net_OnTarget));
        }
    }

    public void Net_OnTarget(Mobile from, object targeted)
    {
        if (targeted is Corpse && (((Corpse)targeted).Owner == null || !((Corpse)targeted).Owner.Player))
        {
            if (ValidateDeepWater(((Corpse)targeted).Map, ((Corpse)targeted).X, ((Corpse)targeted).Y))
            {
                from.Animate(12, 5, 1, true, false, 0);
                Timer.DelayCall(TimeSpan.FromSeconds(0.5), MoveCorpse, new object[] { (Corpse)targeted, from });
            }
            else
            {
                from.SendLocalizedMessage(1010485); // You can only use this in deep water!
            }
        }
        else
        {
            from.SendMessage("Сеть можно накинуть только на труп!");
        }
    }

    public void MoveCorpse(object o)
    {
        object[] ojs = (object[])o;

        Corpse c = ojs[0] as Corpse;
        Mobile from = ojs[1] as Mobile;

        if (c != null && from != null)
        {
            c.MoveToWorld(from.Location, from.Map);
        }
    }

    public override void AddNameProperties(IPropertyList list)
    {
        base.AddNameProperties(list);
        list.Add(1041645); // recovered from a shipwrecklist
    }

    private static readonly int[] _waterTiles =
    {
        0x00A8, 0x00AB,
        0x0136, 0x0137
    };

    // SOS.ValidateDeepWater is private in this codebase, so the same land-tile test is
    // carried here rather than widening that type.
    private static bool ValidateDeepWater(Map map, int x, int y)
    {
        var tileID = map.Tiles.GetLandTile(x, y).ID;
        var water = false;

        for (var i = 0; !water && i < _waterTiles.Length; i += 2)
        {
            water = tileID >= _waterTiles[i] && tileID <= _waterTiles[i + 1];
        }

        return water;
    }
}
