using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/EnergyTile.cs).
///     The addon reuses the same materials as the destroyed city: a line of tiles, one of which stands down for two
///     hours while the rest come back up.
/// </summary>
[SerializationGenerator(0, false)]
public partial class EnergyTileAddon : BaseAddon
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private DateTime _nextDeactivation;

    public override BaseAddonDeed Deed => null;

    [Constructible]
    public EnergyTileAddon(int count, Direction direction)
    {
        int x = 0, y = 0;

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                Movement.Movement.Offset(direction, ref x, ref y);
            }

            AddComponent(new EnergyTileComponent(), x, y, 0);
        }

        DeactivateRandom();
    }

    [Constructible]
    public EnergyTileAddon() : this(1, Direction.South)
    {
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        if (_nextDeactivation < Core.Now)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(10), DeactivateRandom);
        }
    }

    private void DeactivateRandom()
    {
        var tiles = new List<EnergyTileComponent>();

        foreach (var component in Components)
        {
            if (component is EnergyTileComponent tile)
            {
                tiles.Add(tile);
            }
        }

        if (tiles.Count == 0)
        {
            return;
        }

        var chosen = tiles[Utility.Random(tiles.Count)];

        foreach (var tile in tiles)
        {
            if (tile == chosen)
            {
                tile.Active = false;
            }
            else if (!tile.Active)
            {
                tile.Active = true;
            }
        }

        _nextDeactivation = Core.Now + TimeSpan.FromMinutes(120);
    }
}

/// <summary>Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/EnergyTile.cs).</summary>
[SerializationGenerator(0, false)]
public partial class EnergyTileComponent : AddonComponent
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _active;

    [Constructible]
    public EnergyTileComponent() : base(0x9B3A)
    {
        _active = true;
        Hue = 2591;
    }

    public override bool OnMoveOver(Mobile m)
    {
        if (!_active)
        {
            return base.OnMoveOver(m);
        }

        if (m is PlayerMobile || m is BaseCreature { Controlled: true } creature && creature.GetMaster() is PlayerMobile)
        {
            AOS.Damage(m, Utility.RandomMinMax(100, 150), false, 0, 0, 0, 0, 100);

            m.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist);
            m.PlaySound(0x665);
        }

        return base.OnMoveOver(m);
    }
}
