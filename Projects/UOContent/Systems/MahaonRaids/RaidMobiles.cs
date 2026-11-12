using ModernUO.Serialization;
using Server.Systems.MahaonRaids;

namespace Server.Mobiles;

// Все налётчики ведут себя одинаково (см. RaidDirector/MahaonRaid): идут по маршруту набега,
// когда ни с кем не дерутся; бьют стражу у ворот, пока не начался прорыв; работают и без
// игроков рядом — набег идёт, даже если на него никто не смотрит.

// -- Пиратские бароны ---------------------------------------------------------------------

[SerializationGenerator(0, false)]
public partial class RaidBandit : Brigand, IRaidSpawn
{
    public int RewardMultiplier => 1;

    [Constructible]
    public RaidBandit()
    {
        Title = "налётчик";
    }

    public override bool PlayerRangeSensitive => false;
    public override void OnActionWander() => RaidDirector.OnRaiderWander(this);
    public override bool IsEnemy(Mobile m) => MahaonRaidCombat.RaiderIsEnemy(this, m) ?? base.IsEnemy(m);

    public override void OnDelete()
    {
        RaidDirector.OnRaiderDeleted(this);
        base.OnDelete();
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

[SerializationGenerator(0, false)]
public partial class RaidPirate : Brigand, IRaidSpawn
{
    public int RewardMultiplier => 1;

    [Constructible]
    public RaidPirate()
    {
        Title = "пират-налётчик";
        Hue = 0x8A0;
    }

    public override bool PlayerRangeSensitive => false;
    public override void OnActionWander() => RaidDirector.OnRaiderWander(this);
    public override bool IsEnemy(Mobile m) => MahaonRaidCombat.RaiderIsEnemy(this, m) ?? base.IsEnemy(m);

    public override void OnDelete()
    {
        RaidDirector.OnRaiderDeleted(this);
        base.OnDelete();
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Некромант ------------------------------------------------------------------------------

[SerializationGenerator(0, false)]
public partial class RaidSkeleton : Skeleton, IRaidSpawn
{
    public int RewardMultiplier => 2;

    [Constructible]
    public RaidSkeleton()
    {
    }

    public override bool PlayerRangeSensitive => false;
    public override void OnActionWander() => RaidDirector.OnRaiderWander(this);
    public override bool IsEnemy(Mobile m) => MahaonRaidCombat.RaiderIsEnemy(this, m) ?? base.IsEnemy(m);

    public override void OnDelete()
    {
        RaidDirector.OnRaiderDeleted(this);
        base.OnDelete();
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

[SerializationGenerator(0, false)]
public partial class RaidZombie : Zombie, IRaidSpawn
{
    public int RewardMultiplier => 2;

    [Constructible]
    public RaidZombie()
    {
    }

    public override bool PlayerRangeSensitive => false;
    public override void OnActionWander() => RaidDirector.OnRaiderWander(this);
    public override bool IsEnemy(Mobile m) => MahaonRaidCombat.RaiderIsEnemy(this, m) ?? base.IsEnemy(m);

    public override void OnDelete()
    {
        RaidDirector.OnRaiderDeleted(this);
        base.OnDelete();
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

[SerializationGenerator(0, false)]
public partial class RaidLich : Lich, IRaidSpawn
{
    public int RewardMultiplier => 3;

    [Constructible]
    public RaidLich()
    {
        Title = "некромант-налётчик";
    }

    public override bool PlayerRangeSensitive => false;
    public override void OnActionWander() => RaidDirector.OnRaiderWander(this);
    public override bool IsEnemy(Mobile m) => MahaonRaidCombat.RaiderIsEnemy(this, m) ?? base.IsEnemy(m);

    public override void OnDelete()
    {
        RaidDirector.OnRaiderDeleted(this);
        base.OnDelete();
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Орда -----------------------------------------------------------------------------------

[SerializationGenerator(0, false)]
public partial class RaidOrc : Orc, IRaidSpawn
{
    public int RewardMultiplier => 1;

    [Constructible]
    public RaidOrc()
    {
    }

    public override bool PlayerRangeSensitive => false;
    public override void OnActionWander() => RaidDirector.OnRaiderWander(this);
    public override bool IsEnemy(Mobile m) => MahaonRaidCombat.RaiderIsEnemy(this, m) ?? base.IsEnemy(m);

    public override void OnDelete()
    {
        RaidDirector.OnRaiderDeleted(this);
        base.OnDelete();
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Дракон: в расписание набегов не входит, остался для [add и старых спаунеров ------------

[SerializationGenerator(0, false)]
public partial class RaidDragon : Dragon, IRaidSpawn
{
    public int RewardMultiplier => 20;

    [Constructible]
    public RaidDragon()
    {
        Title = "дракон-налётчик";
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false) =>
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
}

// -- Стража у ворот на время набега ---------------------------------------------------------

/// <summary>Защитник, которого набег ставит у ворот и убирает, когда бой кончился.</summary>
[SerializationGenerator(0, false)]
public partial class RaidGateDefender : MahaonTownDefender
{
    [Constructible]
    public RaidGateDefender()
    {
        Name = "стражник у ворот";
    }

    public override bool PlayerRangeSensitive => false;
}

[SerializationGenerator(0, false)]
public partial class RaidGateDefenderMage : MahaonTownDefenderMage
{
    [Constructible]
    public RaidGateDefenderMage()
    {
        Name = "боевой маг у ворот";
    }

    public override bool PlayerRangeSensitive => false;
}
