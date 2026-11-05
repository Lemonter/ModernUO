using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     Ported from ServUO's Wrong Revamped dungeon (Mobile/WrongPrisoner.cs) — the armoured prisoner the
///     jailor locks up and the escorts lead back out.
///
///     Adapted: ServUO derives this from its RunUO-era <c>Server.Engines.Quests.BaseEscort</c> and lists
///     <c>EscortToWrongEntrance</c> in <c>Quests</c>. This codebase has no such escort base or quest
///     registry — escortables derive from <see cref="BaseEscortable" /> and get their quest from the ML
///     quest system by type — and <c>EscortToWrongEntrance</c> has not been ported, so the <c>Quests</c>
///     override is dropped. Everything else (the invulnerability until controlled, the plate suit) is the
///     original's; <c>SetWearable(...)</c> is <see cref="Mobile.AddItem" /> here.
/// </summary>
[SerializationGenerator(0, false)]
public partial class WrongPrisoner : BaseEscortable
{
    [Constructible]
    public WrongPrisoner()
    {
        Title = "заключённый";
        IsPrisoner = true;
        Female = false;
        Body = 0x190;
        Hue = 33802;
        Name = RussianNames.RandomMale();

        AddItem(new PlateChest());
        AddItem(new PlateArms());
        AddItem(new PlateGloves());
        AddItem(new PlateLegs());
    }

    public override bool IsInvulnerable => !Controlled;

    public override void InitOutfit()
    {
    }
}
