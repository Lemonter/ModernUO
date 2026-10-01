using ModernUO.Serialization;
using Server.Engines.Craft;
using Server.Systems.MahaonMetals;
using Server.Targeting;

namespace Server.Items;

/// <summary>Одна руда на все 24 металла — металл хранится полем, не отдельным классом на
/// тип (иначе пришлось бы городить 24 класса). Тот же приём, что уже использовался в этой
/// сессии для сезонных деревьев (один класс + enum вида).
///
/// Плавка — своя реализация по образцу ванильного BaseOre.OnDoubleClick (тот же способ
/// распознавания печи: атрибут ForgeAttribute либо диапазон графики), не расширяет
/// BaseOre напрямую (у него закрытый CraftResource-специфичный конструктор), поэтому
/// логика продублирована, а не унаследована.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonOre : Item
{
    [SerializableField(0)]
    private MahaonMetal _metal;

    public const int OreGraphic = 0x19B9; // тот же общий графон, что был у старой системы — только металл красит

    [Constructible]
    public MahaonOre(MahaonMetal metal = MahaonMetal.Iron, int amount = 1) : base(OreGraphic)
    {
        _metal = metal;
        Stackable = true;
        Amount = amount;
        Weight = 0.5;

        var info = MahaonMetalTable.Get(metal);
        Name = $"руда ({info.RuName})";
        Hue = info.Hue;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        var info = MahaonMetalTable.Get(_metal);
        list.Add(info.EffectDescription);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!Movable)
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(501976); // The ore is too far away.
            return;
        }

        from.SendMessage("Выбери печь, чтобы расплавить эту руду.");
        from.Target = new SmeltTarget(this);
    }

    private static bool IsForge(object obj)
    {
        if (obj.GetType().IsDefined(typeof(ForgeAttribute), false))
        {
            return true;
        }

        var itemId = obj switch
        {
            Item item           => item.ItemID,
            StaticTarget target => target.ItemID,
            _                    => 0
        };

        return itemId is 4017 or >= 6522 and <= 6569 or 11736;
    }

    private const int MaxIngotStack = 60000;

    // The large forge: its addon pieces (LargeForgeWest/East and their parts) or the large forge
    // graphics drawn in the map statics. The small forge and the anvil-forge are the rest of
    // IsForge.
    private static bool IsLargeForge(object obj)
    {
        if (obj is Item item)
        {
            var type = item.GetType();
            if ((type.DeclaringType ?? type).Name.StartsWith("LargeForge", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        var itemId = obj switch
        {
            Item i              => i.ItemID,
            StaticTarget target => target.ItemID,
            _                   => 0
        };

        return itemId is >= 6522 and <= 6569;
    }

    private class SmeltTarget : Target
    {
        private readonly MahaonOre _ore;

        public SmeltTarget(MahaonOre ore) : base(2, false, TargetFlags.None)
        {
            _ore = ore;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_ore.Deleted || !from.InRange(_ore.GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(501976);
                return;
            }

            if (!IsForge(targeted))
            {
                from.SendMessage("Это не похоже на печь.");
                return;
            }

            var info = MahaonMetalTable.Get(_ore.Metal);

            if (from.Skills[SkillName.Mining].Value < info.MiningSkillRequired)
            {
                from.SendLocalizedMessage(501986); // You have no idea how to smelt this strange ore!
                return;
            }

            var minSkill = System.Math.Max(0, info.MiningSkillRequired - 25.0);
            var maxSkill = info.MiningSkillRequired + 25.0;

            // Smelting always succeeds at a fixed yield; the check only trains the skill.
            from.CheckTargetSkill(SkillName.Mining, targeted, minSkill, maxSkill);

            var ingotsPerOre = IsLargeForge(targeted) ? 3 : 2;
            var toConsume = System.Math.Min(_ore.Amount, MaxIngotStack / ingotsPerOre);
            var ingotAmount = toConsume * ingotsPerOre;

            _ore.Consume(toConsume);

            var ingot = new MahaonIngot(_ore.Metal, ingotAmount);

            if (from.Backpack?.TryDropItem(from, ingot, false) != true)
            {
                ingot.MoveToWorld(from.Location, from.Map);
            }

            from.PlaySound(0x2A);
            from.SendMessage(0x59, $"Ты выплавляешь {ingot.Amount} слитков ({info.RuName}).");
        }
    }
}
