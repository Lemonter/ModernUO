using System.Collections.Generic;
using Server.Items;

namespace Server.Engines.VvV;

// Ported from real OSI/ServUO content (Scripts/Services/ViceVsVirtue/VvVRewards.cs). Dropped
// entries whose item type doesn't exist in this codebase: MaceAndShieldGlasses,
// VesperOrderShield, ClaininsSpellbook, CrystallineRing, WizardsCrystalGlasses,
// PrimerOnArmsTalisman, HumanFeyLeggings, VvVEpaulette/VvVGargishEpaulette/
// VvVGargishPlateArms/VvVGargishStoneChest (see VvVSteeds.cs/MiscRewards.cs headers).
public static class VvVRewards
{
    public static List<CollectionItem> Rewards { get; private set; }

    public static void Configure()
    {
        Rewards = new List<CollectionItem>
        {
            new(typeof(VvVPotionKeg), 6870, 1094764, 437, 500),  // Greater Stam
            new(typeof(VvVPotionKeg), 6870, 1094718, 13, 500),   // Supernova
            new(typeof(VvVPotionKeg), 6870, 1155541, 2500, 500), // Stat Loss Removal
            new(typeof(VvVPotionKeg), 6870, 1155543, 2543, 500), // Anti Paralysis

            new(typeof(EssenceOfCourage), 3838, 1155554, 2718, 250), // Essence of Courage

            new(typeof(VvVSteedStatuette), 8484, 1155545, ViceVsVirtueSystem.VirtueHue, 500), // Virtue War Horse
            new(typeof(VvVSteedStatuette), 8484, 1155545, ViceVsVirtueSystem.ViceHue, 500),   // Vice War Horse
            new(typeof(VvVSteedStatuette), 8501, 1155546, ViceVsVirtueSystem.VirtueHue, 500), // Virtue War Ostard
            new(typeof(VvVSteedStatuette), 8501, 1155546, ViceVsVirtueSystem.ViceHue, 500),   // Vice War Ostard

            new(typeof(VvVHairDye), 3838, 1155538, ViceVsVirtueSystem.VirtueHue, 2500), // Virtue Hair Dye
            new(typeof(VvVHairDye), 3838, 1155539, ViceVsVirtueSystem.ViceHue, 2500),   // Vice Hair Dye

            new(typeof(VvVTrapKit), 7866, 1155527, 0, 250), // Poison Trap Kit
            new(typeof(VvVTrapKit), 7866, 1155528, 0, 250), // Freezing Trap Kit
            new(typeof(VvVTrapKit), 7866, 1155529, 0, 250), // Shocking Trap Kit
            new(typeof(VvVTrapKit), 7866, 1155530, 0, 250), // Blades Trap Kit
            new(typeof(VvVTrapKit), 7866, 1155531, 0, 250), // Explosion Trap Kit

            new(typeof(CannonTurretPlans), 5360, 1155503, 0, 3000), // Cannon Turret
            new(typeof(ManaSpike), 2308, 1155508, 0, 1000),         // Mana Spike

            new(typeof(ForgedRoyalPardon), 18098, 1155524, 0, 10000),        // Royal Forged Pardon
            new(typeof(ScrollofTranscendence), 5360, 1094934, 0x490, 10000), // Scroll of Transcendence

            new(typeof(VvVRobe), 9859, 1155532, ViceVsVirtueSystem.VirtueHue, 5000), // virtue robe
            new(typeof(VvVRobe), 9859, 1155533, ViceVsVirtueSystem.ViceHue, 5000),   // vice robe

            new(typeof(CovetousTileDeed), 5360, 1155516, 0, 10000), // Covetous Tile
            new(typeof(DeceitTileDeed), 5360, 1155517, 0, 10000),   // Deceit Tile
            new(typeof(DespiseTileDeed), 5360, 1155518, 0, 10000),  // Despise Tile
            new(typeof(DestardTileDeed), 5360, 1155519, 0, 10000),  // Destard Tile
            new(typeof(HythlothTileDeed), 5360, 1155520, 0, 10000), // Hythloth Tile
            new(typeof(PrideTileDeed), 5360, 1155521, 0, 10000),    // Pride Tile
            new(typeof(ShameTileDeed), 5360, 1155522, 0, 10000),    // Shame Tile
            new(typeof(WrongTileDeed), 5360, 1155523, 0, 10000),    // Wrong Tile

            new(typeof(FoldedSteelGlasses), 12216, 0, 1150, 500),
            new(typeof(SpiritOfTheTotem), 5445, 0, 1109, 500),
            new(typeof(TomeOfLostKnowledge), 3834, 0, 1328, 500),
            new(typeof(HuntersHeaddress), 5447, 0, 1428, 500),
            new(typeof(HeartOfTheLion), 5141, 0, 1281, 500),
            new(typeof(CrimsonCincture), 5435, 0, 1157, 500),
            new(typeof(RingOfTheVile), 4234, 0, 1271, 500),
            new(typeof(RuneBeetleCarapace), 10109, 0, 0, 500),
            new(typeof(KasaOfTheRajin), 10136, 0, 0, 500),
            new(typeof(OrnamentOfTheMagician), 4230, 0, 1364, 500),
            new(typeof(InquisitorsResolution), 5140, 0, 1266, 500),
            new(typeof(Stormgrip), 10130, 0, 0, 500),

            new(typeof(VvVWand1), 3571, 0, 0, 500),
            new(typeof(VvVWand2), 3571, 0, 0, 500),
            new(typeof(VvVWizardsHat), 5912, 0, 0, 500),
            new(typeof(VvVWoodlandArms), 11116, 0, 0, 500),
            new(typeof(VvVDragonArms), 9815, 0, 0, 500),
            new(typeof(VvVPlateArms), 5136, 0, 0, 500),
            new(typeof(VvVStuddedChest), 5083, 0, 0, 500),
            new(typeof(VvVGargishEarrings), 16915, 0, 0, 500),

            new(typeof(CompassionBanner), 39351, 1123375, 0, 10000),
            new(typeof(HonestyBanner), 39353, 1123377, 0, 10000),
            new(typeof(HonorBanner), 39355, 1123379, 0, 10000),
            new(typeof(HumilityBanner), 39357, 1123381, 0, 10000),
            new(typeof(JusticeBanner), 39359, 1123383, 0, 10000),
            new(typeof(SacraficeBanner), 39361, 1123385, 0, 10000),
            new(typeof(SpiritualityBanner), 39363, 1123387, 0, 10000),
            new(typeof(ValorBanner), 39365, 1123389, 0, 10000),

            new(typeof(CovetousBanner), 39335, 1123359, 0, 10000),
            new(typeof(DeceitBanner), 39337, 1123361, 0, 10000),
            new(typeof(DespiseBanner), 39339, 1123363, 0, 10000),
            new(typeof(DestardBanner), 39341, 1123365, 0, 10000),
            new(typeof(HythlothBanner), 39343, 1123367, 0, 10000),
            new(typeof(PrideBanner), 39345, 1123369, 0, 10000),
            new(typeof(ShameBanner), 39347, 1123371, 0, 10000),
            new(typeof(WrongBanner), 39349, 1123373, 0, 10000)
        };
    }

    public static void OnRewardItemCreated(Mobile from, Item item)
    {
        if (item is IOwnerRestricted owned)
        {
            owned.Owner = from;
        }

        if (item is IAccountRestricted accountRestricted && from.Account != null)
        {
            accountRestricted.Account = from.Account.Username;
        }

        ViceVsVirtueSystem.Instance.AddVvVItem(item, true);
    }
}
