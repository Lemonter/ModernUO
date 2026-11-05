using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. IRepairable и его RepairSystem в репозитории отсутствуют, как и
///     виртуальное свойство CanBeWornByGargoyles (заменено на RequiredRaces). IsArtifact
///     отсутствует и отброшен.
/// </summary>
[SerializationGenerator(0, false)]
public partial class VirtueShield : BaseShield
{
    [Constructible]
    public VirtueShield() : base(0x7818)
    {
        Attributes.SpellChanneling = 1;
        Attributes.DefendChance = 10;

        LootType = LootType.Blessed;
    }

    public override int BasePhysicalResistance => 8;
    public override int BaseFireResistance => 8;
    public override int BaseColdResistance => 8;
    public override int BasePoisonResistance => 8;
    public override int BaseEnergyResistance => 8;

    public override int LabelNumber => 1109616; // Virtue Shield

    public override int InitMinHits => 255;
    public override int InitMaxHits => 255;

    public override bool OnEquip(Mobile from)
    {
        var yes = base.OnEquip(from);

        if (yes)
        {
            Effects.PlaySound(from.Location, from.Map, 0x1F7);
            from.FixedParticles(0x376A, 9, 32, 5030, EffectLayer.Waist);
        }

        return yes;
    }
}
