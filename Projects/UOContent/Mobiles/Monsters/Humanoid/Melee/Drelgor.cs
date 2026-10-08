using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Drelgor the Impaler, who haunts the Old Haven training grounds. Ported from ServUO
/// (Scripts/Mobiles/Named/Drelgor.cs). Once a minute he announces himself to everyone standing
/// in that region with him.
///
/// The original walks the whole NetState list and compares region names by string on both
/// sides; here he asks his own region for the players in it, which is the same set without the
/// global sweep. Its `msgevery` field is a hardcoded constant it calls configurable — kept as a
/// real constant, and the "0 disables" branch with it.</summary>
[SerializationGenerator(0, false)]
[CorpseName("a corpse of Drelgor the Impaler")]
public partial class Drelgor : BaseCreature
{
    private static readonly TimeSpan MessageInterval = TimeSpan.FromMinutes(1.0);

    private long _nextMessage;

    [Constructible]
    public Drelgor() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 147;
        BaseSoundID = 451;

        SetStr(127, 137);
        SetDex(82, 94);
        SetInt(48, 55);

        SetHits(131, 136);

        SetDamage(6, 8);

        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Cold, 60);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.Wrestling, 60.0);
        SetSkill(SkillName.Tactics, 60.0);
        SetSkill(SkillName.MagicResist, 60.0);

        Fame = 3600;
        Karma = -3600;

        VirtualArmor = 40;

        PackItem(new Scimitar());
        PackItem(new WoodenShield());

        PackItem(
            Utility.Random(5) switch
            {
                0 => new BoneArms(),
                1 => new BoneChest(),
                2 => new BoneGloves(),
                3 => new BoneLegs(),
                _ => (Item)new BoneHelm()
            }
        );
    }

    public override string DefaultName => "Дрелгор Пронзатель";

    public override bool BleedImmune => true;

    public override OppositionGroup OppositionGroup => OppositionGroup.FeyAndUndead;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Meager);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (_nextMessage > Core.TickCount)
        {
            return;
        }

        _nextMessage = Core.TickCount + (int)MessageInterval.TotalMilliseconds;
        BroadcastMessage();
    }

    /// <summary>Who dares to defile Haven? I am Drelgor the Impaler! I shall claim your souls as
    /// payment for this intrusion!</summary>
    public void BroadcastMessage()
    {
        var region = Region;

        if (region?.Name != "Old Haven Training")
        {
            return;
        }

        foreach (var m in region.GetMobiles())
        {
            if (m.Player)
            {
                m.SendLocalizedMessage(1077840, "", 34);
                m.PlaySound(0x14);
            }
        }
    }
}
