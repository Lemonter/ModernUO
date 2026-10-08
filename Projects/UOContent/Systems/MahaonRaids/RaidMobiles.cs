using ModernUO.Serialization;

namespace Server.Mobiles;

// -- Bandit raid (base reward) --------------------------------------------------------------

[SerializationGenerator(0, false)]
public partial class RaidBandit : Brigand, Systems.MahaonRaids.IRaidSpawn
{
    public int RewardMultiplier => 1;

    [Constructible]
    public RaidBandit()
    {
        Title = "налётчик";
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(Systems.MahaonRaids.MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Pirate raid (base reward; placeholder reusing Brigand stats until a dedicated --
// -- Pirate creature/graphics set is built) -----------------------------------------

[SerializationGenerator(0, false)]
public partial class RaidPirate : Brigand, Systems.MahaonRaids.IRaidSpawn
{
    public int RewardMultiplier => 1;

    [Constructible]
    public RaidPirate()
    {
        Title = "пират-налётчик";
        Hue = 0x8A0; // bluish tint to visually distinguish from bandits until real art exists
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(Systems.MahaonRaids.MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Undead raid (slightly higher reward) -------------------------------------------

[SerializationGenerator(0, false)]
public partial class RaidSkeleton : Skeleton, Systems.MahaonRaids.IRaidSpawn
{
    public int RewardMultiplier => 2;

    [Constructible]
    public RaidSkeleton()
    {
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(Systems.MahaonRaids.MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

[SerializationGenerator(0, false)]
public partial class RaidZombie : Zombie, Systems.MahaonRaids.IRaidSpawn
{
    public int RewardMultiplier => 2;

    [Constructible]
    public RaidZombie()
    {
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(Systems.MahaonRaids.MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Orc mega-raid ("hundreds of orcs under cover") — base reward, but sheer numbers --
// -- are the point ------------------------------------------------------------------

[SerializationGenerator(0, false)]
public partial class RaidOrc : Orc, Systems.MahaonRaids.IRaidSpawn
{
    public int RewardMultiplier => 1;

    [Constructible]
    public RaidOrc()
    {
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(Systems.MahaonRaids.MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Dragon raid (x20 reward, per Mahaon's original rule) ---------------------------

[SerializationGenerator(0, false)]
public partial class RaidDragon : Dragon, Systems.MahaonRaids.IRaidSpawn
{
    public int RewardMultiplier => 20;

    [Constructible]
    public RaidDragon()
    {
        Title = "дракон-налётчик";
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(Systems.MahaonRaids.MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Necromancer raid (summons more undead mid-fight via MahaonNecroSummonerAI — those --
// -- summons count as raid-related too, see MahaonRaidCombat.IsRaidRelated) ---------

[SerializationGenerator(0, false)]
public partial class RaidLich : Lich, Systems.MahaonRaids.IRaidSpawn
{
    public int RewardMultiplier => 3;

    [Constructible]
    public RaidLich()
    {
        Title = "некромант-налётчик";
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(Systems.MahaonRaids.MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}
