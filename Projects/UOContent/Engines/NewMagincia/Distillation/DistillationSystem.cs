/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/DistillationSystem.cs).
///
///     Реестр рецептов дистилляции и контекстов игроков. В ServUO контексты сохранялись через
///     <c>Persistence.Serialize("Saves/CraftContext/DistillationContexts.bin")</c>; здесь это делает
///     <see cref="DistillationPersistence" /> по образцу
///     <c>Engines/CleanUpBritannia/CleanUpBritanniaPersistence.cs</c>.
///
///     Замены:
///     <list type="bullet">
///         <item><c>Timer.DelayCall(TimeSpan, TimerStateCallback, object)</c> → <c>Timer.StartTimer(TimeSpan, Action)</c>.</item>
///         <item>Контекст удаляется по событию удаления игрока, иначе словарь рос бы бесконечно.</item>
///     </list>
/// </summary>
using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Distillation;

public enum Group
{
    WheatBased,
    WaterBased,
    Other
}

public enum Liquor
{
    None,
    Whiskey,
    Bourbon,
    Spirytus,
    Cassis,
    MelonLiquor,
    Mist,
    Akvavit,
    Arak,
    CornWhiskey,
    Brandy
}

public static class DistillationSystem
{
    public static readonly TimeSpan MaturationPeriod = TimeSpan.FromHours(48);

    private static readonly Dictionary<Mobile, DistillationContext> _contexts = new();

    private static DistillationPersistence _persistence;

    public static List<CraftDefinition> CraftDefs { get; } = new();

    public static Dictionary<Mobile, DistillationContext> Contexts => _contexts;

    public static void Configure() => _persistence = new DistillationPersistence();

    public static void Initialize()
    {
        CraftDefs.Clear();

        // Wheat Based
        CraftDefs.Add(
            new CraftDefinition(
                Group.WheatBased, Liquor.Whiskey, new[] { typeof(Yeast), typeof(WheatWort) }, new[] { 3, 1 },
                MaturationPeriod
            )
        );
        CraftDefs.Add(
            new CraftDefinition(
                Group.WheatBased, Liquor.Bourbon,
                new[] { typeof(Yeast), typeof(WheatWort), typeof(PewterBowlOfCorn) }, new[] { 3, 3, 1 },
                MaturationPeriod
            )
        );
        CraftDefs.Add(
            new CraftDefinition(
                Group.WheatBased, Liquor.Spirytus,
                new[] { typeof(Yeast), typeof(WheatWort), typeof(PewterBowlOfPotatos) }, new[] { 3, 1, 1 },
                TimeSpan.MinValue
            )
        );
        CraftDefs.Add(
            new CraftDefinition(
                Group.WheatBased, Liquor.Cassis,
                new[] { typeof(Yeast), typeof(WheatWort), typeof(PewterBowlOfPotatos), typeof(TribalBerry) },
                new[] { 3, 3, 3, 1 }, MaturationPeriod
            )
        );
        CraftDefs.Add(
            new CraftDefinition(
                Group.WheatBased, Liquor.MelonLiquor,
                new[] { typeof(Yeast), typeof(WheatWort), typeof(PewterBowlOfPotatos), typeof(HoneydewMelon) },
                new[] { 3, 3, 3, 1 }, MaturationPeriod
            )
        );
        CraftDefs.Add(
            new CraftDefinition(
                Group.WheatBased, Liquor.Mist, new[] { typeof(Yeast), typeof(WheatWort), typeof(JarHoney) },
                new[] { 3, 3, 1 }, MaturationPeriod
            )
        );

        // Water Based
        CraftDefs.Add(
            new CraftDefinition(
                Group.WaterBased, Liquor.Akvavit,
                new[] { typeof(Yeast), typeof(Pitcher), typeof(PewterBowlOfPotatos) }, new[] { 1, 3, 1 },
                TimeSpan.MinValue
            )
        );
        CraftDefs.Add(
            new CraftDefinition(
                Group.WaterBased, Liquor.Arak, new[] { typeof(Yeast), typeof(Pitcher), typeof(Dates) },
                new[] { 1, 3, 1 }, MaturationPeriod
            )
        );
        CraftDefs.Add(
            new CraftDefinition(
                Group.WaterBased, Liquor.CornWhiskey,
                new[] { typeof(Yeast), typeof(Pitcher), typeof(PewterBowlOfCorn) }, new[] { 1, 3, 1 },
                MaturationPeriod
            )
        );

        // Other
        CraftDefs.Add(
            new CraftDefinition(Group.Other, Liquor.Brandy, new[] { typeof(Pitcher) }, new[] { 4 }, MaturationPeriod)
        );
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m) => _contexts.Remove(m);

    public static void AddContext(Mobile from, DistillationContext context)
    {
        if (from != null)
        {
            _contexts[from] = context;
        }
    }

    public static DistillationContext GetContext(Mobile from)
    {
        if (from == null)
        {
            return null;
        }

        if (!_contexts.TryGetValue(from, out var context))
        {
            context = new DistillationContext();
            _contexts[from] = context;
        }

        return context;
    }

    public static int GetLabel(Liquor liquor, bool strong) => (strong ? 1150718 : 1150442) + (int)liquor;

    public static int GetLabel(Group group) => group == Group.Other ? 1077435 : 1150736 + (int)group;

    public static Liquor GetFirstLiquor(Group group) => GetFirstDefForGroup(group)?.Liquor ?? Liquor.Whiskey;

    public static CraftDefinition GetFirstDefForGroup(Group group)
    {
        foreach (var def in CraftDefs)
        {
            if (def.Group == group)
            {
                return def;
            }
        }

        return null;
    }

    public static CraftDefinition GetDefinition(Liquor liquor, Group group)
    {
        foreach (var def in CraftDefs)
        {
            if (def.Liquor == liquor && def.Group == group)
            {
                return def;
            }
        }

        return GetFirstDefForGroup(group);
    }

    public static void SendDelayedGump(Mobile from) =>
        Timer.StartTimer(TimeSpan.FromSeconds(1.5), () => DistillationGump.DisplayTo(from));

    public static void SerializeState(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // версия

        writer.WriteEncodedInt(_contexts.Count);

        foreach (var (mobile, context) in _contexts)
        {
            writer.Write(mobile);
            context.Serialize(writer);
        }
    }

    public static void DeserializeState(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // версия

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var context = DistillationContext.Deserialize(reader);

            if (mobile != null)
            {
                _contexts[mobile] = context;
            }
        }
    }
}
