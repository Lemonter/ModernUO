using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>
///     Simplified port of ServUO's Scripts/Mobiles/NPCs/MysteriousWisp.cs — placed by
///     DespiseRevampedSetup as dungeon flavor. The original was also a restocking shop
///     (DoRestock, a full dialogue tree) AND an ML Quest giver (WishesOfTheWispQuest/
///     WhisperingWithWispsQuest/MondainQuestGump) — both dropped along with the rest of this
///     port's quest tie-in (see DespiseController's class doc comment); this is a plain
///     stationary decorative NPC with a single greeting line.
/// </summary>
[SerializationGenerator(0, false)]
[CorpseName("a wisp corpse")]
public partial class MysteriousWisp : BaseCreature
{
    [Constructible]
    public MysteriousWisp() : base(AIType.AI_Mage, FightMode.None, 10, 1)
    {
        CantWalk = true;
        Blessed = true;
        SpeechHue = 52;

        Body = 58;
        BaseSoundID = 466;

        SetStr(196, 225);
        SetDex(196, 225);
        SetInt(196, 225);

        SetHits(118, 135);

        SetDamage(17, 18);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 35, 45);
        SetResistance(ResistanceType.Fire, 20, 40);
        SetResistance(ResistanceType.Cold, 10, 30);
        SetResistance(ResistanceType.Poison, 5, 10);
        SetResistance(ResistanceType.Energy, 50, 70);

        SetSkill(SkillName.EvalInt, 80.0);
        SetSkill(SkillName.Magery, 80.0);
        SetSkill(SkillName.MagicResist, 80.0);
        SetSkill(SkillName.Tactics, 80.0);
        SetSkill(SkillName.Wrestling, 80.0);

        Backpack?.Delete();
    }

    public override string DefaultName => "загадочный огонёк";

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(Location, 4))
        {
            SayTo(from, "The wisps whisper of a great struggle between light and shadow...");
        }
    }
}
