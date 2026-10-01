using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonMetals;

/// <summary>Боевые эффекты оружия по металлу — заменяет старый MetalDamageSystem
/// (серебро/золото x3 через угадывание по имени оружия) на честную систему через
/// MahaonMetalTracker. Только эффекты с чёткой одиночной механикой, применимой к
/// урону при ударе — статовые/защитные эффекты брони и ограничения по классу
/// реализуются отдельно.</summary>
public static class MahaonMetalCombatEffects
{
    /// <summary>Процентный бонус к урону — тот же percentageBonus-паттерн, что уже
    /// используется WeaponStyleSystem/MetalDamageSystem. Складывается из двух частей:
    /// общий бонус по тиру металла (чем выше тир — тем больше урона, независимо от
    /// того, какой именно это металл) + особый бонус у металлов с собственной боевой
    /// механикой (Silver/Gold/Ice/Crystal/Sky/Mahaon), поверх тирового.</summary>
    public static int GetDamageBonus(BaseWeapon weapon, Mobile attacker, Mobile defender)
    {
        var metal = MahaonMetalTracker.GetMetal(weapon);

        if (metal == null)
        {
            return 0;
        }

        var bonus = TierDamageBonus(MahaonMetalTable.Get(metal.Value).Tier);

        if (defender is not BaseCreature creature)
        {
            return bonus;
        }

        bonus += metal switch
        {
            MahaonMetal.Silver when creature.IsUndead     => 100, // x2 итоговый урон
            MahaonMetal.Gold when creature.IsDragonKind    => 50,  // x1.5 итоговый урон
            MahaonMetal.Ice                                 => 15,  // доп. урон холодом — упрощено до плоского бонуса
            MahaonMetal.Crystal                             => 15,  // доп. урон ядом — тоже плоский бонус
            MahaonMetal.Sky                                 => 20,  // урон молнией
            MahaonMetal.Mahaon                              => 30,  // сопоставимо с Flame Strike
            _                                                => 0
        };

        return bonus;
    }

    /// <summary>Процентное снижение входящего урона за счёт брони защитника —
    /// зеркальная, ранее отсутствовавшая половина Silver/Gold (оружейная половина —
    /// бонус урона по нежити/драконам — уже была в GetDamageBonus, доспешная "доспехи
    /// лучше защищают от нежити/драконов" из MahaonMetalTable — нет). Возвращает
    /// отрицательное число (вычитается из общего percentageBonus), суммирует бонус по
    /// каждой надетой вещи нужного металла, не по одной случайной. Sky's "защита от
    /// заклинаний, снижающих статы" сюда не входит — под неё нет готовой инфраструктуры
    /// (нужно перехватывать конкретные дебафф-заклинания), оставлено как известный
    /// пробел, не заглушка.</summary>
    public static int GetArmorDefenseBonus(Mobile defender, Mobile attacker)
    {
        if (attacker is not BaseCreature creature)
        {
            return 0;
        }

        var reduction = 0;

        foreach (var item in defender.Items)
        {
            if (item is not BaseArmor)
            {
                continue;
            }

            var metal = MahaonMetalTracker.GetMetal(item);

            if (metal == MahaonMetal.Silver && creature.IsUndead)
            {
                reduction -= 15;
            }
            else if (metal == MahaonMetal.Gold && creature.IsDragonKind)
            {
                reduction -= 15;
            }
        }

        return reduction;
    }

    /// <summary>Чем выше тир металла, тем больше урон/защита — независимо от того, какой
    /// именно это металл (шард-овнер: "чем больше тир, тем больше урона и защиты").
    /// Линейно, 10% за тир — тот же принцип, что и у шанса дропа чертежей по уровню
    /// сундука. Единственная ручка для тюнинга, если баланс не подойдёт.</summary>
    public static int TierDamageBonus(MahaonMetalTier tier) => (int)tier * 10;

    /// <summary>Та же прогрессия, что и у урона, но во флэт-очках физ. резиста —
    /// применяется в MahaonMetalWearEffects.OnWorn для любого металла, поверх его
    /// собственных особых эффектов ношения.</summary>
    public static int TierDefenseBonus(MahaonMetalTier tier) => (int)tier * 3;

    private const double WinterParalyzeChance = 0.10;
    private const double OnyxManaDrainChance = 0.15;
    private const int OnyxManaDrainAmount = 15;

    /// <summary>Разовые эффекты помимо чистого урона — паралич (Winter), слив маны
    /// магам (Onyx). Вызывается сразу после успешного удара, тем же порядком, что и
    /// сигнатурные механики WeaponStyleSystem.</summary>
    public static void OnAfterHit(BaseWeapon weapon, Mobile attacker, Mobile defender)
    {
        var metal = MahaonMetalTracker.GetMetal(weapon);

        if (metal == null || !defender.Alive)
        {
            return;
        }

        if (metal == MahaonMetal.Winter && Utility.RandomDouble() < WinterParalyzeChance)
        {
            defender.Paralyze(TimeSpan.FromSeconds(2));
            defender.FixedParticles(0x376A, 9, 32, 5032, EffectLayer.Waist, 0);
            defender.PlaySound(0x204);
        }

        if (metal == MahaonMetal.Onyx && defender.Skills[SkillName.Magery].Value > 0 &&
            Utility.RandomDouble() < OnyxManaDrainChance)
        {
            var drained = Math.Min(defender.Mana, OnyxManaDrainAmount);
            defender.Mana -= drained;
            defender.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Head, 0);
            attacker.SendMessage(0x59, $"Оникс высасывает {drained} маны из противника.");
        }
    }
}
