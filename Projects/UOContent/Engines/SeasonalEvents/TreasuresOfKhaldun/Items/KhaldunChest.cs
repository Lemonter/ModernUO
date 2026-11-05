using System;
using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Items/KhaldunChest.cs).
///     Dropped: <c>RunicReforging.GenerateRandomItem</c> and <c>TreasureMapChest.GetRandomItemStat</c> (neither
///     exists here — the rare drop is a plain random magic item, the same substitution the Doom port uses).
///     <c>IRevealableItem</c> comes from the VvV port. The refill timer is a <see cref="TimerExecutionToken" />
///     cancelled in <c>OnAfterDelete</c> and re-armed after deserialization.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KhaldunChest : LockableContainer, IRevealableItem
{
    private TimerExecutionToken _refillToken;

    public override int DefaultGumpID => 0x49;

    public bool CheckWhenHidden => true;

    [Constructible]
    public KhaldunChest() : base(Utility.RandomList(0xE3C, 0xE3E, 0x9A9))
    {
        Movable = false;
        Locked = true;
        Visible = false;

        Hue = 2745;
        LiftOverride = true;
        Weight = 0.0;

        LockLevel = 90;
        RequiredSkill = 90;
        MaxLockLevel = 100;

        TrapType = TrapType.PoisonTrap;
        TrapPower = 100;

        Timer.StartTimer(TimeSpan.FromSeconds(1), Fill);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        TryDelayedLock();
    }

    public void Fill()
    {
        Reset();

        for (var i = Items.Count - 1; i >= 0; i--)
        {
            Items[i].Delete();
        }

        for (var i = 0; i < Utility.RandomMinMax(6, 12); i++)
        {
            DropItem(Loot.RandomGem());
        }

        DropItem(new Gold(Utility.RandomMinMax(800, 1100)));

        Item item = null;

        if (0.30 > Utility.RandomDouble())
        {
            switch (Utility.Random(7))
            {
                case 0:
                    item = new Bandage(Utility.Random(10, 30));
                    break;
                case 1:
                    item = new SmokeBomb { Amount = Utility.Random(3, 6) };
                    break;
                case 2:
                    item = new InvisibilityPotion { Amount = Utility.Random(1, 3) };
                    break;
                case 3:
                    item = new Lockpick(Utility.Random(1, 10));
                    break;
                case 4:
                    item = new DreadHornMane(Utility.Random(1, 2));
                    break;
                case 5:
                    item = new Corruption(Utility.Random(1, 2));
                    break;
                case 6:
                    item = new Taint(Utility.Random(1, 2));
                    break;
            }

            DropItem(item);
        }

        if (0.25 > Utility.RandomDouble())
        {
            DropItem(new CounterfeitPlatinum());
        }

        if (0.2 > Utility.RandomDouble())
        {
            item = Utility.Random(3) switch
            {
                0 => new ZombiePainting(),
                1 => new SkeletonPortrait(),
                _ => new LichPainting()
            };

            DropItem(item);
        }

        if (0.1 > Utility.RandomDouble())
        {
            item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();

            if (item != null)
            {
                DropItem(item);
            }
        }
    }

    public void Reset()
    {
        EndTimer();

        Visible = false;
        Locked = true;

        RequiredSkill = 90;
        LockLevel = RequiredSkill - Utility.Random(1, 10);
        MaxLockLevel = RequiredSkill;

        TrapType = TrapType.MagicTrap;
        TrapPower = 100;
    }

    public virtual bool CheckReveal(Mobile m) => m.CheckTargetSkill(SkillName.DetectHidden, this, 80.0, 100.0);

    public virtual void OnRevealed(Mobile m)
    {
        Visible = true;
    }

    public virtual bool CheckPassiveDetect(Mobile m)
    {
        if (m.InRange(Location, 4))
        {
            var skill = (int)m.Skills[SkillName.DetectHidden].Value;

            if (skill >= 80 && Utility.Random(300) < skill)
            {
                return true;
            }
        }

        return false;
    }

    public override void LockPick(Mobile from)
    {
        TryDelayedLock();

        base.LockPick(from);
    }

    public void TryDelayedLock()
    {
        if (Locked || _refillToken.Running)
        {
            return;
        }

        EndTimer();

        Timer.StartTimer(TimeSpan.FromMinutes(Utility.RandomMinMax(10, 15)), Fill, out _refillToken);
    }

    private void EndTimer()
    {
        _refillToken.Cancel();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        EndTimer();
    }
}
