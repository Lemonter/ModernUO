using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/JackThePumpkinKing.cs).
///     <c>SetWearable</c> becomes <c>AddItem(new X { Hue = hue })</c>. <see cref="UnholyPumpkin" /> keeps the same
///     behaviour with token-based timers, and its <c>ICarvable</c> signature is the void one used here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class JackThePumpkinKing : BaseCreature
{
    private bool _inHere;

    public override string CorpseName => "труп Короля Тыкв";

    [Constructible]
    public JackThePumpkinKing() : base(AIType.AI_NecroMage, FightMode.Closest, 10, 1)
    {
        Name = "Джек";
        Title = "Король Тыкв";
        Body = 0x190;
        Hue = Race.RandomSkinHue();

        SetStr(500);
        SetDex(200);
        SetInt(1200);

        SetHits(26000);

        SetDamage(22, 26);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 70, 80);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Anatomy, 25);
        SetSkill(SkillName.MagicResist, 120);
        SetSkill(SkillName.Tactics, 100, 110);
        SetSkill(SkillName.Wrestling, 130, 140);
        SetSkill(SkillName.Parry, 20, 30);
        SetSkill(SkillName.Magery, 120);
        SetSkill(SkillName.EvalInt, 120);
        SetSkill(SkillName.Necromancy, 120);
        SetSkill(SkillName.SpiritSpeak, 120);
        SetSkill(SkillName.Meditation, 120);
        SetSkill(SkillName.Focus, 70, 80);

        Fame = 12000;
        Karma = -12000;

        AddItem(new ClothNinjaHood { Hue = 1281 });
        AddItem(new BoneChest { Hue = 1175 });
        AddItem(new BoneArms { Hue = 1175 });
        AddItem(new BoneGloves { Hue = 1175 });
        AddItem(new ThighBoots());
        AddItem(new Scepter());
    }

    public override void OnDamage(int amount, Mobile from, bool willKill)
    {
        base.OnDamage(amount, from, willKill);

        if (from == null || from == this || _inHere)
        {
            return;
        }

        _inHere = true;

        AOS.Damage(from, this, Utility.RandomMinMax(8, 20), 100, 0, 0, 0, 0);

        MovingEffect(from, Utility.RandomMinMax(0xC6A, 0xC6C), 10, 0, false, false, 0, 0);
        PlaySound(0x491);

        if (Utility.RandomDouble() < 0.05)
        {
            Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => CreatePumpkins(from));
        }

        _inHere = false;
    }

    private static void CreatePumpkins(Mobile from)
    {
        var map = from.Map;

        if (map == null)
        {
            return;
        }

        var count = Utility.RandomMinMax(1, 3);

        for (var i = 0; i < count; ++i)
        {
            var x = from.X + Utility.RandomMinMax(-1, 1);
            var y = from.Y + Utility.RandomMinMax(-1, 1);
            var z = from.Z;

            if (!map.CanFit(x, y, z, 16, false, true))
            {
                z = map.GetAverageZ(x, y);

                if (z == from.Z || !map.CanFit(x, y, z, 16, false, true))
                {
                    continue;
                }
            }

            var pumpkin = new UnholyPumpkin
            {
                Hue = 0,
                ItemID = Utility.RandomMinMax(0xC6A, 0xC6C)
            };

            pumpkin.MoveToWorld(new Point3D(x, y, z), map);
        }
    }

    public override bool AlwaysMurderer => true;

    public override Poison PoisonImmune => Poison.Deadly;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfSorceresDungeon/Creatures/JackThePumpkinKing.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class UnholyPumpkin : Item, ICarvable
{
    private TimerExecutionToken _spawnToken;

    [Constructible]
    public UnholyPumpkin() : base(0xF7E)
    {
        Movable = false;

        Timer.StartTimer(TimeSpan.FromSeconds(Utility.RandomMinMax(5, 10)), SpawnUndead, out _spawnToken);
    }

    public override string DefaultName => "нечестивая тыква";

    public void Carve(Mobile from, Item item)
    {
        Effects.PlaySound(GetWorldLocation(), Map, 0x48F);
        Effects.SendLocationEffect(GetWorldLocation(), Map, 0x3728, 10, 10, 0, 0);

        if (Utility.RandomDouble() < 0.3)
        {
            from.SendMessage("Вы уничтожаете тыкву.");

            var gold = new Gold(25, 100);
            gold.MoveToWorld(GetWorldLocation(), Map);

            Delete();
            _spawnToken.Cancel();
        }
        else
        {
            from.SendMessage("Вы повреждаете тыкву.");
        }
    }

    private void SpawnUndead()
    {
        if (Deleted)
        {
            return;
        }

        Mobile spawn = Utility.Random(12) switch
        {
            0  => new Skeleton(),
            1  => new Zombie(),
            2  => new Wraith(),
            3  => new Spectre(),
            4  => new Ghoul(),
            5  => new Mummy(),
            6  => new Bogle(),
            7  => new RottingCorpse(),
            8  => new BoneKnight(),
            9  => new SkeletalKnight(),
            10 => new Lich(),
            _  => new LichLord()
        };

        spawn.MoveToWorld(Location, Map);

        Delete();
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _spawnToken.Cancel();
    }
}
