using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Shadowguard;

[SerializationGenerator(0, false)]
public partial class ShadowguardBottleOfLiquor : BaseDecayingItem
{
    public override int Lifespan => 60;
    public override int LabelNumber => 1042961; // a bottle of liquor

    public BarEncounter Encounter { get; set; }

    [Constructible]
    public ShadowguardBottleOfLiquor(BarEncounter encounter) : base(0x99B) => Encounter = encounter;

    public override void OnDoubleClick(Mobile m)
    {
        if (!m.InRange(GetWorldLocation(), 2))
        {
            return;
        }

        if (0.1 > Utility.RandomDouble())
        {
            m.BAC = Math.Min(60, m.BAC + 10);
            m.PlaySound(Utility.RandomList(0x30, 0x2D6));
            BaseBeverage.CheckHeaveTimer(m);

            // *You ready the bottle to throw but it's enchanting label persuades you to drink it instead!*
            m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1156270, m.NetState);

            Delete();
            return;
        }

        m.SendLocalizedMessage(1010086); // What do you want to use this on?
        m.BeginTarget(10, false, TargetFlags.None, (from, targeted) =>
        {
            if (0.25 > Utility.RandomDouble() && m.BAC > 0)
            {
                AOS.Damage(m, Utility.RandomMinMax(25, 50), 100, 0, 0, 0, 0);
                // *You wind up to throw but in your inebriated state you manage to hit yourself!*
                m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1156271, m.NetState);
                m.FixedParticles(0x3728, 20, 10, 5044, EffectLayer.Head);

                Delete();
            }
            else if (targeted is ShadowguardPirate pirate)
            {
                m.DoHarmful(pirate);
                m.MovingParticles(pirate, 0x99B, 10, 0, false, true, 0, 0, 9502, 6014, 0x11D, EffectLayer.Waist, 0);

                Timer.DelayCall(TimeSpan.FromSeconds(0.5), () =>
                {
                    if (pirate.Alive)
                    {
                        pirate.BlockReflect = true;
                        AOS.Damage(pirate, m, 300, false, 0, 0, 0, 0, 0, 0, 100);
                        pirate.BlockReflect = false;
                        pirate.FixedParticles(0x3728, 20, 10, 5044, EffectLayer.Head);

                        pirate.PlaySound(Utility.Random(0x3E, 3));
                    }
                });

                Delete();
            }
            else
            {
                m.SendLocalizedMessage(1156211); // You cannot throw this there!
            }
        });
    }

    public override void OnAfterDelete() => Encounter?.CheckEncounter();
}

public enum VirtueType
{
    Honesty,
    Compassion,
    Valor,
    Justice,
    Sacrifice,
    Honor,
    Spirituality,
    Humility,
    Deceit,
    Despise,
    Destard,
    Wrong,
    Covetous,
    Shame,
    Hythloth,
    Pride
}

[SerializationGenerator(0, false)]
public partial class ShadowguardApple : BaseDecayingItem
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ShadowguardCypress _tree;

    [CommandProperty(AccessLevel.GameMaster)]
    public OrchardEncounter Encounter { get; set; }

    public bool Thrown;

    public override int Lifespan => 30;

    [Constructible]
    public ShadowguardApple(OrchardEncounter encounter, ShadowguardCypress tree) : base(0x9D0)
    {
        Encounter = encounter;
        _tree = tree;
    }

    public override void AddNameProperty(IPropertyList list)
    {
        if (_tree != null)
        {
            list.Add(1156210, _tree.VirtueType.ToString()); // An Enchanted Apple of ~1_TYPE~
        }
    }

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack) || _tree == null)
        {
            return;
        }

        m.SendLocalizedMessage(1010086); // What do you want to use this on?
        m.BeginTarget(10, false, TargetFlags.None, (from, targeted) =>
        {
            Thrown = true;

            var tree = targeted switch
            {
                ShadowguardCypress t                             => t,
                ShadowguardCypress.ShadowguardCypressFoilage foil => foil.Tree,
                _                                                 => null
            };

            if (tree == null)
            {
                return;
            }

            var p = tree.Location;
            var map = tree.Map;

            from.Animate(31, 7, 1, true, false, 0);
            m.MovingParticles(tree, ItemID, 10, 0, false, true, 0, 0, 9502, 6014, 0x11D, EffectLayer.Waist, 0);

            Timer.DelayCall(TimeSpan.FromSeconds(0.7), () =>
            {
                if (tree.IsOppositeVirtue(_tree.VirtueType))
                {
                    tree.Delete();

                    Effects.SendLocationParticles(EffectItem.Create(p, map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                    Effects.PlaySound(p, map, 0x243);

                    // *Your throw releases powerful magics and destroys the tree!*
                    m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1156213, m.NetState);

                    if (_tree != null)
                    {
                        p = _tree.Location;
                        _tree.Delete();

                        Effects.SendLocationParticles(EffectItem.Create(p, map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                        Effects.PlaySound(p, map, 0x243);
                    }

                    tree.Encounter.CheckEncounter();
                    Delete();
                }
                else if (Encounter != null)
                {
                    using var mobiles = Encounter.Region.GetMobilesPooled();
                    foreach (var mob in mobiles)
                    {
                        if (mob is not PlayerMobile { Alive: true } pm)
                        {
                            continue;
                        }

                        var spawnLoc = pm.Location;
                        var creature = new VileTreefellow();

                        for (var i = 0; i < 10; i++)
                        {
                            var x = Utility.RandomMinMax(spawnLoc.X - 1, spawnLoc.X + 1);
                            var y = Utility.RandomMinMax(spawnLoc.Y - 1, spawnLoc.Y + 1);
                            var z = spawnLoc.Z;

                            if (map.CanSpawnMobile(x, y, z))
                            {
                                spawnLoc = new Point3D(x, y, z);
                                break;
                            }
                        }

                        creature.MoveToWorld(spawnLoc, map);
                        Timer.DelayCall(() => creature.Combatant = pm);

                        Encounter.AddSpawn(creature);
                    }

                    // *Your throw seems to have summoned an ambush!*
                    m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1156212, m.NetState);
                    Delete();
                }
            });
        });
    }

    public override void OnDelete()
    {
        base.OnDelete();

        if (Thrown || Encounter == null)
        {
            return;
        }

        using var mobiles = Encounter.Region.GetMobilesPooled();
        foreach (var mob in mobiles)
        {
            if (mob is not PlayerMobile { Alive: true } pm)
            {
                continue;
            }

            var p = pm.Location;
            var map = pm.Map;
            var creature = new VileTreefellow();

            for (var i = 0; i < 10; i++)
            {
                var x = Utility.RandomMinMax(p.X - 1, p.X + 1);
                var y = Utility.RandomMinMax(p.Y - 1, p.Y + 1);
                var z = p.Z;

                if (map.CanSpawnMobile(x, y, z))
                {
                    p = new Point3D(x, y, z);
                    break;
                }
            }

            creature.MoveToWorld(p, map);
            Timer.DelayCall(() => creature.Combatant = pm);

            Encounter.AddSpawn(creature);
        }
    }

    public override void OnAfterDelete() => Encounter?.OnAppleDeleted();
}

[SerializationGenerator(0, false)]
public partial class ShadowguardCypress : Item
{
    [CommandProperty(AccessLevel.GameMaster)]
    public OrchardEncounter Encounter { get; set; }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private VirtueType _virtueType;

    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private ShadowguardCypressFoilage _foilage;

    [Constructible]
    public ShadowguardCypress(OrchardEncounter encounter, VirtueType type) : base(3329)
    {
        _virtueType = type;
        Encounter = encounter;

        _foilage = new ShadowguardCypressFoilage(Utility.RandomBool() ? 0xD96 : 0xD9A, this);

        Movable = false;
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        if (_foilage != null)
        {
            _foilage.Location = new Point3D(X, Y, Z + 6);
        }
    }

    public override void OnMapChange()
    {
        if (_foilage != null)
        {
            _foilage.Map = Map;
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack == null || !from.InRange(Location, 3))
        {
            return;
        }

        if (Encounter.Apple is not { Deleted: false })
        {
            Encounter.Apple = new ShadowguardApple(Encounter, this);
            from.Backpack.DropItem(Encounter.Apple);

            Encounter.OnApplePicked();
        }
    }

    public bool IsOppositeVirtue(VirtueType type) =>
        type switch
        {
            VirtueType.Honesty      => _virtueType == VirtueType.Deceit,
            VirtueType.Compassion   => _virtueType == VirtueType.Despise,
            VirtueType.Valor        => _virtueType == VirtueType.Destard,
            VirtueType.Justice      => _virtueType == VirtueType.Wrong,
            VirtueType.Sacrifice    => _virtueType == VirtueType.Covetous,
            VirtueType.Honor        => _virtueType == VirtueType.Shame,
            VirtueType.Spirituality => _virtueType == VirtueType.Hythloth,
            VirtueType.Humility     => _virtueType == VirtueType.Pride,
            VirtueType.Deceit       => _virtueType == VirtueType.Honesty,
            VirtueType.Despise      => _virtueType == VirtueType.Compassion,
            VirtueType.Destard      => _virtueType == VirtueType.Valor,
            VirtueType.Wrong        => _virtueType == VirtueType.Justice,
            VirtueType.Covetous     => _virtueType == VirtueType.Sacrifice,
            VirtueType.Shame        => _virtueType == VirtueType.Honor,
            VirtueType.Hythloth     => _virtueType == VirtueType.Spirituality,
            VirtueType.Pride        => _virtueType == VirtueType.Humility,
            _                       => _virtueType == VirtueType.Deceit
        };

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _foilage?.Delete();
        Encounter?.CheckEncounter();
    }

    [SerializationGenerator(0, false)]
    public partial class ShadowguardCypressFoilage : Item
    {
        public ShadowguardCypress Tree { get; set; }

        public ShadowguardCypressFoilage(int id, ShadowguardCypress cypress) : base(id)
        {
            Movable = false;
            Tree = cypress;
        }

        public override void OnDoubleClick(Mobile m) => Tree?.OnDoubleClick(m);
    }
}

[SerializationGenerator(0, false)]
public partial class Phylactery : BaseDecayingItem
{
    public override int Lifespan => 60;
    public override int LabelNumber => _purified ? 1156221 : 1156220; // Purified Phylactery : Corrupt Phylactery

    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _purified;

    [Constructible]
    public Phylactery() : base(17076) => Hue = 2075;

    public override void OnDoubleClick(Mobile m)
    {
        if (!IsChildOf(m.Backpack))
        {
            return;
        }

        m.SendLocalizedMessage(1010086); // What do you want to use this on?
        m.BeginTarget(3, false, TargetFlags.None, (from, targeted) =>
        {
            if (targeted is PurifyingFlames flames)
            {
                if (!from.InLOS(flames))
                {
                    from.SendLocalizedMessage(500237); // Target cannot be seen.
                }
                else if (!Purified)
                {
                    m.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1156225, m.NetState); // *You purify the phylactery!*

                    Effects.SendLocationParticles(EffectItem.Create(flames.Location, flames.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                    Effects.PlaySound(flames.Location, flames.Map, 0x225);

                    Purified = true;
                }
            }
            else if (targeted is CursedSuitOfArmor armor)
            {
                if (!from.InLOS(armor))
                {
                    from.SendLocalizedMessage(500237); // Target cannot be seen.
                }
                else if (!_purified)
                {
                    m.SendLocalizedMessage(1156224); // *The cursed armor rejects the phylactery!*
                }
                else
                {
                    m.SendLocalizedMessage(1156222); // *You throw the phylactery at the armor causing it to disintegrate!*

                    var map = armor.Map;
                    var p = armor.ItemID == 5402
                        ? new Point3D(armor.X - 1, armor.Y, armor.Z)
                        : new Point3D(armor.X, armor.Y - 1, armor.Z);

                    armor.Delete();
                    Delete();

                    Effects.SendLocationParticles(EffectItem.Create(p, map, EffectItem.DefaultDuration), 0x3709, 10, 30, 2720, 7, 5052, 0);
                    Effects.PlaySound(p, map, 0x225);

                    Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
                    {
                        Item item = new Static(Utility.Random(8762, 16));
                        item.Hue = 1111;
                        item.Name = "Broken Armor";
                        item.MoveToWorld(p, Map.TerMur);

                        if (ShadowguardController.GetEncounter(p, Map.TerMur) is ArmoryEncounter encounter)
                        {
                            encounter.AddDestroyedArmor(item);
                        }

                        var ticks = 1;
                        Timer.DelayCall(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50), 2, () =>
                        {
                            Misc.Geometry.Circle2D(p, map, ticks, (pnt, mob) =>
                            {
                                Effects.PlaySound(pnt, mob, 0x307);
                                Effects.SendLocationEffect(pnt, mob, Utility.RandomBool() ? 14000 : 14013, 20, 2018, 0);
                            });

                            ticks++;
                        });
                    });
                }
            }
        });
    }
}

[SerializationGenerator(0, false)]
public partial class CursedSuitOfArmor : Item
{
    [CommandProperty(AccessLevel.GameMaster)]
    public ShadowguardEncounter Encounter { get; set; }

    public override int LabelNumber => 1156218; // Cursed Suit of Armor

    [Constructible]
    public CursedSuitOfArmor(ShadowguardEncounter encounter) : base(0x151A)
    {
        Encounter = encounter;
        Movable = false;
    }

    public override void OnAfterDelete() => Encounter?.CheckEncounter();
}

[SerializationGenerator(0, false)]
public partial class PurifyingFlames : Item
{
    public override int LabelNumber => 1156217; // Purifying Flames

    [Constructible]
    public PurifyingFlames() : base(0x19AB) => Movable = false;
}

public enum Flow
{
    EastWest,
    NorthSouth,
    NorthWestCorner,
    NorthEastCorner,
    SouthWestCorner,
    SouthEastCorner
}

[SerializationGenerator(0, false)]
public partial class ShadowguardCanal : Item, IAxe
{
    public override int LabelNumber => 1156228; // Canal

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private Flow _flow;

    [SerializableFieldChanged(0)]
    private void OnFlowChanged(Flow oldValue, Flow newValue) => InvalidateIdFromFlow();

    [Constructible]
    public ShadowguardCanal() : base(Utility.RandomList(39911, 39915, 39919, 39924, 39928, 39932))
    {
        InvalidateID();
        Hue = 2500;
    }

    [Constructible]
    public ShadowguardCanal(Flow flow) : base(0)
    {
        Flow = flow;
        Hue = 2500;
    }

    public bool Axe(Mobile from, BaseAxe axe)
    {
        if (!Movable)
        {
            return false;
        }

        Effects.PlaySound(Location, Map, 0x3B3);
        from.SendLocalizedMessage(500461); // You destroy the item.
        Delete();
        return true;
    }

    public void Fill()
    {
        ItemID--;
        Hue = 0;
    }

    private void InvalidateIdFromFlow()
    {
        ItemID = _flow switch
        {
            Flow.NorthSouth      => 39911,
            Flow.SouthEastCorner => 39915,
            Flow.SouthWestCorner => 39919,
            Flow.EastWest        => 39924,
            Flow.NorthEastCorner => 39928,
            Flow.NorthWestCorner => 39932,
            _                    => ItemID
        };
    }

    private void InvalidateID()
    {
        switch (ItemID)
        {
            case 39911:
                _flow = Flow.NorthSouth;
                break;
            case 39915:
                _flow = Flow.SouthEastCorner;
                break;
            case 39919:
                _flow = Flow.SouthWestCorner;
                break;
            case 39924:
                _flow = Flow.EastWest;
                break;
            case 39928:
                _flow = Flow.NorthEastCorner;
                break;
            case 39932:
                _flow = Flow.NorthWestCorner;
                break;
            default:
                ItemID = 39911;
                InvalidateID();
                break;
        }
    }

    public bool Connects(ShadowguardCanal next)
    {
        var d = Utility.GetDirection(Location, next.Location);
        var f = next.Flow;

        return d switch
        {
            Direction.North => _flow switch
            {
                Flow.NorthSouth or Flow.SouthWestCorner or Flow.SouthEastCorner =>
                    f is Flow.NorthSouth or Flow.NorthEastCorner or Flow.NorthWestCorner,
                _ => false
            },
            Direction.South => _flow switch
            {
                Flow.NorthSouth or Flow.NorthWestCorner or Flow.NorthEastCorner =>
                    f is Flow.NorthSouth or Flow.SouthEastCorner or Flow.SouthWestCorner,
                _ => false
            },
            Direction.East => _flow switch
            {
                Flow.EastWest or Flow.NorthWestCorner or Flow.SouthWestCorner =>
                    f is Flow.EastWest or Flow.NorthEastCorner or Flow.SouthEastCorner,
                _ => false
            },
            Direction.West => _flow switch
            {
                Flow.EastWest or Flow.NorthEastCorner or Flow.SouthEastCorner =>
                    f is Flow.EastWest or Flow.NorthWestCorner or Flow.SouthWestCorner,
                _ => false
            },
            _ => false
        };
    }
}

[SerializationGenerator(0, false)]
public partial class ShadowguardSpigot : Item
{
    public override int LabelNumber => 1156275; // A Spigot

    [Constructible]
    public ShadowguardSpigot(int id) : base(id) => Movable = false;

    public override void OnDoubleClick(Mobile m)
    {
        if (m.InRange(Location, 2) && ItemID != 17294 && ItemID != 17278 &&
            ShadowguardController.GetEncounter(Location, Map) is FountainEncounter encounter)
        {
            encounter.UseSpigot(this, m);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ShadowguardDrain : Item
{
    public override int LabelNumber => 1156272; // A Drain

    [Constructible]
    public ShadowguardDrain() : base(0x9BFF)
    {
        Movable = false;
        Hue = 2500;
    }
}

[SerializationGenerator(0, false)]
public partial class MagicDrakeWing : BaseDecayingItem
{
    public override int Lifespan => 90;
    public override int LabelNumber => 1156233; // Magic Drake Wing

    [Constructible]
    public MagicDrakeWing() : base(0x1E85)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack) && ShadowguardController.GetEncounter(from.Location, from.Map) is BelfryEncounter encounter)
        {
            var p = encounter.SpawnPoints[1];
            encounter.ConvertOffset(ref p);
            BaseCreature.TeleportPets(from, p, from.Map);
            from.MoveToWorld(p, Map.TerMur);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class FeedingBell : BaseAddon
{
    public override int LabelNumber => 1156232; // Feeding Bell

    [Constructible]
    public FeedingBell()
    {
        AddComponent(new AddonComponent(38955), 0, 0, 0);
        AddComponent(new AddonComponent(38951), 1, 0, 0);
        AddComponent(new LocalizedAddonComponent(19548, 1156232), 0, 0, 0);

        AddComponent(new AddonComponent(3892), 0, 0, 0);
        AddComponent(new AddonComponent(3892), 1, 0, 0);
        AddComponent(new AddonComponent(3893), 0, 1, 0);
        AddComponent(new AddonComponent(3893), 0, 1, 0);
    }

    public override void OnComponentUsed(AddonComponent c, Mobile from)
    {
        if (!from.InRange(c.Location, 2) || c.ItemID != 19548)
        {
            return;
        }

        if (ShadowguardController.GetEncounter(c.Location, c.Map) is not BelfryEncounter { Drakes.Count: 0 } encounter)
        {
            return;
        }

        var toSpawn = 2 + encounter.PartySize() * 3;

        for (var i = 0; i < toSpawn; i++)
        {
            encounter.SpawnDrake(Location, from);
            Effects.PlaySound(Location, Map, 0x66C);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class WitheringBones : Container
{
    public override int LabelNumber => 1156214; // The Withered Bones of an Adventurer
    public override bool IsDecoContainer => false;

    [Constructible]
    public WitheringBones() : base(0xECF)
    {
        Movable = false;

        DropItem(new TatteredBook());
    }
}

[SerializationGenerator(0, false)]
public partial class TatteredBook : Item
{
    public override int LabelNumber => 1156215; // a tattered book

    [Constructible]
    public TatteredBook() : base(7712) => Movable = false;

    public override void OnDoubleClick(Mobile m) => m.SendGump(new TatteredBookGump());
}

public class TatteredBookGump : DynamicGump
{
    public TatteredBookGump() : base(100, 150)
    {
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 400, 300, 9380);

        /* I've finally found...this vile orchard is the key to Minax's enchantments!... days
        I've spent trapped within this tower, I dare not pick the fruit from the foliage for I
        know not what consequences may...Hunger is building...so hungry I must...Blech! Vile
        fruit!...What's this? For when I tossed this vile apple from whence it came a horrific
        beast appeared!...for I hope I can fight it...<br> */
        builder.AddHtmlLocalized(40, 45, 330, 200, 1156216, 1, false, true);
    }
}
