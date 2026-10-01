using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Regions;

namespace Server.Items;

/// <summary>A cache hidden in the Exodus dungeon. Ported from ServUO
/// (Scripts/Services/Revamped Dungeons/TheExodusEncounter/Items/ExodusChest.cs).
///
/// It is invisible until someone with 98 Detect Hidden walks within three tiles of it; from the
/// moment it is found it lasts five minutes and then vanishes with whatever is left inside.
/// Its own region tells anyone with the skill, standing within two tiles, that something is
/// there — which is the only hint the dungeon gives.
///
/// <see cref="RitualItems" /> is empty for now. It should hold the four summoning items — the
/// rite, the sacrificial dagger, the robe of rite and the summoning altar — but those are the
/// entry to the Exodus encounter proper (the altar builds it, the rite and dagger drive it),
/// and that encounter is not ported yet. The hook is here and every Exodus creature already
/// calls it; when the ritual chain lands, the four types go in this one array and the whole
/// drop chain starts working. Handing out the items now, with nothing to use them on, would be
/// worse than leaving them out.</summary>
[SerializationGenerator(0, false)]
public partial class ExodusChest : DecorativeBox, IRevealableItem
{
    private TimerExecutionToken _deleteTimer;
    private ExodusChestRegion _region;

    [Constructible]
    public ExodusChest()
    {
        Visible = false;
        Movable = false;

        Locked = true;
        LockLevel = 90;
        RequiredSkill = 90;
        MaxLockLevel = 100;

        Weight = 0.0;
        Hue = 2700;

        TrapType = TrapType.PoisonTrap;
        TrapPower = 100;

        GenerateTreasure();
    }

    /// <summary>The four summoning items, once the Exodus encounter is ported. See the class
    /// note above.</summary>
    public static Type[] RitualItems { get; } = Type.EmptyTypes;

    public override int DefaultGumpID => 0x10C;

    public override bool IsDecoContainer => false;

    public bool CheckWhenHidden => true;

    public bool CheckReveal(Mobile m) => m.InRange(Location, 3) && m.Skills.DetectHidden.Value >= 98.0;

    public void OnRevealed(Mobile m)
    {
        Visible = true;
        StartDeleteTimer();
    }

    public virtual bool CheckPassiveDetect(Mobile m)
    {
        if (!m.InRange(Location, 4))
        {
            return false;
        }

        var skill = (int)m.Skills.DetectHidden.Value;

        return skill >= 80 && Utility.Random(300) < skill;
    }

    public void StartDeleteTimer()
    {
        if (RitualItems.Length > 0 && Utility.RandomDouble() < 0.2)
        {
            var item = RitualItems.RandomElement().CreateInstance<Item>();

            if (item != null)
            {
                DropItem(item);
            }
        }

        _deleteTimer.Cancel();
        Timer.StartTimer(TimeSpan.FromMinutes(5), Delete, out _deleteTimer);
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        if (!Deleted)
        {
            UpdateRegion();
        }
    }

    public override void OnMapChange()
    {
        if (!Deleted)
        {
            UpdateRegion();
        }
    }

    public void UpdateRegion()
    {
        _region?.Unregister();
        _region = null;

        if (Deleted || Map == null || Map == Map.Internal)
        {
            return;
        }

        _region = new ExodusChestRegion(this);
        _region.Register();
    }

    public override void OnAfterDelete()
    {
        _deleteTimer.Cancel();

        _region?.Unregister();
        _region = null;

        base.OnAfterDelete();
    }

    protected virtual void GenerateTreasure()
    {
        DropItem(new Gold(1500, 3000));

        foreach (var gemType in Loot.GemTypes)
        {
            var gem = gemType.CreateInstance<Item>();

            if (gem != null)
            {
                gem.Amount = Utility.RandomMinMax(1, 6);
                DropItem(gem);
            }
        }

        // Same as the potions below: the original passes a count to a constructor that has no
        // amount parameter. Smoke bombs do stack under ML, so this is one stack of three to six.
        if (Utility.RandomDouble() < 0.25)
        {
            var bombs = new SmokeBomb();

            if (bombs.Stackable)
            {
                bombs.Amount = Utility.RandomMinMax(3, 6);
                DropItem(bombs);
            }
            else
            {
                DropItem(bombs);

                for (var i = Utility.RandomMinMax(2, 5); i > 0; i--)
                {
                    DropItem(new SmokeBomb());
                }
            }
        }

        // The original asks for one to three potions by passing the count to the constructor,
        // but neither potion is stackable or takes an amount — in ServUO that int binds to the
        // deserialization constructor's Serial and one broken bottle is dropped. One to three
        // separate bottles is what was meant.
        if (Utility.RandomDouble() < 0.25)
        {
            var bottles = Utility.RandomMinMax(1, 3);
            var parasitic = Utility.RandomBool();

            for (var i = 0; i < bottles; i++)
            {
                DropItem(parasitic ? new ParasiticPotion() : new InvisibilityPotion());
            }
        }

        if (Utility.RandomDouble() < 0.2)
        {
            var essence = Loot.RandomEssence();

            if (essence != null)
            {
                essence.Amount = Utility.RandomMinMax(3, 6);
                DropItem(essence);
            }
        }

        if (Utility.RandomDouble() < 0.1)
        {
            DropItem(
                Utility.Random(4) switch
                {
                    0 => new Taint(),
                    1 => new Corruption(),
                    2 => new Blight(),
                    _ => (Item)new LuminescentFungi()
                }
            );
        }
    }

    /// <summary>Every Exodus creature calls this on death, one kill in ten. Silent while the
    /// ritual chain is unported — see the class note.</summary>
    public static void GiveRitualItem(Mobile m)
    {
        if (RitualItems.Length == 0)
        {
            return;
        }

        var item = RitualItems.RandomElement().CreateInstance<Item>();

        if (item == null)
        {
            return;
        }

        m.PlaySound(0x5B4);
        m.AddToBackpack(item);
        m.SendLocalizedMessage(1072223); // An item has been placed in your backpack.
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        // A chest that was already opened is spent.
        if (!Locked)
        {
            Delete();
            return;
        }

        Timer.StartTimer(UpdateRegion);
    }
}

/// <summary>The five-by-five patch around a hidden chest, which nudges anyone skilled enough to
/// notice it.</summary>
public class ExodusChestRegion : BaseRegion
{
    public ExodusChestRegion(ExodusChest chest) : base(
        null,
        chest.Map,
        Find(chest.Location, chest.Map),
        new Rectangle2D(chest.Location.X - 2, chest.Location.Y - 2, 5, 5)
    ) => ExodusChest = chest;

    public ExodusChest ExodusChest { get; }

    public override void OnEnter(Mobile m)
    {
        if (ExodusChest?.Visible == false && m is PlayerMobile && m.Skills.DetectHidden.Value >= 98.0)
        {
            m.SendLocalizedMessage(1153493); // Your keen senses detect something hidden in the area...
        }
    }
}
