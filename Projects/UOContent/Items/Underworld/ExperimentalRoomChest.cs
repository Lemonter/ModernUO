using System;
using System.Collections.Generic;
using ModernUO.Serialization;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Services/Underworld/ExperimentalRoom/
// ExperimentalRoomChest.cs) — the final-room reward chest. Drop a completed ExperimentalGem
// in (or double-click it while carrying one) and it's consumed for a random trophy. Rewards
// are instanced per-player (IsChildVisibleTo) so multiple players finishing around the same
// time don't see each other's loot, and unclaimed drops decay after 10 minutes.
[SerializationGenerator(0, false)]
public partial class ExperimentalRoomChest : MetalBox
{
    private Dictionary<Item, Mobile> _instancing = new();

    public override bool DisplayWeight => false;
    public override bool DisplaysContent => false;
    public override bool Decays => true;
    public override TimeSpan DecayTime => TimeSpan.FromMinutes(10.0);

    [Constructible]
    public ExperimentalRoomChest()
    {
        Movable = false;
        LiftOverride = true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        var pack = from.Backpack;

        if (pack != null)
        {
            var item = pack.FindItemByType(typeof(ExperimentalGem));

            if (item is ExperimentalGem { Complete: true })
            {
                item.Delete();

                var toDrop = GetRandomDrop();

                if (toDrop != null)
                {
                    AddItemFor(toDrop, from);
                }
            }
        }

        base.OnDoubleClick(from);
    }

    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
    {
        if (dropped is ExperimentalGem { Complete: true } && from.InRange(Location, 2))
        {
            dropped.Delete();

            var toDrop = GetRandomDrop();

            if (toDrop != null)
            {
                AddItemFor(toDrop, from);
            }

            OnDoubleClick(from);
        }

        return false;
    }

    public void AddItemFor(Item item, Mobile mob)
    {
        if (item == null || mob == null)
        {
            return;
        }

        DropItem(item);
        item.SetLastMoved();

        _instancing ??= new Dictionary<Item, Mobile>();
        _instancing[item] = mob;
    }

    public override bool IsChildVisibleTo(Mobile m, Item child)
    {
        if (m.AccessLevel > AccessLevel.Player)
        {
            return true;
        }

        if (_instancing == null)
        {
            return true;
        }

        return !_instancing.TryGetValue(child, out var owner) || owner == m;
    }

    public override bool OnDecay()
    {
        var items = new List<Item>(Items);

        foreach (var i in items)
        {
            if (i.Decays && i.LastMoved.Add(DecayTime) < Core.Now)
            {
                i.Delete();
                _instancing.Remove(i);
            }
        }

        return false;
    }

    public override void RemoveItem(Item item)
    {
        _instancing?.Remove(item);
        base.RemoveItem(item);
    }

    public Item GetRandomDrop() =>
        Utility.Random(17) switch
        {
            <= 6  => new Stalagmite(),
            <= 10 => new Flowstone(),
            11    => new CanvaslessEasel(),
            12    => new HangingChainmailLegs(),
            13    => new HangingRingmailTunic(),
            14    => new PluckedChicken(),
            15    => new ColorfulTapestry(),
            _     => new TwoStoryBanner()
        };

    [AfterDeserialization]
    private void AfterDeserialization() => _instancing ??= new Dictionary<Item, Mobile>();
}
