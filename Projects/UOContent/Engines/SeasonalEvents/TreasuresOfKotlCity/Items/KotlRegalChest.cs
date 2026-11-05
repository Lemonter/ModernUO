using System;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKotlCity/Items/RegalChest.cs).
///     Dropped: <c>RunicReforging.GenerateRandomItem</c>/<c>TreasureMapChest.GetRandomItemStat</c> (neither exists
///     here — the rare drop is a plain random magic item, as in the Doom and Khaldun ports) and the
///     <c>PointsSystem</c> season check, replaced by <see cref="KotlCityData.Enabled" />.
/// </summary>
[SerializationGenerator(0, false)]
public partial class KotlRegalChest : LockableContainer, IRevealableItem
{
    private TimerExecutionToken _refillToken;

    public bool CheckWhenHidden => true;

    [Constructible]
    public KotlRegalChest() : base(0x4D0C)
    {
        Movable = false;
        Locked = true;

        Hue = 2591;
        LiftOverride = true;

        RequiredSkill = 90;
        LockLevel = RequiredSkill - Utility.Random(1, 10);
        MaxLockLevel = RequiredSkill;
        TrapType = TrapType.MagicTrap;
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

        if (0.1 > Utility.RandomDouble())
        {
            DropItem(new StasisChamberPowerCore());
        }

        if (0.1 > Utility.RandomDouble())
        {
            DropItem(new CardOfSemidar((CardOfSemidar.CardType)Utility.RandomMinMax(0, 5)));
        }

        if (0.25 > Utility.RandomDouble())
        {
            DropItem(new InoperativeAutomatonHead());
        }

        if (0.1 > Utility.RandomDouble() && KotlCityData.Enabled)
        {
            var item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();

            if (item != null)
            {
                DropItem(item);
            }
        }

        if (0.25 > Utility.RandomDouble())
        {
            Item item = Utility.Random(8) switch
            {
                0 => new JournalDrSpector1(),
                1 => new JournalDrSpector2(),
                2 => new JournalDrSpector3(),
                3 => new JournalDrSpector4(),
                4 => new HistoryOfTheGreatWok1(),
                5 => new HistoryOfTheGreatWok2(),
                6 => new HistoryOfTheGreatWok3(),
                _ => new HistoryOfTheGreatWok4()
            };

            DropItem(item);
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

/// <summary>
///     Ported from ServUO (Scripts/Items/Functional/Automaton/BlackrockMoonstone.cs) — the Kotl reward list sells it,
///     so it rides along with this pack.
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackrockMoonstone : Item
{
    public override int LabelNumber => 1156993; // Black Moonstone

    [Constructible]
    public BlackrockMoonstone() : base(0x9CAA)
    {
        Hue = 1175;
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Items/Functional/Automaton/AutomatonActuator.cs) — a Kotl reward.
/// </summary>
[SerializationGenerator(0, false)]
public partial class AutomatonActuator : Item
{
    public override int LabelNumber => 1156997; // Automaton Actuator

    [Constructible]
    public AutomatonActuator() : base(0x9CE9)
    {
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Items/Functional/Automaton/InoperativeAutomatonHead.cs) — a Kotl chest drop.
/// </summary>
[SerializationGenerator(0, false)]
[Flippable(0x9DB1, 0x9DB2)]
public partial class InoperativeAutomatonHead : Item
{
    public override int LabelNumber => 1157002; // Inoperative Automaton Head

    [Constructible]
    public InoperativeAutomatonHead() : base(0x9DB1)
    {
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Quests/Eodon/MyrmidexThreat/Items/QuestItems.cs) — only the power core, because
///     the Kotl docking stations accept nothing else.
/// </summary>
[SerializationGenerator(0, false)]
public partial class StasisChamberPowerCore : Item
{
    public override int LabelNumber => 1156623;

    [Constructible]
    public StasisChamberPowerCore() : base(40155)
    {
    }
}
