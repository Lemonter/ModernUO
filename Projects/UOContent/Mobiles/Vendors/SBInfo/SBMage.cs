using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles
{
    public class SBMage : SBInfo
    {
        public override IShopSellInfo SellInfo { get; } = new InternalSellInfo();

        public override List<GenericBuyInfo> BuyInfo { get; } = new InternalBuyInfo();

        public class InternalBuyInfo : List<GenericBuyInfo>
        {
            public InternalBuyInfo()
            {
                // Mahaon: every casting-discipline book sold here, all at a flat 10gp
                // (was individually priced 18/115/etc.) — user's explicit ask, "все книги
                // кастов ... по 10 голды". Chivalry/Bushido/Ninjitsu/Spellweaving/Mysticism
                // added alongside the two that were already here (Magery/Necromancy); era
                // gates match how NecromancerSpellbook was already gated behind Core.AOS.
                Add(new GenericBuyInfo(typeof(Spellbook), 10, 10, 0xEFA, 0));

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

                    // Mahaon: all 16 Mysticism scrolls, same flat 10gp as every other
                    // discipline's scrolls below — moved here from the court mage's own
                    // short-lived shop per the shard owner's ask ("перенеси в продажу нпс
                    // маг, которые уже продают рунбуки и свитки другие"). Spell IDs run
                    // 677-692 (16, matches MysticSpellbook.BookCount) with itemIDs
                    // sequential right alongside (0x2D9E-0x2DAD) — confirmed against each
                    // scroll class's own hardcoded base(spellId, itemId) constructor call.
                    Type[] mysticismScrolls =
                    {
                        typeof(NetherBoltScroll), typeof(HealingStoneScroll), typeof(PurgeMagicScroll),
                        typeof(EnchantScroll), typeof(SleepScroll), typeof(EagleStrikeScroll),
                        typeof(AnimatedWeaponScroll), typeof(StoneFormScroll), typeof(SpellTriggerScroll),
                        typeof(MassSleepScroll), typeof(CleansingWindsScroll), typeof(BombardScroll),
                        typeof(SpellPlagueScroll), typeof(HailStormScroll), typeof(NetherCycloneScroll),
                        typeof(RisingColossusScroll)
                    };

                    for (var i = 0; i < mysticismScrolls.Length; i++)
                    {
                        Add(new GenericBuyInfo(mysticismScrolls[i], 10, 20, 0x2D9E + i, 0));
                    }
                }

                Add(new GenericBuyInfo(typeof(ScribesPen), 8, 10, 0xFBF, 0));

                Add(new GenericBuyInfo(typeof(BlankScroll), 5, 20, 0x0E34, 0));

                Add(new GenericBuyInfo("1041072", typeof(MagicWizardsHat), 11, 10, 0x1718, Utility.RandomDyedHue()));

                Add(new GenericBuyInfo(typeof(RecallRune), 15, 10, 0x1F14, 0));

                // Mahaon: moved here from the court mage's own short-lived shop, same ask
                // as the Mysticism scrolls above — no other vendor sold one at all before.
                Add(new GenericBuyInfo(typeof(Runebook), 500, 20, 0x22C5, 0));

                // Mahaon: SoulCatcherTattooNeedle already exists and works ([Constructible],
                // GM-spawnable) but had no in-game way for a normal player to obtain one —
                // every mage vendor now sells it directly.
                // Пять тысяч, как и у мистика: тату теперь навсегда, а не на неделю, и за четыреста
                // золота такое отдавать нелепо — вся возня с камнями душ обесценивалась.
                Add(new GenericBuyInfo(typeof(SoulCatcherTattooNeedle), 5000, 5, 0x0F9F, 0));

                Add(new GenericBuyInfo(typeof(RefreshPotion), 15, 10, 0xF0B, 0));
                Add(new GenericBuyInfo(typeof(AgilityPotion), 15, 10, 0xF08, 0));
                Add(new GenericBuyInfo(typeof(NightSightPotion), 15, 10, 0xF06, 0));
                Add(new GenericBuyInfo(typeof(LesserHealPotion), 15, 10, 0xF0C, 0));
                Add(new GenericBuyInfo(typeof(StrengthPotion), 15, 10, 0xF09, 0));
                Add(new GenericBuyInfo(typeof(LesserPoisonPotion), 15, 10, 0xF0A, 0));
                Add(new GenericBuyInfo(typeof(LesserCurePotion), 15, 10, 0xF07, 0));
                Add(new GenericBuyInfo(typeof(LesserExplosionPotion), 21, 10, 0xF0D, 0));

                Add(new GenericBuyInfo(typeof(BlackPearl), 5, 20, 0xF7A, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), 5, 20, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(Garlic), 3, 20, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), 3, 20, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), 3, 20, 0xF86, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), 3, 20, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(SpidersSilk), 3, 20, 0xF8D, 0));
                Add(new GenericBuyInfo(typeof(SulfurousAsh), 3, 20, 0xF8C, 0));

                if (Core.AOS)
                {
                    Add(new GenericBuyInfo(typeof(BatWing), 3, 999, 0xF78, 0));
                    Add(new GenericBuyInfo(typeof(DaemonBlood), 6, 999, 0xF7D, 0));
                    Add(new GenericBuyInfo(typeof(PigIron), 5, 999, 0xF8A, 0));
                    Add(new GenericBuyInfo(typeof(NoxCrystal), 6, 999, 0xF8E, 0));
                    Add(new GenericBuyInfo(typeof(GraveDust), 3, 999, 0xF8F, 0));
                }

                // Mahaon: all 8 circles now (was capped at the first 3), flat 10gp each —
                // same "все свитки ... по 10 голды" ask as the books above. itemID formula
                // unchanged from the stock 3-circle version (a pre-existing, harmless
                // cosmetic quirk in the buy-gump icon for the first scroll — the actually
                // purchased item always uses its own type's real, correct graphic).
                var types = Loot.RegularScrollTypes;

                for (var i = 0; i < types.Length; ++i)
                {
                    var itemID = 0x1F2E + i;

                    if (i == 6)
                    {
                        itemID = 0x1F2D;
                    }
                    else if (i > 6)
                    {
                        --itemID;
                    }

                    Add(new GenericBuyInfo(types[i], 10, 20, itemID, 0));
                }

                // Necromancy scrolls — itemIDs are sequential per-circle (0x2260 + circle
                // offset), confirmed against each scroll class's own hardcoded graphic.
                if (Core.AOS)
                {
                    var necroTypes = Core.SE ? Loot.SENecromancyScrollTypes : Loot.NecromancyScrollTypes;

                    for (var i = 0; i < necroTypes.Length; ++i)
                    {
                        Add(new GenericBuyInfo(necroTypes[i], 10, 20, 0x2260 + i, 0));
                    }
                }

                // Spellweaving (Arcanist) scrolls — same sequential-itemID confirmation as
                // Necromancy above (0x2D51 + circle offset), matching
                // Loot.ArcanistScrollTypes' own order (SummonFey/SummonFiend are commented
                // out there, so this naturally skips them too).
                if (Core.ML)
                {
                    var arcanistTypes = Loot.ArcanistScrollTypes;

                    for (var i = 0; i < arcanistTypes.Length; ++i)
                    {
                        var itemID = 0x2D51 + i;

                        if (i >= 6) // SummonFey/SummonFiend (0x2D57/0x2D58) are skipped in this array
                        {
                            itemID += 2;
                        }

                        Add(new GenericBuyInfo(arcanistTypes[i], 10, 20, itemID, 0x8FD));
                    }
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(WizardsHat), 15);
                Add(typeof(Runebook), 200);
                Add(typeof(BlackPearl), 3);
                Add(typeof(Bloodmoss), 4);
                Add(typeof(MandrakeRoot), 2);
                Add(typeof(Garlic), 2);
                Add(typeof(Ginseng), 2);
                Add(typeof(Nightshade), 2);
                Add(typeof(SpidersSilk), 2);
                Add(typeof(SulfurousAsh), 2);

                if (Core.AOS)
                {
                    Add(typeof(BatWing), 1);
                    Add(typeof(DaemonBlood), 3);
                    Add(typeof(PigIron), 2);
                    Add(typeof(NoxCrystal), 3);
                    Add(typeof(GraveDust), 1);
                }

                Add(typeof(RecallRune), 13);
                Add(typeof(Spellbook), 9);

                var types = Loot.RegularScrollTypes;

                for (var i = 0; i < types.Length; ++i)
                {
                    Add(types[i], (i / 8 + 2) * 2);
                }

                if (Core.SE)
                {
                    Add(typeof(ExorcismScroll), 3);
                    Add(typeof(AnimateDeadScroll), 8);
                    Add(typeof(BloodOathScroll), 8);
                    Add(typeof(CorpseSkinScroll), 8);
                    Add(typeof(CurseWeaponScroll), 8);
                    Add(typeof(EvilOmenScroll), 8);
                    Add(typeof(PainSpikeScroll), 8);
                    Add(typeof(SummonFamiliarScroll), 8);
                    Add(typeof(HorrificBeastScroll), 8);
                    Add(typeof(MindRotScroll), 10);
                    Add(typeof(PoisonStrikeScroll), 10);
                    Add(typeof(WraithFormScroll), 15);
                    Add(typeof(LichFormScroll), 16);
                    Add(typeof(StrangleScroll), 16);
                    Add(typeof(WitherScroll), 16);
                    Add(typeof(VampiricEmbraceScroll), 20);
                    Add(typeof(VengefulSpiritScroll), 20);
                }
            }
        }
    }
}
