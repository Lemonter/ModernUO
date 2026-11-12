using System;
using ModernUO.Serialization;
using Server.Systems.MahaonCombat;
using Server.Targeting;

namespace Server.Items;

/// <summary>
///     Снадобья травника — немагическая медицина.
///
///     Место в мире у них своё: алхимия лечит и бодрит волшебными зельями, травник — тем,
///     что даёт сама земля. Поэтому они бьют по механикам, от которых зелья не спасают:
///     кровотечение и переломы из MahaonCombat раньше лечились только бинтом и временем,
///     а теперь у них есть свои средства. Противоядие слабее алхимического, зато его
///     можно собрать в поле, а отвары дают долгие мягкие усиления вместо коротких сильных.
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class MahaonRemedy : Item
{
    public MahaonRemedy(int itemId, string name, int hue, int amount) : base(itemId)
    {
        Stackable = true;
        Amount = amount;
        Name = name;
        Hue = hue;
    }

    public override double DefaultWeight => 0;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (!from.Alive)
        {
            return;
        }

        Use(from);
    }

    protected abstract void Use(Mobile from);
}

/// <summary>Снадобье, которое накладывают на себя или на соседа — отсюда прицел.</summary>
[SerializationGenerator(0, false)]
public abstract partial class MahaonTargetedRemedy : MahaonRemedy
{
    public MahaonTargetedRemedy(int itemId, string name, int hue, int amount) : base(itemId, name, hue, amount)
    {
    }

    protected override void Use(Mobile from)
    {
        from.SendMessage(0x3B2, "На кого наложить?");
        from.Target = new RemedyTarget(this);
    }

    /// <summary>Возвращает true, если снадобье потрачено.</summary>
    protected abstract bool Apply(Mobile from, Mobile patient);

    private class RemedyTarget : Target
    {
        private readonly MahaonTargetedRemedy _remedy;

        public RemedyTarget(MahaonTargetedRemedy remedy) : base(1, false, TargetFlags.Beneficial) => _remedy = remedy;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_remedy.Deleted || !_remedy.IsChildOf(from.Backpack))
            {
                return;
            }

            if (targeted is not Mobile { Alive: true } patient)
            {
                from.SendMessage(0x22, "Это можно наложить только на живого.");
                return;
            }

            if (_remedy.Apply(from, patient))
            {
                _remedy.Consume();
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonBloodstopSalve : MahaonTargetedRemedy
{
    [Constructible]
    public MahaonBloodstopSalve(int amount = 1) : base(0x09EC, "кровоостанавливающая мазь", 0x0595, amount)
    {
    }

    protected override bool Apply(Mobile from, Mobile patient)
    {
        var bleeding = BleedingSystem.IsBleeding(patient);
        BleedingSystem.CureBleed(patient);

        // Немного заживляет и без кровотечения, но мазь — не бинт: лечит мало.
        var heal = Utility.RandomMinMax(5, 10) + (int)(from.Skills[SkillName.Herbalism].Value / 10.0);
        patient.Heal(heal, from);
        patient.PlaySound(0x57);

        if (!bleeding)
        {
            from.SendMessage(0x59, "Мазь затягивает ссадины.");
        }

        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonComfreySplint : MahaonTargetedRemedy
{
    [Constructible]
    public MahaonComfreySplint(int amount = 1) : base(0x0E21, "шина с окопником", 0x0497, amount)
    {
    }

    protected override bool Apply(Mobile from, Mobile patient)
    {
        // Шину жалко тратить впустую: без перелома она остаётся в сумке.
        if (BoneFractureSystem.GetActiveFracture(patient) == null)
        {
            from.SendMessage(0x22, "Переломов нет — шина не нужна.");
            return false;
        }

        BoneFractureSystem.Mend(patient);
        patient.PlaySound(0x57);
        return true;
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonHerbalAntidote : MahaonRemedy
{
    [Constructible]
    public MahaonHerbalAntidote(int amount = 1) : base(0x0E25, "противоядие травника", 0x0A3, amount)
    {
    }

    protected override void Use(Mobile from)
    {
        if (from.Poison == null)
        {
            from.SendMessage(0x22, "Ты не отравлен.");
            return;
        }

        // Слабые яды снимает всегда, сильные — как повезёт: это отвар, а не алхимия.
        var chance = from.Poison.Level switch
        {
            <= 1 => 1.0,
            2    => 0.5,
            _    => 0.2
        };

        Consume();
        from.PlaySound(0x31);

        if (Utility.RandomDouble() < chance)
        {
            from.CurePoison(from);
            from.SendMessage(0x59, "Горечь полыни перебивает яд — тебе легче.");
        }
        else
        {
            from.SendMessage(0x22, "Отвар горек, но этот яд ему не по силам.");
        }
    }
}

/// <summary>Отвар — выпиваешь сам, действует долго и мягко.</summary>
[SerializationGenerator(0, false)]
public abstract partial class MahaonBrew : MahaonRemedy
{
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(10);

    public MahaonBrew(string name, int hue, int amount) : base(0x182A, name, hue, amount)
    {
    }

    protected override void Use(Mobile from)
    {
        Consume();
        from.PlaySound(0x31);
        Drink(from);
    }

    protected abstract void Drink(Mobile from);

    /// <summary>
    ///     Прибавка к характеристике под постоянным именем: второй глоток того же отвара
    ///     продлевает действие, а не складывает прибавки.
    /// </summary>
    protected static void Boost(Mobile m, StatType stat, string name, int offset) =>
        m.AddStatMod(new StatMod(stat, name, offset, Duration));
}

[SerializationGenerator(0, false)]
public partial class MahaonStJohnsTea : MahaonBrew
{
    [Constructible]
    public MahaonStJohnsTea(int amount = 1) : base("зверобойный чай", 0x08A5, amount)
    {
    }

    protected override void Drink(Mobile from)
    {
        Boost(from, StatType.Dex, "MahaonHerbalTea", 5);
        from.Stam = Math.Min(from.StamMax, from.Stam + 20);
        from.SendMessage(0x59, "Тепло зверобоя разливается по телу: ты двигаешься легче.");
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonMountainRootBrew : MahaonBrew
{
    [Constructible]
    public MahaonMountainRootBrew(int amount = 1) : base("отвар горного корня", 0x0455, amount)
    {
    }

    protected override void Drink(Mobile from)
    {
        Boost(from, StatType.Str, "MahaonRootBrew", 5);
        from.Stam = from.StamMax;
        from.SendMessage(0x59, "Горький корень гонит усталость прочь: сил прибавилось.");
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonWarmingBrew : MahaonBrew
{
    private const string ModName = "MahaonWarmingBrew";
    private const int ColdResist = 15;

    [Constructible]
    public MahaonWarmingBrew(int amount = 1) : base("согревающий отвар", 0x0480, amount)
    {
    }

    protected override void Drink(Mobile from)
    {
        // Сопротивление не умеет истекать само, поэтому снимаем его таймером. Прежнее
        // снимаем сразу: повторный глоток продлевает защиту, а не удваивает её.
        from.RemoveResistanceMod(ModName);
        var mod = new ResistanceMod(ResistanceType.Cold, ModName, ColdResist);
        from.AddResistanceMod(mod);
        Timer.DelayCall(Duration, () => from.RemoveResistanceMod(mod));
        from.SendMessage(0x59, "Снежноцвет согревает изнутри: холод больше не страшен.");
    }
}

[SerializationGenerator(0, false)]
public partial class MahaonMoonwortInfusion : MahaonBrew
{
    [Constructible]
    public MahaonMoonwortInfusion(int amount = 1) : base("настой лунницы", 0x0490, amount)
    {
    }

    protected override void Drink(Mobile from)
    {
        Boost(from, StatType.Int, "MahaonMoonwort", 5);
        from.Mana = Math.Min(from.ManaMax, from.Mana + 40);
        from.SendMessage(0x59, "Настой лунницы проясняет голову.");
    }
}
