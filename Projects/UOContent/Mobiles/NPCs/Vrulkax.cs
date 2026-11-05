using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Vrulkax : BaseVendor
{
    private static readonly Type[][] _table =
    [
        [typeof(BritchesOfWarding), typeof(GargishBritchesOfWarding)],
        [typeof(GlovesOfFeudalGrip), typeof(GargishKiltOfFeudalVise)],
        [typeof(CuffsOfTheArchmage), typeof(GargishCuffsOfTheArchmage)],
        [typeof(BritchesOfWarding), typeof(GargishBritchesOfWarding)],
        [typeof(BowOfTheInfiniteSwarm), typeof(GlaiveOfTheInfiniteSwarm)]
    ];

    private readonly List<SBInfo> _sbInfos = new();

    private DateTime _nextSpeak;

    [Constructible]
    public Vrulkax() : base("возвышенный искусник")
    {
    }

    public override bool IsActiveVendor => false;
    public override bool IsInvulnerable => true;
    public override string DefaultName => "Врулкакс";

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        foreach (var pair in _table)
        {
            if (dropped is IDurability durability && pair[0] == dropped.GetType())
            {
                if (durability.MaxHitPoints == 255 && durability.HitPoints == 255)
                {
                    var item = Loot.Construct(pair[1]);

                    if (item != null)
                    {
                        from.AddToBackpack(item);
                        dropped.Delete();
                    }

                    return true;
                }

                // I can only work with artifacts that are in pristine condition.
                SayTo(from, 1157368);
                return false;
            }
        }

        // I'm sorry, I cannot accept this item.
        SayTo(from, 1157365);
        return false;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (_nextSpeak < Core.Now && m is PlayerMobile && InRange(m.Location, 5) && InLOS(m))
        {
            // I am the Exalted Artificer. Hand me select human artifacts to convert them for gargoyle use.
            Say(1157369);
            _nextSpeak = Core.Now + TimeSpan.FromSeconds(25);
        }

        base.OnMovement(m, oldLocation);
    }

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        CantWalk = true;
        Race = Race.Gargoyle;

        Hue = 34545;
        HairItemID = 0x4259;
        HairHue = 0x0;
    }

    public override void InitOutfit()
    {
        AddItem(new SerpentstoneStaff());
        AddItem(
            new GargishClothChestType1
            {
                Hue = 902
            }
        );
        AddItem(
            new GargishClothArmsType1
            {
                Hue = 902
            }
        );
        AddItem(
            new GargishClothKiltType1
            {
                Hue = 902
            }
        );
    }
}
