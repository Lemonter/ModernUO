using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Normal/Gregorio.cs). The Guilty quest gate on
/// Damage/CanBeHarmedBy cannot be expressed — the quest system it depends on does not exist here —
/// so he is ported as the plain murderer brigand he otherwise is.</summary>
[SerializationGenerator(0, false)]
public partial class Gregorio : BaseCreature
{
    [Constructible]
    public Gregorio() : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1)
    {
        Race = Race.Human;
        Title = "разбойник";

        InitBody();
        InitOutfit();

        SetStr(86, 100);
        SetDex(81, 95);
        SetInt(61, 75);

        SetDamage(15, 27);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 10, 15);
        SetResistance(ResistanceType.Fire, 10, 15);
        SetResistance(ResistanceType.Poison, 10, 15);
        SetResistance(ResistanceType.Energy, 10, 15);

        SetSkill(SkillName.MagicResist, 25.0, 50.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);
        SetSkill(SkillName.Wrestling, 80.0, 100.0);

        PackGold(50, 150);
    }

    public override string CorpseName => "труп Грегорио";
    public override string DefaultName => "Грегорио";

    public override bool AlwaysMurderer => true;

    private void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x8412;
        Female = false;

        HairItemID = 0x203C;
        HairHue = 0x47A;
        FacialHairItemID = 0x204D;
        FacialHairHue = 0x47A;
    }

    private void InitOutfit()
    {
        AddItem(new Sandals { Hue = 0x75E });
        AddItem(new Shirt());
        AddItem(new ShortPants { Hue = 0x66C });
        AddItem(new SkullCap { Hue = 0x649 });
        AddItem(new Pitchfork());
    }
}
