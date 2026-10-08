using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items
{
    [Flippable(0x13B2, 0x13B1)]
    [SerializationGenerator(0, false)]
    public partial class JukaBow : Bow
    {
        [Constructible]
        public JukaBow()
        {
            Resource = CraftResource.RegularWood;
        }

        public override int AosStrengthReq => 80;
        public override int AosDexterityReq => 80;

        public override int OldStrengthReq => 80;
        public override int OldDexterityReq => 80;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsModified => Hue == 0x453;

        public override void OnDoubleClick(Mobile from)
        {
            if (IsModified)
            {
                from.SendMessage("Это уже переделано.");
            }
            else if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("Чтобы это изменить, положи в рюкзак.");
            }
            else if (from.Skills.Fletching.Base < 100.0)
            {
                from.SendMessage("Переделать это оружие может только мастер лучного дела.");
            }
            else
            {
                from.BeginTarget(2, false, TargetFlags.None, OnTargetGears);
                from.SendMessage("Выбери шестерни.");
            }
        }

        public void OnTargetGears(Mobile from, object targ)
        {
            if (targ is not Gears g || !g.IsChildOf(from.Backpack))
            {
                from.SendMessage(
                    "Это не шестерни."
                ); // Apparently gears that aren't in your backpack aren't really gears at all. :-(
            }
            else if (IsModified)
            {
                from.SendMessage("Это уже переделано.");
            }
            else if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("Чтобы это изменить, положи в рюкзак.");
            }
            else if (from.Skills.Fletching.Base < 100.0)
            {
                from.SendMessage("Переделать это оружие может только мастер лучного дела.");
            }
            else
            {
                g.Consume();

                Hue = 0x453;
                Slayer = (SlayerName)Utility.Random(2, 25);

                from.SendMessage("Ты переделываешь это.");
            }
        }
    }
}
