using System.Collections.Generic;
using Server.Items;

namespace Server.Engines.Craft
{
    public enum CraftMarkOption
    {
        MarkItem,
        DoNotMark,
        PromptForMark
    }

    public class CraftContext
    {
        public CraftContext()
        {
            Items = new List<CraftItem>();
            LastResourceIndex = -1;
            LastResourceIndex2 = -1;
            LastGroupIndex = -1;
        }

        public List<CraftItem> Items { get; }

        public int LastResourceIndex { get; set; }

        public int LastResourceIndex2 { get; set; }

        public int LastGroupIndex { get; set; }

        public bool DoNotColor { get; set; }

        /// <summary>
        ///     Учебная работа: ресурсы тратятся и навык растёт как обычно, но готовое
        ///     изделие в рюкзак не попадает, а следующая попытка запускается сама — пока
        ///     есть из чего делать и цел инструмент.
        ///
        ///     Живёт в контексте, а не в самом предмете: контекст и так у каждого игрока
        ///     свой на каждую ремесленную систему, и галочка обязана быть именно такой —
        ///     кузнец может точить болванки вхолостую, оставаясь при этом обычным портным.
        ///     Сохранять нечего, контекст и сам не переживает перезапуск.
        /// </summary>
        public bool Practice { get; set; }

        public CraftMarkOption MarkOption { get; set; }

        // T2A: last hue used for hue-aware crafting (tailoring cloth)
        public int LastHue { get; set; } = -1;

        // T2A jewelry: transient gem info set by GemSelectTarget, consumed by BaseJewel.OnCraft
        public GemType PendingGemType { get; set; }
        public int PendingGemCount { get; set; }

        public CraftItem LastMade
        {
            get
            {
                if (Items.Count > 0)
                {
                    return Items[0];
                }

                return null;
            }
        }

        public void OnMade(CraftItem item)
        {
            Items.Remove(item);

            if (Items.Count == 10)
            {
                Items.RemoveAt(9);
            }

            Items.Insert(0, item);
        }
    }
}
