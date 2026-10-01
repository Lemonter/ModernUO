using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Despise;

public enum LeashLength
{
    Short,
    Long
}

public enum Aggression
{
    Defensive,
    Aggressive
}

/// <summary>
///     Ported from ServUO's Despise Revamped dungeon (Scripts/Items/Quest/WispOrb.cs) — the
///     item players use to possess/control a DespiseCreature. Properties with side effects in
///     their original setters (Pet/LeashLength/Aggression/Alignment/Conscripted) use
///     [SerializableProperty] instead of [SerializableField] — that attribute lets the
///     generator persist a hand-written property's backing field while leaving the
///     getter/setter body (and its side effects) exactly as written, rather than the plain
///     get/set [SerializableField] generates. Anchor (IEntity — a Mobile or an Item) isn't
///     directly serializable, so it's a computed property over two concrete backing fields
///     instead (_anchorMobile/_anchorItem), mirroring the original's own Serialize/Deserialize
///     discriminator by hand.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WispOrb : Item
{
    public override int LabelNumber => 1153273; // A Wisp Orb

    private const int MinPowerToConscript = 4;

    [SerializableField(0)]
    private Mobile _owner;

    [SerializableProperty(1)]
    [CommandProperty(AccessLevel.GameMaster)]
    public DespiseCreature Pet
    {
        get => _pet;
        set
        {
            if (_pet != null && value == null)
            {
                _pet.Unlink();
            }
            else
            {
                _pet = value;
                _pet?.Link(this);
            }

            this.MarkDirty();
            InvalidateHue();
            InvalidateProperties();
        }
    }

    [SerializableProperty(2)]
    [CommandProperty(AccessLevel.GameMaster)]
    public LeashLength LeashLength
    {
        get => _leashLength;
        set
        {
            _leashLength = value;
            this.MarkDirty();
            InvalidateHue();
            InvalidateProperties();
        }
    }

    [SerializableProperty(3)]
    [CommandProperty(AccessLevel.GameMaster)]
    public Aggression Aggression
    {
        get => _aggression;
        set
        {
            _aggression = value;
            this.MarkDirty();
            InvalidateHue();
            InvalidateProperties();
        }
    }

    [SerializableProperty(4)]
    [CommandProperty(AccessLevel.GameMaster)]
    public Alignment Alignment
    {
        get => _alignment;
        set
        {
            _alignment = value;
            this.MarkDirty();
            InvalidateProperties();
        }
    }

    [SerializableField(5)]
    private Mobile _anchorMobile;

    [SerializableField(6)]
    private Item _anchorItem;

    [CommandProperty(AccessLevel.GameMaster)]
    public IEntity Anchor
    {
        get => (IEntity)_anchorMobile ?? _anchorItem;
        set
        {
            _anchorMobile = value as Mobile;
            _anchorItem = value as Item;

            if (_pet != null && value == null)
            {
                _anchorMobile = _owner;
                _pet.Home = GetAnchorLocation();
            }

            InvalidateProperties();
        }
    }

    [SerializableProperty(7)]
    [CommandProperty(AccessLevel.GameMaster)]
    public bool Conscripted
    {
        get => _conscripted;
        set
        {
            _conscripted = value;
            this.MarkDirty();

            if (_conscripted && DespiseController.Instance is { Sequencing: true })
            {
                DespiseController.Instance.TryAddToArmy(this);
            }
        }
    }

    [Constructible]
    public WispOrb(Mobile owner, Alignment alignment) : base(8448)
    {
        _owner = owner;
        LootType = LootType.Blessed;
        _alignment = alignment;

        _orbs.Add(this);
        InvalidateHue();
    }

    public void OnUnlinkPet()
    {
        _pet = null;
        _anchorMobile = null;
        _anchorItem = null;
        _aggression = Aggression.Aggressive;
        InvalidateProperties();
    }

    public bool CheckOwnerAlignment()
    {
        if (_owner == null || _owner.Karma > 0 && _alignment != Alignment.Good || _owner.Karma < 0 && _alignment != Alignment.Evil)
        {
            _owner?.SendLocalizedMessage(1153313); // You are no longer aligned with your Wisp Orb. It dissolves into aether!
            Delete();
            return false;
        }

        return true;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (CheckOwnerAlignment() && IsChildOf(from.Backpack) && from == _owner)
        {
            var cliloc = _pet == null ? 1153274 : 1153277;
            from.SendLocalizedMessage(cliloc); // Target a creature to possess. / Target an object or creature to set the anchor. Target the Wisp Orb to change the leash setting. Target the possessed creature to change its aggression.
            from.Target = new InternalTarget(this);
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new ReleaseEntry(from, this));
        list.Add(new ConscriptEntry(from, this));
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1153329, $"#{GetAlignment()}"); // Alignment: ~1_VAL~
        list.Add(1153306, $"{GetArmyPower()}"); // Army Power: ~1_VAL~
        list.Add(1153272, _pet != null ? _pet.Name : "None"); // Controlling: ~1_VAL~

        var name = GetAnchorName();

        if (name != null)
        {
            if (name is int cliloc)
            {
                list.Add(1153265, $"#{cliloc}"); // Anchor: ~1_NAME~
            }
            else if (name is string str)
            {
                list.Add(1153265, str);
            }
        }

        var leash = 1153262 + (int)_leashLength;
        var aggr = 1153269 + (int)_aggression;

        list.Add(1153260, $"#{leash}"); // Leash: ~1_VAL~
        list.Add(1153267, $"#{aggr}"); // Aggression: ~1_VAL~
    }

    public override bool DropToWorld(Mobile m, Point3D p)
    {
        m.SendLocalizedMessage(1153233); // The Wisp Orb vanishes to whence it came...
        Delete();
        return false;
    }

    public static void CheckDrop(Container c, Mobile m)
    {
        foreach (var orb in c.Items.OfType<WispOrb>().ToList())
        {
            m.SendLocalizedMessage(1153233); // The Wisp Orb vanishes to whence it came...
            orb.Delete();
        }
    }

    public override bool OnDroppedInto(Mobile from, Container target, Point3D p)
    {
        if (target.RootParent == from)
        {
            return base.OnDroppedInto(from, target, p);
        }

        from.SendLocalizedMessage(1153233); // The Wisp Orb vanishes to whence it came...
        Delete();
        return false;
    }

    public override bool OnDroppedOnto(Mobile from, Item target)
    {
        if (target is Container && target.RootParent != from)
        {
            from.SendLocalizedMessage(1153233); // The Wisp Orb vanishes to whence it came...
            Delete();
            return false;
        }

        return base.OnDroppedOnto(from, target);
    }

    public Point3D GetAnchorLocation()
    {
        if (_pet == null)
        {
            return Point3D.Zero;
        }

        var anchor = Anchor ?? (_pet.ControlMaster as IEntity);

        return anchor switch
        {
            Item { HeldBy: not null } heldItem => heldItem.HeldBy.Location,
            Item item                          => item.GetWorldLocation(),
            not null                           => anchor.Location,
            _                                  => Point3D.Zero
        };
    }

    public IPoint3D GetAnchorActual()
    {
        if (_pet == null)
        {
            return null;
        }

        var anchor = Anchor ?? (_pet.ControlMaster as IEntity);

        if (anchor is Item { RootParent: not null } item)
        {
            return item.RootParent;
        }

        return anchor;
    }

    private class ConscriptEntry : ContextMenuEntry
    {
        private readonly Mobile _from;
        private readonly WispOrb _orb;

        public ConscriptEntry(Mobile from, WispOrb orb) : base(1153285, -1) // Conscript
        {
            _from = from;
            _orb = orb;

            if (_orb.Pet == null || _orb.Conscripted || _orb.Pet.Alignment != _orb.Alignment)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_orb.Pet != null && _orb.IsChildOf(_from.Backpack) && !_orb.Conscripted && _orb.Pet.Alignment == _orb.Alignment)
            {
                if (_orb.Pet.Power < MinPowerToConscript)
                {
                    _from.SendLocalizedMessage(1153311); // The creature under control of your Wisp Orb cannot be conscripted at this time.
                }
                else
                {
                    _from.SendLocalizedMessage(1153310); // The creature you are controlling will now fight with you when the Call to Arms sounds. If you do not wish this, then release control of it.
                    _orb.Conscripted = true;
                }
            }
        }
    }

    private class ReleaseEntry : ContextMenuEntry
    {
        private readonly Mobile _from;
        private readonly WispOrb _orb;

        public ReleaseEntry(Mobile from, WispOrb orb) : base(1153284, -1) // Release
        {
            _from = from;
            _orb = orb;

            if (_orb.Pet == null)
            {
                Flags |= CMEFlags.Disabled;
            }
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            _orb.Pet?.Unlink();
        }
    }

    private class InternalTarget : Target
    {
        private readonly WispOrb _orb;

        public InternalTarget(WispOrb orb) : base(8, true, TargetFlags.None)
        {
            _orb = orb;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is BaseCreature)
            {
                var creature = targeted as DespiseCreature;

                if (creature == null)
                {
                    from.SendLocalizedMessage(1153286); // That cannot be possessed by a Wisp Orb.
                }
                else if (_orb.Pet == null)
                {
                    if (((BaseCreature)targeted).Controlled)
                    {
                        from.SendLocalizedMessage(1153287); // That creature is already under the control of a Wisp Orb.
                    }
                    else if (creature.Power > 5)
                    {
                        from.SendLocalizedMessage(1153336); // That creature is too powerful for you to coerce.
                    }
                    else
                    {
                        _orb.Anchor = from;

                        _orb.Pet = creature;
                        creature.Link(_orb);

                        _orb.Pet.SetControlMaster(from);
                        _orb.Pet.ControlTarget = from;
                        _orb.Pet.ControlOrder = OrderType.Follow;

                        from.SendLocalizedMessage(1153276); // Your Wisp Orb takes control of the creature!
                        _orb.Pet.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1153295, from.Name); // * This creature is now under the control of ~1_NAME~ *
                    }
                }
                else if (targeted == _orb.Pet)
                {
                    var aggr = (int)_orb.Aggression + 1;

                    if (aggr >= 2)
                    {
                        aggr = 0;
                    }

                    _orb.Aggression = (Aggression)aggr;

                    from.SendLocalizedMessage(1153279, _orb.Aggression.ToString()); // Your possessed creature's aggression level is now: ~1_VAL~
                }
                else
                {
                    _orb.TrySetAnchor(from, (BaseCreature)targeted);
                }
            }
            else if (targeted == _orb)
            {
                var length = (int)_orb.LeashLength + 1;

                if (length >= 2)
                {
                    length = 0;
                }

                _orb.LeashLength = (LeashLength)length;

                from.SendLocalizedMessage(1153278, _orb.LeashLength.ToString()); // Your possessed creature's leash is now: ~1_VAL~
            }
            else if (targeted is IPoint3D p3D && _orb.Pet != null)
            {
                _orb.TrySetAnchor(from, p3D);
            }
        }
    }

    private object GetAnchorName()
    {
        return Anchor switch
        {
            null                => "None",
            Mobile mobile       => mobile.Name,
            Item { Name: not null } item => item.Name,
            Item item           => item.LabelNumber,
            StaticTarget target => $"{target.Name} {target.Location}",
            LandTarget target   => $"{target.Name} {target.Location}",
            _                   => new Point3D(Anchor).ToString()
        };
    }

    public void TrySetAnchor(Mobile from, IPoint3D p)
    {
        if (!CheckOwnerAlignment() || from != _owner)
        {
            return;
        }

        if (p is Mobile m)
        {
            Anchor = m;
            from.SendLocalizedMessage(1153280, m == _owner ? "You!" : m.Name + ".");

            _pet.ControlTarget = m;
            _pet.ControlOrder = OrderType.Follow;
        }

        if (p is Item item)
        {
            Anchor = item;

            var name = GetAnchorName(); // Your possessed creature is now anchored to ~1_NAME~

            if (name is int cliloc)
            {
                from.SendLocalizedMessage(1153280, $"#{cliloc}");
            }
            else if (name is string str)
            {
                from.SendLocalizedMessage(1153280, str);
            }

            _pet.ControlTarget = _pet.ControlMaster;
            _pet.ControlOrder = OrderType.Follow;
        }
    }

    private int GetAlignment() => _alignment switch
    {
        Alignment.Good => 1153330,
        Alignment.Evil => 1153331,
        _              => -1
    };

    public void InvalidateHue()
    {
        if (_pet == null)
        {
            Hue = 1910; // shadow wisp color
        }
        else if (_pet.Combatant != null)
        {
            Hue = 1931; // Orange
        }
        else if (IsFollowing())
        {
            Hue = 1912;
        }
        else
        {
            Hue = _aggression switch
            {
                Aggression.Defensive  => 1917, // blue
                Aggression.Aggressive => 1914, // green
                _                     => Hue
            };
        }
    }

    public bool IsFollowing() =>
        (int)_pet.GetDistanceToSqrt(GetAnchorLocation()) > _pet.GetLeashLength() + 1 && _pet.ControlOrder == OrderType.Follow;

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _orbs.Remove(this);

        if (_pet is { Alive: true })
        {
            _pet.Unlink(false);
        }
    }

    public int GetArmyPower()
    {
        if (_pet == null)
        {
            return 0;
        }

        var power = _pet.Power;
        return power * power;
    }

    public static void TeleportPet(Mobile owner)
    {
        if (owner?.Backpack == null)
        {
            return;
        }

        if (owner.Backpack.FindItemByType(typeof(WispOrb)) is WispOrb { Pet: not null } orb)
        {
            orb.Pet.MoveToWorld(owner.Location, owner.Map);
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _orbs.Add(this);

        if (_anchorMobile == null && _anchorItem == null && _pet != null)
        {
            Anchor = _owner;
        }
    }

    private static readonly List<WispOrb> _orbs = new();
    public static List<WispOrb> Orbs => _orbs;
}
