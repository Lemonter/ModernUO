using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Items;

// Ported from real OSI/ServUO content (Scripts/Items/Addons/FountainOfFortune.cs) — a
// standalone SA-era decoration near the Maze of Death, unrelated to either Underworld
// puzzle. Toss a coin in for a 20% chance at a rare drop or an 80% chance at one of several
// hour-long buffs, and it offers free resurrection (10-minute re-prompt cooldown) to anyone
// dead standing nearby.
//
// Two of the 4 real item rewards (SolesOfProvidence, GemologistsSatchel) don't exist in this
// codebase — dropped, leaving RelicFragment/EnchantedEssence (same substitution already used
// for the Maze of Death's own reward table, see UnderworldPuzzleItem). Real upstream bug
// fixed: the buff branch's `Utility.Random(4)` only ever picked among the first 4 of its 6
// switch cases — the special-protection and balm-boost buffs (cases 4 and 5) were dead code,
// unreachable — changed to `Utility.Random(6)` so all 6 are actually selectable.
//
// ModernUO's ResurrectGump has no result-callback parameter (unlike ServUO's), so instead of
// setting the 10-minute re-prompt cooldown from the player's answer, it's set at the moment
// the gump is sent — same practical effect (don't re-spam the prompt), simpler trigger point.
// LuckTable/SpecialProtection/BalmBoost are exposed as public static queries exactly like the
// original but — matching the original's own scope, per its own "standalone, self-contained"
// framing — aren't wired into this codebase's loot/combat/poison systems; a later pass can
// hook GetLuckBonus/UnderProtection/BalmBoost into those if wanted.
[SerializationGenerator(0, false)]
public partial class FountainOfFortune : BaseAddon
{
    private const int LuckBonus = 400;

    private static readonly List<FountainOfFortune> Fountains = new();
    private static readonly Dictionary<Mobile, DateTime> LuckTable = new();
    private static readonly Dictionary<Mobile, DateTime> SpecialProtectionTable = new();
    private static readonly Dictionary<Mobile, DateTime> BalmBoostTable = new();

    private static Timer _defragTimer;

    [SerializableField(0)]
    private Dictionary<Mobile, DateTime> _resCooldown = new();

    [SerializableField(1)]
    private Dictionary<Mobile, DateTime> _rewardCooldown = new();

    public override bool HandlesOnMovement => true;

    [Constructible]
    public FountainOfFortune()
    {
        var itemId = 0x1731;

        AddComponent(new AddonComponent(itemId++), -2, +1, 0);
        AddComponent(new AddonComponent(itemId++), -1, +1, 0);
        AddComponent(new AddonComponent(itemId++), +0, +1, 0);
        AddComponent(new AddonComponent(itemId++), +1, +1, 0);

        AddComponent(new AddonComponent(itemId++), +1, +0, 0);
        AddComponent(new AddonComponent(itemId++), +1, -1, 0);
        AddComponent(new AddonComponent(itemId++), +1, -2, 0);

        AddComponent(new AddonComponent(itemId++), +0, -2, 0);
        AddComponent(new AddonComponent(itemId++), +0, -1, 0);
        AddComponent(new AddonComponent(itemId++), +0, +0, 0);

        AddComponent(new AddonComponent(itemId++), -1, +0, 0);
        AddComponent(new AddonComponent(itemId++), -2, +0, 0);

        AddComponent(new AddonComponent(itemId++), -2, -1, 0);
        AddComponent(new AddonComponent(itemId++), -1, -1, 0);

        AddComponent(new AddonComponent(itemId++), -1, -2, 0);
        AddComponent(new AddonComponent(++itemId), -2, -2, 0);

        Movable = false;

        AddFountain(this);
    }

    public bool OnTarget(Mobile from, Item coin)
    {
        DefragTables();

        if (IsCoolingDown(from))
        {
            from.SendLocalizedMessage(1113368); // You already made a wish today. Try again tomorrow!
            return false;
        }

        if (0.20 >= Utility.RandomDouble())
        {
            Item item = Utility.Random(2) switch
            {
                0 => new RelicFragment(5),
                _ => new EnchantedEssence(5)
            };

            if (from.Backpack == null || !from.Backpack.TryDropItem(from, item, false))
            {
                item.MoveToWorld(from.Location, from.Map);
            }
        }
        else
        {
            switch (Utility.Random(6))
            {
                case 0:
                    from.AddStatMod(new StatMod(StatType.Str, "FoF_Str", 10, TimeSpan.FromMinutes(60)));
                    from.SendLocalizedMessage(1113373); // You suddenly feel stronger!
                    break;
                case 1:
                    from.AddStatMod(new StatMod(StatType.Dex, "FoF_Dex", 10, TimeSpan.FromMinutes(60)));
                    from.SendLocalizedMessage(1113374); // You suddenly feel more agile!
                    break;
                case 2:
                    from.AddStatMod(new StatMod(StatType.Int, "FoF_Int", 10, TimeSpan.FromMinutes(60)));
                    from.SendLocalizedMessage(1113371); // You suddenly feel wiser!
                    break;
                case 3:
                    LuckTable[from] = Core.Now + TimeSpan.FromMinutes(60);
                    from.SendLocalizedMessage(1079551); // Your luck just improved!
                    break;
                case 4:
                    SpecialProtectionTable[from] = Core.Now + TimeSpan.FromMinutes(60);
                    from.SendLocalizedMessage(1113375); // You suddenly feel less vulnerable!
                    break;
                case 5:
                    BalmBoostTable[from] = Core.Now + TimeSpan.FromMinutes(60);
                    from.SendLocalizedMessage(1113372); // The duration of your balm has been increased by an hour!
                    break;
            }

            from.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
        }

        from.PlaySound(0x22);

        _rewardCooldown[from] = Core.Now + TimeSpan.FromHours(24);

        if (coin.Amount <= 1)
        {
            coin.Delete();
        }
        else
        {
            coin.Amount--;
        }

        return false;
    }

    public bool IsCoolingDown(Mobile from)
    {
        foreach (var fountain in Fountains)
        {
            if (fountain._rewardCooldown.ContainsKey(from))
            {
                return true;
            }
        }

        return false;
    }

    public static int GetLuckBonus(Mobile from) => LuckTable.ContainsKey(from) ? LuckBonus : 0;

    public static bool UnderProtection(Mobile m) => SpecialProtectionTable.ContainsKey(m);

    public static bool HasBalmBoost(Mobile m) => BalmBoostTable.ContainsKey(m);

    public bool CanRes(Mobile m)
    {
        if (!_resCooldown.TryGetValue(m, out var until))
        {
            return true;
        }

        if (until >= Core.Now)
        {
            return false;
        }

        _resCooldown.Remove(m);
        return true;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (m.Player && CanRes(m) && !m.Alive && m.InRange(Location, 5))
        {
            _resCooldown[m] = Core.Now + TimeSpan.FromMinutes(10);
            m.SendGump(new ResurrectGump(m));
        }
    }

    public static void DefragTables()
    {
        foreach (var fountain in Fountains)
        {
            fountain.DefragOwnTables();
        }

        DefragBuffTable(LuckTable, m => m.NetState != null ? 1079552 : 0); // Your luck just ran out.
        DefragBuffTable(SpecialProtectionTable, null);
        DefragBuffTable(BalmBoostTable, null);
    }

    private void DefragOwnTables()
    {
        List<Mobile> remove = null;

        foreach (var kvp in _resCooldown)
        {
            if (kvp.Value < Core.Now)
            {
                (remove ??= new List<Mobile>()).Add(kvp.Key);
            }
        }

        if (remove != null)
        {
            foreach (var m in remove)
            {
                _resCooldown.Remove(m);
            }
        }

        remove = null;

        foreach (var kvp in _rewardCooldown)
        {
            if (kvp.Value < Core.Now)
            {
                (remove ??= new List<Mobile>()).Add(kvp.Key);
            }
        }

        if (remove == null)
        {
            return;
        }

        foreach (var m in remove)
        {
            _rewardCooldown.Remove(m);
        }
    }

    private static void DefragBuffTable(Dictionary<Mobile, DateTime> table, Func<Mobile, int> expireMessage)
    {
        List<Mobile> remove = null;

        foreach (var kvp in table)
        {
            if (kvp.Value < Core.Now)
            {
                (remove ??= new List<Mobile>()).Add(kvp.Key);
            }
        }

        if (remove == null)
        {
            return;
        }

        foreach (var m in remove)
        {
            table.Remove(m);

            var cliloc = expireMessage?.Invoke(m) ?? 0;

            if (cliloc > 0)
            {
                m.SendLocalizedMessage(cliloc);
            }
        }
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        RemoveFountain(this);

        _resCooldown.Clear();
        _rewardCooldown.Clear();
    }

    public static void AddFountain(FountainOfFortune fountain)
    {
        if (Fountains.Contains(fountain))
        {
            return;
        }

        Fountains.Add(fountain);
        StartTimer();
    }

    public static void RemoveFountain(FountainOfFortune fountain)
    {
        Fountains.Remove(fountain);

        if (Fountains.Count == 0 && _defragTimer != null)
        {
            _defragTimer.Stop();
            _defragTimer = null;
        }
    }

    public static void StartTimer()
    {
        if (_defragTimer?.Running == true)
        {
            return;
        }

        _defragTimer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), DefragTables);
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _resCooldown ??= new Dictionary<Mobile, DateTime>();
        _rewardCooldown ??= new Dictionary<Mobile, DateTime>();

        AddFountain(this);
    }
}
