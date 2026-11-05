using Server.Items;
using Server.Systems.MahaonCombat;
using Xunit;

namespace Server.Tests.Systems.MahaonCombat;

/// <summary>
///     Профиль удара — общий подсчёт, через который теперь идёт урон оружия и пойдут попадание
///     и крит.
///
///     Сам удар (BaseWeapon.OnHit) здесь не гоняется: ему нужны карта, сеть и два живых бойца.
///     Его перестройка механическая — двадцать семь строк «percentageBonus += x» превращены в
///     «strike.Add(подпись, Dmg, x)» в том же порядке, — а то, на чём она держится, проверяется
///     здесь: что сумма корзины равна сумме слагаемых, что потолки режут, и что пустой реестр
///     не меняет ничего.
/// </summary>
[Collection("Sequential UOContent Tests")]
public class StrikeProfileTests
{
    private sealed class Fixed : IStrikeContributor
    {
        private readonly StrikeField _field;
        private readonly double _value;

        public Fixed(StrikeField field, double value)
        {
            _field = field;
            _value = value;
        }

        public void Contribute(in StrikeContext context, StrikeProfile profile) =>
            profile.Add("тест", _field, _value);
    }

    [Fact]
    public void Sum_AddsUpOneFieldOnly()
    {
        var p = new StrikeProfile();

        p.Add("стойка", StrikeField.Damage, 20);
        p.Add("перелом", StrikeField.Damage, -30);
        p.Add("мастерство", StrikeField.HitChance, 6);

        Assert.Equal(-10, p.Sum(StrikeField.Damage));
        Assert.Equal(6, p.Sum(StrikeField.HitChance));
        Assert.Equal(0, p.Sum(StrikeField.CritChance));
    }

    /// <summary>Нулевой вклад в чеке не нужен — и в строках его нет.</summary>
    [Fact]
    public void ZeroContribution_IsNotRecorded()
    {
        var p = new StrikeProfile();

        p.Add("ничего", StrikeField.Damage, 0);

        Assert.Empty(p.Lines);
    }

    /// <summary>
    ///     Целые слагаемые обязаны сложиться ровно в то же целое, что раньше копилось в
    ///     percentageBonus. Именно на этом держится «урон не изменился ни на процент».
    /// </summary>
    [Fact]
    public void IntegerTerms_SumExactlyAsBefore()
    {
        int[] terms = { 25, -30, 100, 7, -10, 13, 50, 1, -1, 300 };
        var p = new StrikeProfile();
        var old = 0;

        foreach (var t in terms)
        {
            p.Add("слагаемое", StrikeField.Damage, t);
            old += t;
        }

        Assert.Equal(old, (int)p.Sum(StrikeField.Damage));
    }

    [Fact]
    public void Caps_ClampHitAndCrit()
    {
        var p = new StrikeProfile();

        p.Add("много", StrikeField.HitChance, 80);
        p.Add("много", StrikeField.CritChance, 80);

        Assert.Equal(StrikeProfile.MaxHitChance, p.HitChance);
        Assert.Equal(StrikeProfile.MaxCritChance, p.CritChance);

        var q = new StrikeProfile();

        q.Add("штраф", StrikeField.HitChance, -80);
        q.Add("штраф", StrikeField.CritChance, -5);

        Assert.Equal(-StrikeProfile.MaxHitChance, q.HitChance);
        Assert.Equal(0, q.CritChance);
    }

    /// <summary>Пока никто не зарегистрирован, рычаг попадания стоит в нуле.</summary>
    [Fact]
    public void EmptyRegistry_ChangesNothing()
    {
        var weapon = new Katana();

        try
        {
            Assert.Equal(0, StrikeProfiles.HitChanceBonus(null, null, weapon));
            Assert.Empty(StrikeProfiles.Gather(null, null, weapon).Lines);
        }
        finally
        {
            weapon.Delete();
        }
    }

    [Fact]
    public void RegisteredContributor_ReachesTheSwing()
    {
        var weapon = new Katana();
        var attacker = new Mobile();
        var hit = new Fixed(StrikeField.HitChance, 6);

        StrikeProfiles.Register(hit);

        try
        {
            Assert.Equal(6, StrikeProfiles.HitChanceBonus(attacker, null, weapon));
            Assert.Single(StrikeProfiles.Gather(attacker, null, weapon).Lines);
        }
        finally
        {
            StrikeProfiles.Unregister(hit);
            weapon.Delete();
            attacker.Delete();
        }

        Assert.Equal(0, StrikeProfiles.HitChanceBonus(attacker, null, weapon));
    }

    [Fact]
    public void LastStrike_IsRememberedPerAttacker()
    {
        var a = new Mobile();
        var b = new Mobile();
        var p = new StrikeProfile();

        p.Add("стойка", StrikeField.Damage, 20);

        try
        {
            StrikeProfiles.Remember(a, p);

            Assert.Same(p, StrikeProfiles.LastOf(a));
            Assert.Null(StrikeProfiles.LastOf(b));
        }
        finally
        {
            a.Delete();
            b.Delete();
        }
    }
}
