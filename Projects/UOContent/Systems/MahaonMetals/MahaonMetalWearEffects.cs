using System;
using Server.Items;
using Server.Systems.MahaonProfessions;

namespace Server.Systems.MahaonMetals;

/// <summary>Эффекты ношения — ограничения по классу (через уже существующую систему
/// профессий) и постоянные бонусы, пока предмет надет. Резист/статы через стандартные
/// движковые моды (ResistanceMod/StatMod, чистятся вручную по имени на основе Serial),
/// сопротивление магии и скрытность Тени — через EquippedSkillMod, который сам себя
/// деактивирует при снятии предмета, чистить руками не нужно.</summary>
public static class MahaonMetalWearEffects
{
    public static bool CanWear(Item item, Mobile from, out string reason)
    {
        var metal = MahaonMetalTracker.GetMetal(item);

        if (metal == null)
        {
            reason = null;
            return true;
        }

        if (metal == MahaonMetal.Shadow &&
            !(ProfessionSystem.TouchesCategory(from, ProfessionCategory.Magic) ||
              ProfessionSystem.TouchesCategory(from, ProfessionCategory.Thief)))
        {
            reason = "Эту вещь могут надеть только маги и воры.";
            return false;
        }

        if (metal == MahaonMetal.Verite && !ProfessionSystem.TouchesCategory(from, ProfessionCategory.Warrior))
        {
            reason = "Эту вещь могут надеть только воины.";
            return false;
        }

        reason = null;
        return true;
    }

    public static void OnWorn(Item item, Mobile from)
    {
        var metal = MahaonMetalTracker.GetMetal(item);

        if (metal == null)
        {
            return;
        }

        var tag = item.Serial.Value.ToString();

        // Generic tier bonus — every metal gets this on top of its own special effect
        // below, if it has one. Higher tier = more physical defense, same idea as the
        // damage-side TierDamageBonus in MahaonMetalCombatEffects.
        var tierDefense = MahaonMetalCombatEffects.TierDefenseBonus(MahaonMetalTable.Get(metal.Value).Tier);

        if (tierDefense > 0)
        {
            from.AddResistanceMod(new ResistanceMod(ResistanceType.Physical, $"{tag}MahaonMetalTier", tierDefense));
        }

        switch (metal)
        {
            case MahaonMetal.Ice:
                from.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, $"{tag}MahaonMetal", 1));
                break;
            case MahaonMetal.Crystal:
                from.AddResistanceMod(new ResistanceMod(ResistanceType.Poison, $"{tag}MahaonMetal", 1));
                break;
            case MahaonMetal.Winter:
                from.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, $"{tag}MahaonMetal", 3));
                break;
            case MahaonMetal.Onyx:
                from.AddSkillMod(new EquippedSkillMod(SkillName.MagicResist, $"{tag}MahaonMetal", true, 5, item, from));
                break;
            case MahaonMetal.Shadow:
                from.AddSkillMod(new EquippedSkillMod(SkillName.Hiding, $"{tag}MahaonMetal", true, 0.5, item, from));
                break;
            case MahaonMetal.Valorite:
                // "+0.5% к магическому умению" — трактую как бонус к EvalInt (влияет на
                // силу заклинаний), не трогая AosAttributes предмета вообще.
                from.AddSkillMod(new EquippedSkillMod(SkillName.EvalInt, $"{tag}MahaonMetal", true, 0.5, item, from));
                break;
            case MahaonMetal.Diamond:
                var (statType, amount) = DiamondStatFor(item.Layer);
                from.AddStatMod(new StatMod(statType, $"{tag}MahaonMetal", amount, TimeSpan.Zero));
                break;
        }
    }

    public static void OnUnworn(Item item, Mobile from)
    {
        var metal = MahaonMetalTracker.GetMetal(item);

        if (metal == null)
        {
            return;
        }

        var tag = item.Serial.Value.ToString();

        from.RemoveResistanceMod($"{tag}MahaonMetalTier");

        switch (metal)
        {
            case MahaonMetal.Ice:
            case MahaonMetal.Crystal:
            case MahaonMetal.Winter:
                from.RemoveResistanceMod($"{tag}MahaonMetal");
                break;
            case MahaonMetal.Diamond:
                from.RemoveStatMod($"{tag}MahaonMetal");
                break;
                // Onyx/Shadow: EquippedSkillMod сам проверяет, надет ли ещё предмет —
                // ручное снятие не требуется.
        }
    }

    // Пользователь явно указал только 3 слота (голова-интеллект, ноги-ловкость,
    // руки-сила) — остальные додумал сам по аналогии: тело/грудь и плащ тоже к силе
    // (общая "телесная" броня), перчатки к ловкости (кисти), шея к интеллекту
    // (украшения обычно про магию). Это моя достройка, не то, что было явно сказано.
    private static (StatType, int) DiamondStatFor(Layer layer) => layer switch
    {
        Layer.Helm or Layer.Neck                                       => (StatType.Int, 5),
        Layer.Pants or Layer.InnerLegs or Layer.OuterLegs or Layer.Shoes => (StatType.Dex, 5),
        Layer.Gloves                                                    => (StatType.Dex, 5),
        _                                                                 => (StatType.Str, 5) // грудь/руки/плащ/по умолчанию
    };
}
