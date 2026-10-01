using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonQoL;

/// <summary>
///     QoL: hides, meat, feathers, and scales show up in the corpse automatically instead
///     of requiring a knife and the carving interaction first. Reuses the same type
///     mappings BaseCreature's own carving code uses, just applied at death instead of on
///     a manual carve.
/// </summary>
public static class MonsterLootDropSystem
{
    [OnEvent(nameof(CreatureEvents.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature bc)
    {
        var corpse = bc.Corpse as Corpse;
        if (corpse == null || corpse.Deleted)
        {
            return;
        }

        var yieldMultiplier = 1;

        if (bc.LastKiller is Mobiles.PlayerMobile killer)
        {
            var profession = Systems.MahaonProfessions.ProfessionSystem.GetProfession(killer);
            if (profession != null &&
                Systems.MahaonProfessions.ProfessionData.All[profession.Value].Category ==
                Systems.MahaonProfessions.ProfessionCategory.Ranger)
            {
                yieldMultiplier = 2; // rangers know how to work a kill properly
            }
        }

        if (bc.Hides > 0)
        {
            Item hide = bc.HideType switch
            {
                HideType.Regular => new Leather(bc.Hides * yieldMultiplier),
                HideType.Spined  => new SpinedLeather(bc.Hides * yieldMultiplier),
                HideType.Horned  => new HornedLeather(bc.Hides * yieldMultiplier),
                HideType.Barbed  => new BarbedLeather(bc.Hides * yieldMultiplier),
                _                => null
            };

            if (hide != null)
            {
                corpse.DropItem(hide);
            }
        }

        if (bc.Meat > 0)
        {
            Item meat = bc.MeatType switch
            {
                MeatType.Ribs    => new RawRibs(bc.Meat * yieldMultiplier),
                MeatType.Bird    => new RawBird(bc.Meat * yieldMultiplier),
                MeatType.LambLeg => new RawLambLeg(bc.Meat * yieldMultiplier),
                _                => null
            };

            if (meat != null)
            {
                corpse.DropItem(meat);
            }
        }

        if (bc.Feathers > 0)
        {
            corpse.DropItem(new Feather(bc.Feathers * yieldMultiplier));
        }

        if (bc.Scales > 0)
        {
            DropScales(corpse, bc.ScaleType, bc.Scales * yieldMultiplier);
        }
    }

    private static void DropScales(Corpse corpse, ScaleType type, int amount)
    {
        switch (type)
        {
            case ScaleType.Red:
                corpse.DropItem(new RedScales(amount));
                break;
            case ScaleType.Yellow:
                corpse.DropItem(new YellowScales(amount));
                break;
            case ScaleType.Black:
                corpse.DropItem(new BlackScales(amount));
                break;
            case ScaleType.Green:
                corpse.DropItem(new GreenScales(amount));
                break;
            case ScaleType.White:
                corpse.DropItem(new WhiteScales(amount));
                break;
            case ScaleType.Blue:
                corpse.DropItem(new BlueScales(amount));
                break;
            case ScaleType.All:
                corpse.DropItem(new RedScales(amount));
                corpse.DropItem(new YellowScales(amount));
                corpse.DropItem(new BlackScales(amount));
                corpse.DropItem(new GreenScales(amount));
                corpse.DropItem(new WhiteScales(amount));
                corpse.DropItem(new BlueScales(amount));
                break;
        }
    }
}
