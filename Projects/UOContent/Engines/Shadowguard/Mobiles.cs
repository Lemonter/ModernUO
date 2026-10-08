using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Shadowguard;

[SerializationGenerator(0, false)]
public partial class ShadowguardPirate : BaseCreature
{
    [Constructible]
    public ShadowguardPirate() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = NameList.RandomName("male");
        Title = "the Pirate";

        Body = 0x190;
        Hue = Race.RandomSkinHue();

        SetStr(386, 400);
        SetDex(151, 165);
        SetInt(161, 175);

        SetHits(1200);

        SetDamage(15, 21);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Anatomy, 125.0);
        SetSkill(SkillName.MagicResist, 83.5, 92.5);
        SetSkill(SkillName.Wrestling, 125.0);
        SetSkill(SkillName.Tactics, 125.0);

        AddItem(new ExecutionersAxe());

        AddItem(new Boots(Utility.RandomNeutralHue()));
        AddItem(new ShortPants());
        AddItem(new FancyShirt());
        AddItem(new TricorneHat());

        Fame = 5000;
        Karma = -5000;

        Utility.AssignRandomHair(this);
    }

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 3);

    public override bool AlwaysMurderer => true;

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _blockReflect;

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
    {
        base.Damage(amount, from, informMount, ignoreEvilOmen);

        if (!_blockReflect && from != null && amount > 0)
        {
            _blockReflect = true;
            AOS.Damage(from, this, Math.Max(1, (int)(amount * .37)), false, 0, 0, 0, 0, 0, 0, 100);
            _blockReflect = false;

            from.PlaySound(0x1F1);
        }
    }
}

[SerializationGenerator(0, false)]
public partial class ShantyThePirate : ShadowguardPirate
{
    [Constructible]
    public ShantyThePirate()
    {
        Name = "Shanty";

        SetHits(10000);

        SetSkill(SkillName.Fencing, 120.0);
        SetSkill(SkillName.Macing, 120.0);
        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Swords, 120.0);
        SetSkill(SkillName.Tactics, 120.0);
        SetSkill(SkillName.Wrestling, 120.0);

        Fame = 15000;
        Karma = -15000;

        BlockReflect = true;
    }

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 3);

    [AfterDeserialization]
    private void AfterDeserialization() => BlockReflect = true;
}

[SerializationGenerator(0, false)]
public partial class VileWaterElemental : WaterElemental
{
    public override bool CanMoveOverObstacles => false;

    [Constructible]
    public VileWaterElemental()
    {
        Name = "a vile water elemental";
        Hue = 1916;
        Body = 13;
    }

    public override bool DeleteCorpseOnDeath => true;

    public override bool OnBeforeDeath()
    {
        if (ShadowguardController.GetEncounter(Location, Map) is FountainEncounter encounter)
        {
            var canal = new ShadowguardCanal();
            canal.MoveToWorld(Location, Map);
            encounter.AddShadowguardCanal(canal);
        }

        return base.OnBeforeDeath();
    }
}

[SerializationGenerator(0, false)]
public partial class HurricaneElemental : VileWaterElemental
{
    [Constructible]
    public HurricaneElemental()
    {
        Name = "a hurricane elemental";
        Body = 16;

        SetStr(400, 500);
        SetDex(140, 250);
        SetInt(130, 150);

        SetHits(550, 700);
        SetMana(650, 750);

        SetDamage(14, 16);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 50);

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 45, 55);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 40, 60);

        SetSkill(SkillName.Wrestling, 95.0, 110.0);
        SetSkill(SkillName.Tactics, 95.0, 110.0);
        SetSkill(SkillName.Magery, 95.0, 110.0);
        SetSkill(SkillName.EvalInt, 95.0, 110.0);
        SetSkill(SkillName.Parry, 95.0, 110.0);
        SetSkill(SkillName.DetectHidden, 63.0);
    }
}

[SerializationGenerator(0, false)]
public partial class VileTreefellow : FeralTreefellow
{
    [Constructible]
    public VileTreefellow()
    {
        Name = "a vile treefellow";

        SetDamage(12, 16);

        SetResistance(ResistanceType.Physical, 2);
        SetResistance(ResistanceType.Cold, 5);
        SetResistance(ResistanceType.Poison, 3);
        SetResistance(ResistanceType.Energy, 2);

        SetSkill(SkillName.MagicResist, 40.1, 55.0);
        SetSkill(SkillName.Tactics, 65.1, 90.0);
        SetSkill(SkillName.Wrestling, 65.1, 105.0);
        SetSkill(SkillName.Poisoning, 100.0);
        SetSkill(SkillName.DetectHidden, 40.0, 45.0);
        SetSkill(SkillName.Parry, 55.0, 60.0);

    }

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);
        Paralyze(defender);
    }

    private static void Paralyze(Mobile defender)
    {
        defender.Paralyze(TimeSpan.FromSeconds(Utility.Random(3)));

        defender.FixedEffect(0x376A, 6, 1);
        defender.PlaySound(0x204);

        defender.SendLocalizedMessage(1060164); // The attack has temporarily paralyzed you!
    }

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 3);
}

[SerializationGenerator(0, false)]
public partial class EnsorcelledArmor : BaseCreature
{
    public ArmoryEncounter Encounter { get; set; }

    public override string CorpseName => "магический труп";

    [Constructible]
    public EnsorcelledArmor() : this(null)
    {
    }

    [Constructible]
    public EnsorcelledArmor(ArmoryEncounter encounter) : base(AIType.AI_Melee, FightMode.Weakest)
    {
        Encounter = encounter;
        Name = "ensorcelled armor";
        BaseSoundID = 412;

        Body = 0x190;
        SetStr(386, 400);
        SetDex(151, 165);
        SetInt(161, 175);

        SetDamage(15, 21);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Anatomy, 125.0);
        SetSkill(SkillName.Fencing, 46.0, 77.5);
        SetSkill(SkillName.Macing, 35.0, 57.5);
        SetSkill(SkillName.Poisoning, 60.0, 82.5);
        SetSkill(SkillName.MagicResist, 83.5, 92.5);
        SetSkill(SkillName.Swords, 125.0);
        SetSkill(SkillName.Tactics, 125.0);
        SetSkill(SkillName.Lumberjacking, 125.0);

        AddItem(new CloseHelm { Hue = 0x96D });
        AddItem(new PlateArms { Hue = 0x96D });
        AddItem(new PlateLegs { Hue = 0x96D });
        AddItem(new PlateChest { Hue = 0x96D });
        AddItem(new PlateGorget { Hue = 0x96D });
        AddItem(new PlateGloves { Hue = 0x96D });
        AddItem(new Halberd { Hue = 0x96D });
        AddItem(new HalfApron(728));

        Fame = 8500;
        Karma = -8500;
    }

    public override bool AlwaysMurderer => true;

    public override bool OnBeforeDeath()
    {
        if (!base.OnBeforeDeath())
        {
            return false;
        }

        if (0.66 > Utility.RandomDouble() && Encounter != null)
        {
            new Phylactery().MoveToWorld(Location, Map);
        }

        return true;
    }

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 3);
}

[SerializationGenerator(0, false)]
public partial class VileDrake : Drake
{
    [Constructible]
    public VileDrake()
    {
        Name = "a vile drake";

        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 80, 90);
        SetResistance(ResistanceType.Poison, 90, 100);
        SetResistance(ResistanceType.Energy, 70, 80);

        SetSkill(SkillName.MagicResist, 65.0, 80.0);
        SetSkill(SkillName.Tactics, 65.0, 90.0);
        SetSkill(SkillName.Wrestling, 110.0, 130.0);
        SetSkill(SkillName.DetectHidden, 50.6);
        SetSkill(SkillName.Parry, 65.0, 75.0);

    }

    public override void OnDeath(Container c)
    {
        if (ShadowguardController.GetEncounter(c.Location, c.Map) is BelfryEncounter)
        {
            c.DropItem(new MagicDrakeWing());
        }

        base.OnDeath(c);
    }

    public override void GenerateLoot() => AddLoot(LootPack.Rich, 3);
}

[SerializationGenerator(0, false)]
public partial class ShadowguardGreaterDragon : GreaterDragon
{
    [Constructible]
    public ShadowguardGreaterDragon()
    {
        Tamable = false;

        SetHits(9800, 10999);

        SetDamage(29, 38);

        SetSkill(SkillName.EvalInt, 110.0, 145.0);
        SetSkill(SkillName.Magery, 110.0, 145.0);
        SetSkill(SkillName.MagicResist, 110.0, 150.0);
        SetSkill(SkillName.Tactics, 110.0, 155.0);
        SetSkill(SkillName.Wrestling, 115.0, 155.0);
        SetSkill(SkillName.DetectHidden, 120.0);
        SetSkill(SkillName.Parry, 120.0);
    }


    public override void OnThink()
    {
        base.OnThink();

        if (ShadowguardController.GetEncounter(Location, Map) is BelfryEncounter encounter && Z == -20)
        {
            var p = encounter.SpawnPoints[0];
            encounter.ConvertOffset(ref p);

            MoveToWorld(p, Map);
        }
    }

    protected override bool OnMove(Direction d)
    {
        if (ShadowguardController.GetEncounter(Location, Map) != null)
        {
            var x = X;
            var y = Y;

            Movement.Movement.Offset(d, ref x, ref y);

            var z = Map.GetAverageZ(x, y);

            foreach (var item in Map.GetItemsInRange<Item>(new Point3D(x, y, z), 0))
            {
                if (item.Z + item.ItemData.CalcHeight > z)
                {
                    z = item.Z + item.ItemData.CalcHeight;
                }
            }

            foreach (var tile in Map.Tiles.GetStaticTiles(x, y))
            {
                var itemData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                if (tile.Z + itemData.CalcHeight > z)
                {
                    z = tile.Z + itemData.CalcHeight;
                }
            }

            if (z < Z)
            {
                return false;
            }
        }

        return base.OnMove(d);
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
    {
        if (from == null || (ShadowguardController.GetEncounter(Location, Map) != null && Z == from.Z))
        {
            base.Damage(amount, from, informMount, ignoreEvilOmen);
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
        AddLoot(LootPack.Gems, 8);
    }

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        if (Map == null || 0.5 <= Utility.RandomDouble())
        {
            return;
        }

        var pushRange = Utility.RandomMinMax(2, 4);

        var d = Utility.GetDirection(Location, defender.Location);
        var x = defender.X;
        var y = defender.Y;

        for (var i = 0; i < pushRange; i++)
        {
            Movement.Movement.Offset(d, ref x, ref y);
        }

        defender.MoveToWorld(new Point3D(x, y, Z), Map);
    }
}

[SerializationGenerator(0, false)]
public partial class LadyMinax : BaseCreature
{
    [Constructible]
    public LadyMinax() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "Minax";
        Title = "the Enchantress";

        Body = 0x191;
        Hue = Race.RandomSkinHue();
        HairItemID = 0x203C;
        HairHue = Race.RandomHairHue();

        SetStr(386, 400);
        SetDex(151, 165);
        SetInt(161, 175);

        SetDamage(15, 21);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Magery, 125.0);
        SetSkill(SkillName.EvalInt, 125.0);
        SetSkill(SkillName.Meditation, 125.0);
        SetSkill(SkillName.Anatomy, 125.0);
        SetSkill(SkillName.Fencing, 46.0, 77.5);
        SetSkill(SkillName.Macing, 35.0, 57.5);
        SetSkill(SkillName.Poisoning, 60.0, 82.5);
        SetSkill(SkillName.MagicResist, 83.5, 92.5);
        SetSkill(SkillName.Swords, 125.0);
        SetSkill(SkillName.Tactics, 125.0);
        SetSkill(SkillName.Lumberjacking, 125.0);

        EquipItem(new Cloak { Hue = 1157 });
        EquipItem(new Boots { Hue = 1175 });
        EquipItem(new FemaleStuddedChest { Hue = 1175 });
        EquipItem(new LeatherGloves { Hue = 1157 });
    }

    public override bool AlwaysMurderer => true;

    protected override bool OnMove(Direction d)
    {
        if (ShadowguardController.GetEncounter(Location, Map) is RoofEncounter encounter)
        {
            var spawn = encounter.SpawnPoints[0];
            encounter.ConvertOffset(ref spawn); // relative offset -> absolute world coordinate, same as OnThink below

            var x = X;
            var y = Y;

            Movement.Movement.Offset(d, ref x, ref y);

            var p = new Point3D(x, y, Map.GetAverageZ(x, y));
            var z = p.Z;

            if (p.Y < spawn.Y - 5 || p.Y > spawn.Y + 4 || p.X > spawn.X + 4 || p.X < spawn.X - 5)
            {
                return false;
            }

            foreach (var item in Map.GetItemsInRange<Item>(p, 0))
            {
                if (item.Z + item.ItemData.CalcHeight > z)
                {
                    z = item.Z + item.ItemData.CalcHeight;
                }
            }

            foreach (var tile in Map.Tiles.GetStaticTiles(x, y))
            {
                var itemData = TileData.ItemTable[tile.ID & TileData.MaxItemValue];

                if (tile.Z + itemData.CalcHeight > z)
                {
                    z = tile.Z + itemData.CalcHeight;
                }
            }

            if (z < Z)
            {
                return false;
            }
        }

        return base.OnMove(d);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (ShadowguardController.GetEncounter(Location, Map) is RoofEncounter encounter)
        {
            var spawn = encounter.SpawnPoints[0];
            var p = Location;
            encounter.ConvertOffset(ref spawn);

            if (Z < 30 || p.Y < spawn.Y - 5 || p.Y > spawn.Y + 4 || p.X > spawn.X + 4 || p.X < spawn.X - 5)
            {
                MoveToWorld(spawn, Map.TerMur);
            }
        }
    }

    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
    {
        if (ShadowguardController.GetEncounter(Location, Map) != null && from != null)
        {
            from.SendLocalizedMessage(1156254); // Minax laughs as she deflects your puny attacks! Defeat her minions to close the Time Gate!
            return;
        }

        base.Damage(amount, from, informMount, ignoreEvilOmen);
    }
}
