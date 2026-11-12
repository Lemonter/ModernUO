using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBHerbalist : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Ginseng), 3, 20, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Garlic), 3, 20, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), 3, 20, 0xF86, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), 3, 20, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), 5, 20, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(MortarPestle), 8, 20, 0xE9B, 0));
                Add(new GenericBuyInfo(typeof(Bottle), 5, 20, 0xF0E, 0));

                // Mahaon: инструмент травничества — у того, у кого его естественно искать.
                Add(new GenericBuyInfo("ступка травника", typeof(MahaonHerbalistMortar), 15, 20, 0xE9B, 0x0595));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(Bloodmoss), 3);
                Add(typeof(MandrakeRoot), 2);
                Add(typeof(Garlic), 2);
                Add(typeof(Ginseng), 2);
                Add(typeof(Nightshade), 2);
                Add(typeof(Bottle), 3);
                Add(typeof(MortarPestle), 4);

                // Mahaon: травник скупает дикие травы — собирателю есть куда сбыть сбор,
                // даже если варить он не умеет. Цена растёт с редкостью травы.
                Add(typeof(MahaonHerbalistMortar), 7);
                Add(typeof(MahaonPlantain), 1);
                Add(typeof(MahaonYarrow), 2);
                Add(typeof(MahaonStJohnsWort), 2);
                Add(typeof(MahaonComfrey), 3);
                Add(typeof(MahaonWormwood), 3);
                Add(typeof(MahaonMountainRoot), 5);
                Add(typeof(MahaonSnowbloom), 6);
                Add(typeof(MahaonMoonwort), 8);
            }
        }
    }
}
