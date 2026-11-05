using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfKhaldun/Creatures/CultistAmbusher.cs).
///     Dropped: the <c>Caddellite</c> damage gate (<c>OnBeforeDamage</c> and the Caddellite item-socket system have
///     no counterpart here), so the ambusher takes damage from every source. <c>SetWearable</c> becomes
///     <c>AddItem</c>/<c>EquipItem</c>; <c>SetWeaponAbility</c> is gone, <see cref="GetWeaponAbility" /> replaces it.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CultistAmbusher : BaseCreature
{
    public override string CorpseName => "труп амбушера-культиста";

    [Constructible]
    public CultistAmbusher() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Name = "культист-засадник";
        Body = 0x190;
        Hue = 2500;
        BaseSoundID = 0x45A;

        SetStr(150, 200);
        SetDex(150);
        SetInt(25, 44);

        SetHits(500, 1000);

        SetDamage(8, 18);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10, 20);
        SetResistance(ResistanceType.Fire, 10, 20);
        SetResistance(ResistanceType.Cold, 10, 20);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);

        SetSkill(SkillName.Fencing, 100.0, 120.0);
        SetSkill(SkillName.Macing, 100.0, 120.0);
        SetSkill(SkillName.MagicResist, 100.0, 120.0);
        SetSkill(SkillName.Swords, 100.0, 120.0);
        SetSkill(SkillName.Tactics, 100.0, 120.0);
        SetSkill(SkillName.Archery, 100.0, 120.0);
        SetSkill(SkillName.Parry, 100.0, 120.0);

        Fame = 8000;
        Karma = -8000;

        Utility.AssignRandomHair(this);

        switch (Utility.Random(3))
        {
            case 0:
                {
                    AddItem(new RingmailChest { Hue = 1510 });
                    AddItem(new ChainLegs { Hue = 1345 });
                    AddItem(new Sandals { Hue = 1345 });
                    AddItem(new LeatherNinjaHood { Hue = 1345 });
                    AddItem(new LeatherGloves { Hue = 1345 });
                    AddItem(new LeatherArms { Hue = 1345 });
                    break;
                }
            case 1:
                {
                    AddItem(new Robe { Hue = 2306 });
                    AddItem(new BearMask { Hue = 2683 });
                    break;
                }
            case 2:
                {
                    AddItem(new Shirt { Hue = 676 });
                    AddItem(new RingmailLegs());
                    AddItem(new StuddedArms());
                    AddItem(new StuddedGloves());
                    break;
                }
        }

        switch (Utility.Random(2))
        {
            case 0:
                {
                    EquipItem(
                        Loot.Construct(
                            new[]
                            {
                                typeof(Kryss), typeof(Spear), typeof(ShortSpear), typeof(Lance), typeof(Pike),
                                typeof(WarMace), typeof(Mace), typeof(WarHammer), typeof(WarAxe)
                            }
                        )
                    );

                    break;
                }
            case 1:
                {
                    EquipItem(
                        Loot.Construct(
                            new[] { typeof(Yumi), typeof(Crossbow), typeof(RepeatingCrossbow), typeof(HeavyCrossbow) }
                        )
                    );

                    RangeFight = 7;
                    AI = AIType.AI_Archer;

                    break;
                }
        }
    }

    public override bool AlwaysMurderer => true;

    public override bool ShowFameTitle => false;

    public override void OnGotMeleeAttack(Mobile attacker, int damage)
    {
        base.OnGotMeleeAttack(attacker, damage);

        if (Map != null && AI == AIType.AI_Archer && 0.4 >= Utility.RandomDouble())
        {
            var p = FindLocation(Map, Location, 10);
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x3728,
                10,
                10,
                2023
            );
            Location = p;
            Effects.SendLocationParticles(EffectItem.Create(p, Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

            PlaySound(0x1FE);
        }
    }

    private static Point3D FindLocation(Map map, Point3D center, int range)
    {
        int cx = center.X;
        int cy = center.Y;

        for (var i = 0; i < 20; ++i)
        {
            var x = cx + Utility.Random(range * 2) - range;
            var y = cy + Utility.Random(range * 2) - range;

            if ((cx - x) * (cx - x) + (cy - y) * (cy - y) > range * range)
            {
                continue;
            }

            var z = map.GetAverageZ(x, y);

            if (!map.CanFit(x, y, z, 6, false, false))
            {
                continue;
            }

            var topZ = z;

            foreach (var item in map.GetItemsInRange(new Point3D(x, y, z), 0))
            {
                topZ = Math.Max(topZ, item.Z + item.ItemData.CalcHeight);
            }

            return new Point3D(x, y, topZ);
        }

        return center;
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }

    public override WeaponAbility GetWeaponAbility()
    {
        if (Weapon is BaseWeapon { } weapon and not Fists)
        {
            return Utility.RandomDouble() > 0.5 ? weapon.PrimaryAbility : weapon.SecondaryAbility;
        }

        return null;
    }
}
