using ModernUO.Serialization;
using Server.Engines.ShameRevamped;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Shame Revamped dungeon (Mobiles/Creatures.cs) — the cave troll that holds a
///     rubble wall shut: killing it drops the wall, and the pass-through teleporters stop working.
///
///     Dropped with the original: the saltpeter/potash/charcoal/black powder pack items (none of those
///     item types exist here), and the <c>Region.IsPartOf("Shame")</c> guard on the crystal drop — the
///     creature only ever spawns as a wall guardian, so the guard was defensive.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CaveTroll : Troll
{
    public ShameWall Wall { get; set; }

    [Constructible]
    public CaveTroll() : this(null)
    {
    }

    public CaveTroll(ShameWall wall)
    {
        Name = "пещерный тролль";
        Body = 0x1;
        FightMode = FightMode.Aggressor;

        if (wall != null)
        {
            Title = "страж стены";
        }

        Hue = 638;
        Wall = wall;

        SetStr(180, 210);
        SetDex(107, 205);
        SetInt(40, 70);

        SetHits(638, 978);

        SetDamage(15, 17);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 45, 55);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 35, 45);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.MagicResist, 70, 90);
        SetSkill(SkillName.Tactics, 80, 110);
        SetSkill(SkillName.Wrestling, 80, 110);
        SetSkill(SkillName.DetectHidden, 100.0);

        Fame = 3500;
        Karma = -3500;

        PackGem(1);
    }

    public override MeatType MeatType => MeatType.Ribs;
    public override int Meat => 2;
    public override int TreasureMapLevel => 1;

    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ArmorIgnore;

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        Wall?.OnTrollKilled();

        if (0.10 > Utility.RandomDouble())
        {
            c.DropItem(new ShameCrystal());
        }

        Wall = null;
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}
