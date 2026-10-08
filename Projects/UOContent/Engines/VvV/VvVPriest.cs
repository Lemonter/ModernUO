using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/Mobiles/VvVPriest.cs).
[SerializationGenerator(0, false)]
public partial class VvVPriest : BaseVendor
{
    public override bool IsActiveVendor => false;
    public override bool DisallowAllMoves => true;
    public override bool ClickTitle => true;
    public override bool CanTeach => false;

    protected override List<SBInfo> SBInfos { get; } = new();
    public override void InitSBInfo()
    {
    }

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private VvVType _vvVType;

    // Not a [SerializableField]: VvVBattle is a [PropertyObject], see VvVSigil.cs for why.
    public VvVBattle Battle { get; set; }

    [Constructible]
    public VvVPriest() : this(VvVType.Virtue, null)
    {
    }

    public VvVPriest(VvVType type, VvVBattle battle) : base(type == VvVType.Vice ? "the Priest of Vice" : "the Priest of Virtue")
    {
        _vvVType = type;
        Battle = battle;
    }

    public override void InitBody()
    {
        base.InitBody();

        Name = NameList.RandomName("male");

        SpeechHue = 0x3B2;
        Hue = Race.Human.RandomSkinHue();
        Body = 0x190;
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (ViceVsVirtueSystem.Instance == null || Battle == null)
        {
            return false;
        }

        var entry = ViceVsVirtueSystem.Instance.GetPlayerEntry(from as PlayerMobile);

        if (from.InRange(Location, 2) && entry != null && ViceVsVirtueSystem.IsVvV(from) && dropped is VvVSigil sigil)
        {
            Battle.Update(null, entry, VvVType == VvVType.Vice ? UpdateType.TurnInVice : UpdateType.TurnInVirtue);

            sigil.Delete();
            Battle.Sigil = null;
        }

        return false;
    }

    public override void InitOutfit()
    {
        var hue = VvVType == VvVType.Virtue ? ViceVsVirtueSystem.VirtueHue : ViceVsVirtueSystem.ViceHue;

        var robe = new Robe
        {
            ItemID = 19357,
            Name = VvVType == VvVType.Virtue ? "Robe of Virtue" : "Robe of Vice",
            Hue = hue
        };

        EquipItem(robe);
    }

    [AfterDeserialization]
    private void AfterDeserialization() => Battle = ViceVsVirtueSystem.Instance?.Battle;
}
