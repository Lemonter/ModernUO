using System.Collections.Generic;

namespace Server.Systems.MahaonMetals;

/// <summary>Полное описание металла — требование к навыку добычи, тир, текстовое
/// описание эффекта (для гампа/тултипа), цвет (хью руды/слитка/перекованной вещи).
/// Числовая механика эффектов реализована отдельно в MahaonMetalCombatEffects.cs — тут
/// только данные.</summary>
public readonly record struct MahaonMetalInfo(
    string RuName, int MiningSkillRequired, MahaonMetalTier Tier, string EffectDescription,
    bool UsableForMagicItems, int Hue
);

public static class MahaonMetalTable
{
    // Hues are a first pass, chosen by name/theme, not eyeballed in-game yet — retune any
    // that clash once you can actually look at them side by side. Real OSI colored-metal
    // hues reused where the name matches a real one (Cooper/Bronze/Gold/Agapite/Verite/
    // Valorite/Shadow) so those at least look "correct" to anyone who's played vanilla UO.
    public static readonly Dictionary<MahaonMetal, MahaonMetalInfo> Data = new()
    {
        [MahaonMetal.Iron] = new("Железо", 0, MahaonMetalTier.Обычный,
            "Может использоваться для изготовления магических вещей.", true, 0), // untinted
        [MahaonMetal.Cobalt] = new("Кобальт", 20, MahaonMetalTier.Обычный,
            "Может использоваться для изготовления магических вещей.", true, 0x4F3), // steel blue
        [MahaonMetal.Shadow] = new("Тень", 25, MahaonMetalTier.Обычный,
            "Могут одевать только маги и воры. Каждая вещь даёт +0.5 к мастерству прятаться.", false, 0x966), // real ShadowIron
        [MahaonMetal.Cooper] = new("Медь", 30, MahaonMetalTier.Обычный,
            "Может использоваться для изготовления магических вещей.", true, 0x96D), // real Copper
        [MahaonMetal.Bronze] = new("Бронза", 35, MahaonMetalTier.Обычный,
            "Может использоваться для изготовления магических вещей.", true, 0x972), // real Bronze
        [MahaonMetal.Silver] = new("Серебро", 40, MahaonMetalTier.Обычный,
            "Оружие наносит двойной урон нежити, доспехи лучше защищают от нежити.", false, 0x481), // pale silver
        [MahaonMetal.Gold] = new("Золото", 45, MahaonMetalTier.Обычный,
            "Оружие наносит полуторный урон драконам, доспехи лучше защищают от драконов.", false, 0x8A5), // real Gold
        [MahaonMetal.Melchior] = new("Мельхиор", 50, MahaonMetalTier.Обычный,
            "Может использоваться для изготовления магических вещей.", true, 0x453), // nickel-silver gray

        [MahaonMetal.Ice] = new("Лёд", 55, MahaonMetalTier.Уникальный,
            "Оружие даёт дополнительный урон холодом. Каждая вещь на +1 защищает от огня.", false, 0x480), // icy pale blue
        [MahaonMetal.Crystal] = new("Кристалл", 60, MahaonMetalTier.Уникальный,
            "Доспехи дают +1 защиты от яда, оружие наносит дополнительное повреждение ядом.", false, 0x47F), // clear pale cyan
        [MahaonMetal.Agapite] = new("Агапит", 65, MahaonMetalTier.Уникальный,
            "Может использоваться для изготовления магических вещей.", true, 0x979), // real Agapite (pink)
        [MahaonMetal.Winter] = new("Зима", 70, MahaonMetalTier.Уникальный,
            "Оружие может нанести паралич, доспехи дают защиту от огня +3 за каждую вещь.", false, 0x8FD), // bright white-blue
        [MahaonMetal.Verite] = new("Верит", 75, MahaonMetalTier.Уникальный,
            "Может использоваться для изготовления магических вещей. Одеть могут только воины.", true, 0x89F), // real Verite (green)
        [MahaonMetal.Onyx] = new("Оникс", 80, MahaonMetalTier.Уникальный,
            "Против магов — оружие наносит повреждение, схожее с манадрейном. +5 сопротивление магии за каждую одетую вещь.", false, 0x455), // near-black
        [MahaonMetal.Sky] = new("Небо", 85, MahaonMetalTier.Уникальный,
            "Оружие наносит повреждение молнией, доспехи защищают против заклинаний, снижающих статы.", false, 0x2), // sky blue

        [MahaonMetal.Doom] = new("Doom", 87, MahaonMetalTier.Раритетный,
            "Каждая одетая вещь увеличивает регенерацию.", false, 0x496), // deep blood red
        [MahaonMetal.Diamond] = new("Алмаз", 90, MahaonMetalTier.Раритетный,
            "Каждая одетая вещь повышает один из статов +5 в зависимости от места ношения (голова — интеллект, ноги — ловкость, руки — сила и т.д.).", false, 0x47E), // bright clear white
        [MahaonMetal.Valorite] = new("Валорит", 93, MahaonMetalTier.Раритетный,
            "Каждая вещь даёт +0.5% к магическому умению.", false, 0x8AB), // real Valorite (yellow-green)
        [MahaonMetal.Titanium] = new("Титан", 95, MahaonMetalTier.Раритетный,
            "Может использоваться для изготовления магических вещей.", true, 0x3B2), // dark steel gray

        [MahaonMetal.Mytheril] = new("Мифрил", 97, MahaonMetalTier.Мифический,
            "Может использоваться для изготовления магических вещей.", true, 0x48F), // pale blue-silver
        [MahaonMetal.Mahaon] = new("Махаон", 100, MahaonMetalTier.Мифический,
            "Оружие наносит дополнительное повреждение, схожее с уроном от заклинания Flame Strike. Броня даёт полную защиту от заклинаний до 4 круга включительно.", false, 0x497), // rich purple-red, signature metal

        // Эти три эффекта не были описаны при передаче списка — честно оставляю
        // заглушку вместо выдуманного эффекта. Требования к навыку и тир настоящие.
        [MahaonMetal.Kotium] = new("Котиум", 102, MahaonMetalTier.Мифический,
            "Эффект пока не определён.", false, 0x25), // deep red
        [MahaonMetal.Pawerium] = new("Пауэриум", 105, MahaonMetalTier.Мифический,
            // 0x486 is a verified real purple (same hue as OSI VioletCouragePurple, see
            // MahaonSkillTree.MasteryGainHue) — replaces the previous 0x0095, which was
            // mislabeled "deep purple" here but actually falls in the orange/tan band.
            "Эффект пока не определён.", false, 0x486),
        [MahaonMetal.Lemium] = new("Лемиум", 107, MahaonMetalTier.Мифический,
            "Эффект пока не определён.", false, 0x8A8) // bright gold-white
    };

    public static MahaonMetalInfo Get(MahaonMetal metal) => Data[metal];

    /// <summary>Все металлы, добыча которых доступна при данном значении навыка Mining —
    /// используется для взвешенной таблицы при копании (см. MahaonOreGenerator).</summary>
    public static List<MahaonMetal> AvailableAt(double miningSkill)
    {
        var result = new List<MahaonMetal>();

        foreach (var (metal, info) in Data)
        {
            if (miningSkill >= info.MiningSkillRequired)
            {
                result.Add(metal);
            }
        }

        return result;
    }
}
