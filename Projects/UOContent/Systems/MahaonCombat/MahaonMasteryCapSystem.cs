using System.Collections.Generic;

namespace Server.Systems.MahaonCombat;

/// <summary>
///     Персональный потолок мастерки — то, что поднимают свитки силы на мастерки.
///
///     Все два десятка систем мастерства (школы магии/некромантии/рыцарства/мистицизма/
///     плетения, воинские стили, воровские и ремесленные специализации) устроены
///     одинаково: константа MaxValue = 100 и зажим Math.Min(MaxValue, ...) в Train.
///     Константа общая на всех, поэтому поднять потолок одному игроку было нельзя.
///
///     Ключ — русское имя мастерки, ровно то же, что уходит в MahaonSkillTree.AnnounceGain
///     и показывается в дереве навыков. Оно уникально по всем системам, и это избавляет от
///     необходимости заводить сквозной идентификатор для двух десятков разных перечислений.
/// </summary>
public class MahaonMasteryCapSystem : GenericPersistence
{
    private static MahaonMasteryCapSystem _instance;

    /// <summary>Базовый потолок — та самая MaxValue, что была константой в каждой системе.</summary>
    public const double DefaultCap = 100.0;

    /// <summary>Выше этого свитки не поднимают, как и обычные свитки силы не дают больше 120.</summary>
    public const double AbsoluteCap = 120.0;

    /// <summary>Дорожка потолка в общем хранилище. Ключ — по-прежнему русское имя мастерки:
    /// перевести его на идентификатор дорожки можно, когда у всех веток будет общий реестр
    /// с именами (доска мастерства), а до тех пор имя уникально и этого хватает.</summary>
    private static string CapTrack(string masteryName) => "cap/" + masteryName;

    public MahaonMasteryCapSystem() : base("MahaonMasteryCaps", 1)
    {
    }

    public static void Configure() => _instance = new MahaonMasteryCapSystem();

    public static double GetCap(Mobile m, string masteryName)
    {
        if (m == null || masteryName == null)
        {
            return DefaultCap;
        }

        return MahaonProgress.TryGet(m, CapTrack(masteryName), out var cap) ? cap : DefaultCap;
    }

    /// <summary>Поднимает потолок до указанного значения. Понизить нельзя — свиток,
    /// применённый вторым, не должен отбирать то, что дал первый.</summary>
    public static bool RaiseCap(Mobile m, string masteryName, double cap)
    {
        if (m == null || masteryName == null || cap <= DefaultCap || cap > AbsoluteCap)
        {
            return false;
        }

        // Профессиональная мастерка: свои мастерки уводятся до 120, мастерки второго пути
        // до 110, чужие не поднимаются вовсе.
        if (cap > MahaonProfessions.ProfessionMasteryRules.GetCeiling(m, masteryName))
        {
            return false;
        }

        if (GetCap(m, masteryName) >= cap)
        {
            return false;
        }

        MahaonProgress.Set(m, CapTrack(masteryName), cap);
        MahaonSkillTree.NotifyChanged(m);

        return true;
    }

    /// <summary>Потолки теперь хранит MahaonProgress; здесь — пустая запись, чтобы формат файла не ломался.</summary>
    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version
        writer.WriteEncodedInt(0);
    }

    /// <summary>Читает прежние потолки и перекладывает их в общее хранилище.</summary>
    public override void Deserialize(IGenericReader reader)
    {
        reader.ReadEncodedInt(); // version

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var mobile = reader.ReadEntity<Mobile>();
            var name = reader.ReadString();
            var cap = reader.ReadDouble();

            if (mobile != null && name != null)
            {
                MahaonProgress.Migrated(mobile, CapTrack(name), cap);
            }
        }
    }
}
