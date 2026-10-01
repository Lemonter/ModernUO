using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

/// <summary>A rare drop off the Underworld's own creatures. Ported from ServUO
/// (Scripts/Items/Quest/SAQuestItems.cs). Toss it into the Fountain of Fortune — the only
/// "sacred waters" in the game — for whatever the fountain feels like giving you.</summary>
[SerializationGenerator(0, false)]
public partial class LuckyCoin : Item, ICommodity
{
    [Constructible]
    public LuckyCoin(int amount = 1) : base(0xF87)
    {
        Stackable = true;
        Amount = amount;
        Hue = 1174;
    }

    public override int LabelNumber => 1113366; // lucky coin

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack) || Amount < 1)
        {
            return;
        }

        from.SendLocalizedMessage(1113367); // Make a wish then toss me into sacred waters!!
        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private readonly LuckyCoin _coin;

        public InternalTarget(LuckyCoin coin) : base(3, false, TargetFlags.None) => _coin = coin;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is AddonComponent { Addon: FountainOfFortune fountain })
            {
                fountain.OnTarget(from, _coin);
                return;
            }

            from.SendLocalizedMessage(1113369); // That is not sacred waters. Try looking in the Underworld.
        }
    }
}

/// <summary>Cut from an iron beetle that went down without its plates being chewed up. Ported
/// from ServUO (Scripts/Items/Quest/SAQuestItems.cs); Ter Mur's tiered quests ask for ten.</summary>
[SerializationGenerator(0, false)]
public partial class UndamagedIronBeetleScale : Item, ICommodity
{
    [Constructible]
    public UndamagedIronBeetleScale(int amount = 1) : base(0x26B3)
    {
        Stackable = true;
        Amount = amount;
    }

    public override int LabelNumber => 1112905; // Undamaged Iron Beetle Scale

    int ICommodity.DescriptionNumber => LabelNumber;
    bool ICommodity.IsDeedable => true;
}
