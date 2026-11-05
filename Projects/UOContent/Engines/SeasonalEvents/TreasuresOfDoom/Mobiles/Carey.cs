using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.SeasonalEvents;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO (Scripts/Services/Seasonal Events/TreasuresOfDoom/Mobiles/Carey.cs).
///     ServUO's <c>BaseTurnInMobile</c> (turn in Doom-crested equipment for points) does not exist here, so Carey
///     only opens the reward store; points are credited by <see cref="DoomData.AwardKill" />. <c>SetWearable</c>
///     becomes <c>AddItem(new X { Hue = hue })</c>.
/// </summary>
[SerializationGenerator(0, false)]
public partial class Carey : BaseVendor
{
    public static Point3D SpawnLocation { get; } = new(2373, 1278, -90);

    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public Carey() : base("исследователь")
    {
        Instance = this;
    }

    public static Carey Instance { get; set; }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override bool IsActiveVendor => false;

    public override bool IsInvulnerable => true;

    public override bool DisallowAllMoves => true;

    public override bool ClickTitle => true;

    public override bool CanTeach => false;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = "Кэри";
        Female = true;

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x191;
    }

    public override void InitOutfit()
    {
        AddItem(new Robe { Hue = 1364 });
        AddItem(new ThighBoots { Hue = 1908 });
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1155594); // Artifacts of Doom Trader
    }

    private DateTime _nextSpeak;

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        base.OnMovement(m, oldLocation);

        if (_nextSpeak >= Core.Now)
        {
            return;
        }

        SayTo(m, 1155591); // Bring me items of Doom and I will reward you with valuable items.
        _nextSpeak = Core.Now + TimeSpan.FromSeconds(25);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from is PlayerMobile pm && from.InRange(Location, 5))
        {
            from.SendGump(new DoomRewardGump(this, pm));
        }
    }

}

