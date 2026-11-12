using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class MahaonMystic : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public MahaonMystic() : base("мистик")
    {
        SetSkill(SkillName.Magery, 80.0, 100.0);
        Title = "Мистик";
    }

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override VendorShoeType ShoeType => VendorShoeType.Sandals;

    public override void InitOutfit()
    {
        base.InitOutfit();
        AddItem(new Robe(Utility.RandomNondyedHue()));
    }

    public override void InitSBInfo()
    {
        _sbInfos.Add(new SBMahaonMystic());
    }
}

public class SBMahaonMystic : SBInfo
{
    public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

    public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

    public class InternalBuyInfo : List<GenericBuyInfo>
    {
        public InternalBuyInfo()
        {
            Add(new GenericBuyInfo(typeof(SoulCatcherTattooNeedle), 5000, 20, 0x0F9F, 0));
            Add(new GenericBuyInfo(typeof(SakuroCraftingTool), 8000, 20, 0x0FB4, 0));

            // Книга шамана (см. dev-docs/shamanism): вход в школу Шаманства. Цена — как за
            // серьёзный инструмент, но доступна с первых недель: сама книга ничего не даёт
            // без навыка, а вершины всё равно закрыты специализацией и редкими компонентами.
            Add(new GenericBuyInfo(typeof(ShamanBook), 2500, 20, 0x0FBD, 0x48D));

            // Книги остальных школ — перенесены от мага (SBMage) по просьбе владельца шарда:
            // у мистика теперь все «чужие» школы магии разом. Цена прежняя, плоские 10 золотых,
            // и те же проверки эпохи, что стояли у мага.
            if (Core.AOS)
            {
                Add(new GenericBuyInfo(typeof(NecromancerSpellbook), 10, 10, 0x2253, 0));
            }

            if (Core.SE)
            {
                Add(new GenericBuyInfo(typeof(BookOfChivalry), 10, 10, 0x2252, 0));
                Add(new GenericBuyInfo(typeof(BookOfBushido), 10, 10, 0x238C, 0));
                Add(new GenericBuyInfo(typeof(BookOfNinjitsu), 10, 10, 0x23A0, 0));
            }

            if (Core.ML)
            {
                Add(new GenericBuyInfo(typeof(SpellweavingBook), 10, 10, 0x2D50, 0x8A2));
            }

            if (Core.SA)
            {
                Add(new GenericBuyInfo(typeof(MysticSpellbook), 10, 10, 0x2D9D, 0));
            }
        }
    }

    public class InternalSellInfo : GenericSellInfo
    {
        public InternalSellInfo()
        {
            Add(typeof(SoulCatcherTattooNeedle), 1500);
            Add(typeof(SakuroCraftingTool), 2500);
            Add(typeof(ShamanBook), 600);
        }
    }
}
