using ModernUO.Serialization;
using Server.Items;
using Server.Network;

namespace Server.Mobiles;

/// <summary>A marker for the two creatures Jaacar's barrel can be filled from. It has no
/// members in the original either — the quest objective matches on the interface, so any
/// creature that carries it counts.</summary>
public interface IBloodCreature
{
}

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/BloodWorm.cs). SetSpecialAbility
/// dropped (see Rotworm.cs). The corpse-blood-drain OnAfterMove mechanic is kept verbatim
/// (Corpse confirmed to exist locally at Items/Misc/Corpses/Corpse.cs).</summary>
[SerializationGenerator(0, false)]
[CorpseName("a bloodworm corpse")]
public partial class BloodWorm : BaseCreature, IBloodCreature
{
    [Constructible]
    public BloodWorm() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        Body = 287;

        SetStr(401, 473);
        SetDex(80);
        SetInt(18, 19);

        SetHits(374, 422);

        SetDamage(11, 17);

        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Poison, 40);

        SetResistance(ResistanceType.Physical, 52, 55);
        SetResistance(ResistanceType.Fire, 42, 50);
        SetResistance(ResistanceType.Cold, 29, 31);
        SetResistance(ResistanceType.Poison, 69, 75);
        SetResistance(ResistanceType.Energy, 26, 27);

        SetSkill(SkillName.MagicResist, 35.0);
        SetSkill(SkillName.Tactics, 100.0);
        SetSkill(SkillName.Wrestling, 100.0);
    }

    public override string DefaultName => "кровавый червь";

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Average);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (Utility.RandomDouble() < 0.02)
        {
            c.DropItem(new LuckyCoin());
        }
    }

    public override int GetIdleSound() => 1503;
    public override int GetAngerSound() => 1500;
    public override int GetHurtSound() => 1502;
    public override int GetDeathSound() => 1501;

    public override void OnAfterMove(Point3D oldLocation)
    {
        base.OnAfterMove(oldLocation);

        if (Hits < HitsMax && 0.25 > Utility.RandomDouble())
        {
            Corpse toAbsorb = null;

            foreach (var item in Map.GetItemsInRange(Location, 1))
            {
                if (item is Corpse c && c.ItemID == 0x2006)
                {
                    toAbsorb = c;
                    break;
                }
            }

            if (toAbsorb != null)
            {
                toAbsorb.ProcessDelta();
                toAbsorb.SendRemovePacket();
                toAbsorb.ItemID = Utility.Random(0xECA, 9); // bone graphic
                toAbsorb.Hue = 0;
                toAbsorb.Direction = Direction.North;
                toAbsorb.ProcessDelta();

                Hits = HitsMax;

                // * The creature drains blood from a nearby corpse to heal itself. *
                PublicOverheadMessage(MessageType.Regular, 0x3B2, 1111699);
            }
        }
    }
}
