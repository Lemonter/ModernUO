using ModernUO.Serialization;

namespace Server.Mobiles
{
    /// <summary>
    ///     Simplest possible quest-giver — stationary, friendly, no vendor/shop logic at
    ///     all, just hands quest talk off to MayorQuestSystem on double-click. Place one
    ///     per city via [AddLem, set CityName in Properties for the overhead message to
    ///     make sense (not otherwise used mechanically yet — quests aren't
    ///     city-specific in this first version, any mayor gives the same kind of job).
    /// </summary>
    [SerializationGenerator(0, false)]
    public partial class MahaonMayor : BaseCreature
    {
        [SerializableField(0)]
        private string _cityName;

        [Constructible]
        public MahaonMayor() : base(AIType.AI_Vendor)
        {
            Body = 0x190;
            Name = "мэр";
            Hue = Race.Human.RandomSkinHue();
            Title = "мэр города";

            SetStr(60, 70);
            SetDex(50, 60);
            SetInt(50, 60);
            SetHits(50, 60);

            CantWalk = true;
            Blessed = true; // never actually fightable — a quest-giver, not a target

            AddItem(new Items.FancyShirt { Hue = Utility.RandomNondyedHue() });
            AddItem(new Items.LongPants { Hue = Utility.RandomNondyedHue() });
            AddItem(new Items.Boots());
            AddItem(new Items.FeatheredHat());
        }

        public override bool ClickTitle => true;
        public override bool ShowFameTitle => false;

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(Location, 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            Systems.MahaonQuests.MayorQuestSystem.Talk(from, this);
        }
    }
}
