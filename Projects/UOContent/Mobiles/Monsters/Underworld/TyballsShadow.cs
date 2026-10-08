using ModernUO.Serialization;
using Server.Collections;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The shade of the traitor Tyball, beyond the void in the Shrine. Ported from ServUO
/// (Scripts/Items/Quest/TyballsShadow.cs — filed under items there, by mistake). He carries the
/// yellow key: on death one of the players with looting rights gets it, so the fight has to be
/// repeated for every member of a party that wants entry to the Abyss.
///
/// Two notes on the original's own numbers, ported as they stand rather than "corrected":
/// it sets Energy damage twice (25, then 20), so the 25 never applied and only the second
/// line counts; and the resulting split — 100 physical, 20 poison, 20 energy — adds up to 140 %.
/// Only the duplicate line is dropped here, since it was dead code either way.</summary>
[SerializationGenerator(0, false)]
public partial class TyballsShadow : BaseCreature
{
    [Constructible]
    public TyballsShadow() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 0x190;
        Hue = 0x4001;
        Female = false;
        Name = "Тень Тайболла";

        SetStr(400, 450);
        SetDex(210, 250);
        SetInt(310, 330);

        SetHits(2800, 3000);

        SetDamage(20, 25);

        SetDamageType(ResistanceType.Physical, 100);
        SetDamageType(ResistanceType.Poison, 20);
        SetDamageType(ResistanceType.Energy, 20);

        SetResistance(ResistanceType.Physical, 70);
        SetResistance(ResistanceType.Fire, 70);
        SetResistance(ResistanceType.Cold, 70);
        SetResistance(ResistanceType.Poison, 70);
        SetResistance(ResistanceType.Energy, 70);

        SetSkill(SkillName.Magery, 100.0);
        SetSkill(SkillName.MagicResist, 120.0);
        SetSkill(SkillName.Tactics, 100.0);
        SetSkill(SkillName.Wrestling, 100.0);

        AddItem(new ShroudOfTheCondemned { Movable = Utility.RandomDouble() < 0.1 });

        Fame = 20000;
        Karma = -20000;

        VirtualArmor = 65;
    }

    public override string CorpseName => "труп тени Тайболла";

    public override bool BardImmune => true;
    public override bool Unprovokable => true;
    public override bool Uncalmable => true;
    public override bool AlwaysMurderer => true;

    public override void GenerateLoot() => AddLoot(LootPack.FilthyRich, 3);

    public override void OnDeath(Container c)
    {
        if (Map == Map.TerMur)
        {
            using var toGive = PooledRefList<Mobile>.Create();

            var rights = GetLootingRights(DamageEntries, HitsMax);

            for (var i = rights.Count - 1; i >= 0; --i)
            {
                if (rights[i].m_HasRight)
                {
                    toGive.Add(rights[i].m_Mobile);
                }
            }

            if (toGive.Count > 0)
            {
                toGive[Utility.Random(toGive.Count)].AddToBackpack(new YellowKey1());
            }
        }

        base.OnDeath(c);
    }

    /// <summary>The shrine floor has a lip the pathing walks him under; the original nudges him
    /// back up to Z 0 whenever he ends up below it inside that rectangle.</summary>
    public override void OnThink()
    {
        base.OnThink();

        if (Map != null && Z < 0 && X >= 1177 && X <= 1183 && Y >= 877 && Y <= 886 &&
            Region.Find(Location, Map).IsPartOf("Underworld"))
        {
            Z = 0;
        }
    }
}
