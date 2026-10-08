using Server.Items;

namespace Server.Spells.Mysticism;

/// <summary>Mysticism 678. Ported from ServUO
/// (Scripts/Spells/Mysticism/SpellDefinitions/HealingStoneSpell.cs); registration was already
/// in Spells/Initializer.cs, commented out.
///
/// Puts a HealingStone (Items/Consumables/HealingStone.cs) in the caster's pack, replacing
/// any stone already there — you only ever carry one.</summary>
public class HealingStoneSpell : MysticSpell
{
    private static readonly SpellInfo _info = new(
        "Healing Stone",
        "Kal In Mani",
        230,
        9022,
        Reagent.Bone,
        Reagent.Garlic,
        Reagent.Ginseng,
        Reagent.SpidersSilk
    );

    public HealingStoneSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public override void OnCast()
    {
        if (CheckSequence())
        {
            var pack = Caster.Backpack;

            if (pack == null)
            {
                FinishSequence();
                return;
            }

            // Only one stone at a time — the old one goes.
            foreach (var old in pack.FindItemsByType<HealingStone>())
            {
                old.Delete();
            }

            var skill = Caster.Skills[CastSkill].Value + Caster.Skills[DamageSkill].Value;

            var amount = (int)(skill * 1.25);
            var maxHeal = (int)(skill / 6);

            var stone = new HealingStone(amount, maxHeal);

            if (Caster.PlaceInBackpack(stone))
            {
                Caster.PlaySound(0x650);
                Caster.FixedParticles(0x3779, 10, 15, 5012, EffectLayer.Waist);
                Caster.SendLocalizedMessage(1080115); // A Healing Stone has been placed in your backpack.
            }
            else
            {
                stone.Delete();
                Caster.SendLocalizedMessage(502385); // Your pack cannot hold this item.
            }
        }

        FinishSequence();
    }
}
