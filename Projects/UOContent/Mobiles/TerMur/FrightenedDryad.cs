using ModernUO.Serialization;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Custom/FrightenedDryad.cs). The original is
/// a MondainQuester tied to a full ML quest chain (BoundToTheLandQuest, MondainsLegacy
/// expansion gating) — neither MondainQuester nor BaseQuest exist in this codebase (grepped,
/// zero hits), matching the same ML-quest-tie-in gap already documented for Despise/Rotworm.
/// Simplified to a plain, peaceful townsperson NPC preserving the original's body/name/stats;
/// the quest chain itself is dropped.</summary>
[SerializationGenerator(0, false)]
public partial class FrightenedDryad : BaseCreature
{
    [Constructible]
    public FrightenedDryad() : base(AIType.AI_Melee, FightMode.None, 10, 1)
    {
        Female = true;
        Body = 266;
        Title = "the Frightened Dryad";

        SetStr(100);
        SetDex(100);
        SetInt(25);

        SetHits(100);

        Karma = 2000;
        Blessed = true;
    }

    public override string DefaultName => "испуганная дриада";
}
