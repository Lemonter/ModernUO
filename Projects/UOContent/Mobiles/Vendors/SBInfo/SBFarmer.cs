using System.Collections.Generic;
using Server.Items;
using Server.Systems.MahaonSeasons;
using Server.Systems.MahaonWorld;

namespace Server.Mobiles
{
    public class SBFarmer : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Cabbage), 5, 20, 0xC7B, 0));
                Add(new GenericBuyInfo(typeof(Cantaloupe), 6, 20, 0xC79, 0));
                Add(new GenericBuyInfo(typeof(Carrot), 3, 20, 0xC78, 0));
                Add(new GenericBuyInfo(typeof(HoneydewMelon), 7, 20, 0xC74, 0));
                Add(new GenericBuyInfo(typeof(Squash), 3, 20, 0xC72, 0));
                Add(new GenericBuyInfo(typeof(Lettuce), 5, 20, 0xC70, 0));
                Add(new GenericBuyInfo(typeof(Onion), 3, 20, 0xC6D, 0));
                Add(new GenericBuyInfo(typeof(Pumpkin), 11, 20, 0xC6A, 0));
                Add(new GenericBuyInfo(typeof(GreenGourd), 3, 20, 0xC66, 0));
                Add(new GenericBuyInfo(typeof(YellowGourd), 3, 20, 0xC64, 0));
                // Add( new GenericBuyInfo( typeof( Turnip ), 6, 20, XXXXXX, 0 ) );
                Add(new GenericBuyInfo(typeof(Watermelon), 7, 20, 0xC5C, 0));
                // Add( new GenericBuyInfo( typeof( EarOfCorn ), 3, 20, XXXXXX, 0 ) );
                Add(new GenericBuyInfo(typeof(Eggs), 3, 20, 0x9B5, 0));
                Add(new BeverageBuyInfo(typeof(Pitcher), BeverageType.Milk, 7, 20, 0x9AD, 0));
                Add(new GenericBuyInfo(typeof(Peach), 3, 20, 0x9D2, 0));
                Add(new GenericBuyInfo(typeof(Pear), 3, 20, 0x994, 0));
                Add(new GenericBuyInfo(typeof(Lemon), 3, 20, 0x1728, 0));
                Add(new GenericBuyInfo(typeof(Lime), 3, 20, 0x172A, 0));
                Add(new GenericBuyInfo(typeof(Grapes), 3, 20, 0x9D1, 0));
                Add(new GenericBuyInfo(typeof(Apple), 3, 20, 0x9D0, 0));
                Add(new GenericBuyInfo(typeof(SheafOfHay), 2, 20, 0xF36, 0));
                // Buy-list name has to stay Latin1/English here — the vendor buy-list
                // packet (0x74, OutgoingVendorBuyPackets.SendVendorBuyList) writes this
                // field via WriteLatin1Null, which can't represent Cyrillic at all (silently
                // becomes "?????" on the wire). Same fix as MahaonLibrarian's
                // SakuroBlueprintBuyInfo — the seed's own real item name stays Russian
                // wherever it's shown elsewhere (MahaonCropSeed's own DefaultName), this is
                // only the shop-window label.
                // Плуг — им готовят землю под посев: вспаханная грядка даёт вдвое больше.
                Add(new GenericBuyInfo("Plough", typeof(MahaonPlough), 120, 10, 0x1500, 0));

                Add(new GenericBuyInfo("Onion Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Onion }));
                Add(new GenericBuyInfo("Garlic Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Garlic }));
                Add(new GenericBuyInfo("Wheat Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Wheat }));
                Add(new GenericBuyInfo("Cotton Seeds", typeof(MahaonCropSeed), 12, 20, 0x0DCF, 0, new object[] { MahaonCropType.Cotton }));
                Add(new GenericBuyInfo("Flax Seeds", typeof(MahaonCropSeed), 12, 20, 0x0DCF, 0, new object[] { MahaonCropType.Flax }));
                Add(new GenericBuyInfo("Cabbage Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Cabbage }));
                Add(new GenericBuyInfo("Carrot Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Carrot }));
                Add(new GenericBuyInfo("Lettuce Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Lettuce }));
                Add(new GenericBuyInfo("Pumpkin Seeds", typeof(MahaonCropSeed), 10, 20, 0x0DCF, 0, new object[] { MahaonCropType.Pumpkin }));
                Add(new GenericBuyInfo("Turnip Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Turnip }));
                Add(new GenericBuyInfo("Corn Seeds", typeof(MahaonCropSeed), 10, 20, 0x0DCF, 0, new object[] { MahaonCropType.Corn }));
                // Женьшень — реагент, а не еда, поэтому и семена дороже прочих вчетверо.
                Add(new GenericBuyInfo("Ginseng Seeds", typeof(MahaonCropSeed), 32, 20, 0x0DCF, 0, new object[] { MahaonCropType.Ginseng }));

                Add(new GenericBuyInfo("Watermelon Seeds", typeof(MahaonCropSeed), 10, 20, 0x0DCF, 0, new object[] { MahaonCropType.Watermelon }));
                Add(new GenericBuyInfo("Honeydew Seeds", typeof(MahaonCropSeed), 10, 20, 0x0DCF, 0, new object[] { MahaonCropType.HoneydewMelon }));
                Add(new GenericBuyInfo("Canteloupe Seeds", typeof(MahaonCropSeed), 10, 20, 0x0DCF, 0, new object[] { MahaonCropType.Cantaloupe }));
                Add(new GenericBuyInfo("Squash Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.Squash }));
                Add(new GenericBuyInfo("Yellow Gourd Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.YellowGourd }));
                Add(new GenericBuyInfo("Green Gourd Seeds", typeof(MahaonCropSeed), 8, 20, 0x0DCF, 0, new object[] { MahaonCropType.GreenGourd }));
                Add(new GenericBuyInfo("Grape Seeds", typeof(MahaonCropSeed), 14, 20, 0x0DCF, 0, new object[] { MahaonCropType.Grapes }));

                // Мандрагора и паслён — реагенты; ценник как у женьшеня.
                Add(new GenericBuyInfo("Mandrake Seeds", typeof(MahaonCropSeed), 32, 20, 0x0DCF, 0, new object[] { MahaonCropType.Mandrake }));
                Add(new GenericBuyInfo("Nightshade Seeds", typeof(MahaonCropSeed), 32, 20, 0x0DCF, 0, new object[] { MahaonCropType.Nightshade }));

                // Саженцы. Плодовые дороже: с них потом снимают урожай каждую осень, а дуб
                // с орехом — это только брёвна.
                Add(new GenericBuyInfo("Apple Sapling", typeof(MahaonSapling), 250, 10, 0x0CE9, 0, new object[] { MahaonTreeSpecies.Apple }));
                Add(new GenericBuyInfo("Peach Sapling", typeof(MahaonSapling), 250, 10, 0x0CE9, 0, new object[] { MahaonTreeSpecies.Peach }));
                Add(new GenericBuyInfo("Pear Sapling", typeof(MahaonSapling), 250, 10, 0x0CE9, 0, new object[] { MahaonTreeSpecies.Pear }));
                Add(new GenericBuyInfo("Oak Sapling", typeof(MahaonSapling), 150, 10, 0x0CE9, 0, new object[] { MahaonTreeSpecies.Oak1 }));
                Add(new GenericBuyInfo("Great Oak Sapling", typeof(MahaonSapling), 150, 10, 0x0CE9, 0, new object[] { MahaonTreeSpecies.Oak2 }));
                Add(new GenericBuyInfo("Walnut Sapling", typeof(MahaonSapling), 150, 10, 0x0CE9, 0, new object[] { MahaonTreeSpecies.Walnut1 }));
                Add(new GenericBuyInfo("Great Walnut Sapling", typeof(MahaonSapling), 150, 10, 0x0CE9, 0, new object[] { MahaonTreeSpecies.Walnut2 }));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(MahaonPlough), 60);
                Add(typeof(MahaonCropSeed), 4);
                Add(typeof(MahaonSapling), 60);
                Add(typeof(Pitcher), 5);
                Add(typeof(Eggs), 1);
                Add(typeof(Apple), 1);
                Add(typeof(Grapes), 1);
                Add(typeof(Watermelon), 3);
                Add(typeof(YellowGourd), 1);
                Add(typeof(GreenGourd), 1);
                Add(typeof(Pumpkin), 5);
                Add(typeof(Onion), 1);
                Add(typeof(Lettuce), 2);
                Add(typeof(Squash), 1);
                Add(typeof(Carrot), 1);
                Add(typeof(HoneydewMelon), 3);
                Add(typeof(Cantaloupe), 3);
                Add(typeof(Cabbage), 2);
                Add(typeof(Lemon), 1);
                Add(typeof(Lime), 1);
                Add(typeof(Peach), 1);
                Add(typeof(Pear), 1);
                Add(typeof(SheafOfHay), 1);
            }
        }
    }
}
