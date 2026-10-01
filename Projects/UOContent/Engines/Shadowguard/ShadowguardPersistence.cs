using System.Collections.Generic;
using Server.Items;

namespace Server.Engines.Shadowguard;

/// <summary>
///     Saves the runtime state ServUO's ShadowguardController used to write inline in its own
///     Item.Serialize/Deserialize — in-progress encounters, each player's completed-tower table,
///     and any leftover addon references. Kept separate from the controller Item itself because
///     ShadowguardEncounter has its own hand-rolled Serialize/Deserialize (matching ServUO), which
///     doesn't fit ModernUO's per-field codegen attributes.
/// </summary>
public class ShadowguardPersistence : GenericPersistence
{
    // Priority 1 (same tier as MobilePersistence) meant Deserialize ran before
    // ItemPersistence (priority 2) had restored ShadowguardController itself — Instance was
    // still null for the whole method, every controller?.AddEncounter/Table/Addons write
    // silently no-op'd. Priority 10 matches every other GenericPersistence that depends on
    // Items already being loaded (ChampionTitleSystem/FactionSystem/StealableArtifacts).
    public ShadowguardPersistence() : base("Shadowguard", 10)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        var controller = ShadowguardController.Instance;

        if (controller?.Deleted != false)
        {
            writer.WriteEncodedInt(0);
            writer.WriteEncodedInt(0);
            writer.WriteEncodedInt(0);
            return;
        }

        writer.WriteEncodedInt(controller.Encounters.Count);
        foreach (var encounter in controller.Encounters)
        {
            writer.WriteEncodedInt((int)encounter.Encounter);
            encounter.Serialize(writer);
        }

        writer.WriteEncodedInt(controller.Table?.Count ?? 0);
        if (controller.Table != null)
        {
            foreach (var (m, type) in controller.Table)
            {
                writer.Write(m);
                writer.WriteEncodedInt((int)type);
            }
        }

        writer.WriteEncodedInt(controller.Addons.Count);
        foreach (var addon in controller.Addons)
        {
            writer.Write(addon);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var controller = ShadowguardController.Instance;

        var encounterCount = reader.ReadEncodedInt();
        for (var i = 0; i < encounterCount; i++)
        {
            var type = (EncounterType)reader.ReadEncodedInt();
            var encounter = ShadowguardEncounter.ConstructEncounter(type);
            encounter.Deserialize(reader);

            controller?.AddEncounter(encounter);
        }

        var tableCount = reader.ReadEncodedInt();
        for (var i = 0; i < tableCount; i++)
        {
            var m = reader.ReadEntity<Mobile>();
            var type = (EncounterType)reader.ReadEncodedInt();

            if (m == null || controller == null)
            {
                continue;
            }

            controller.Table ??= new Dictionary<Mobile, EncounterType>();
            controller.Table[m] = type;
        }

        var addonCount = reader.ReadEncodedInt();
        for (var i = 0; i < addonCount; i++)
        {
            if (reader.ReadEntity<Item>() is BaseAddon addon)
            {
                controller?.Addons.Add(addon);
            }
        }
    }
}
