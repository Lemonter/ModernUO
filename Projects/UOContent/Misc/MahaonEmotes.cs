namespace Server.Misc;

public enum Emote
{
    Laugh,
    Giggle,
    Sigh,
    Cheer,
    Applaud,
    Yawn,
    Huh,
    Gasp,
    Groan,
    Whistle,
    Cough,
    Bow,
    Salute,
    Yea,
    No,
    Hey,
    Oh
}

/// <summary>
/// Эмоции людей: звук из штатного набора клиента (свой для мужчин и женщин), где есть — движение
/// тела, и строка «*смеётся*» над головой, как у игрока, набравшего эмоцию. Звуки сверены по
/// soundLegacyMUL: m_laugh — 0x431, f_laugh — 0x321 и так далее; номер звука равен номеру записи.
/// </summary>
public static class MahaonEmotes
{
    // Действия тела людей: 5 — переминается, 6 — потягивается, 17 — руки вверх, 32 — поклон,
    // 33 — отдаёт честь.
    private const int NoAnim = -1;

    private static readonly (int female, int male, int anim, string text)[] _table =
    [
        (0x321, 0x431, NoAnim, "*смеётся*"),
        (0x31A, 0x42A, NoAnim, "*хихикает*"),
        (0x330, 0x442, 5, "*вздыхает*"),
        (0x30F, 0x41E, 17, "*радостно вскидывает руки*"),
        (0x30C, 0x41B, NoAnim, "*хлопает в ладоши*"),
        (0x336, 0x448, 6, "*зевает*"),
        (0x31F, 0x42F, 5, "*недоумевает*"),
        (0x319, 0x429, NoAnim, "*ахает*"),
        (0x31B, 0x42B, NoAnim, "*стонет*"),
        (0x335, 0x447, NoAnim, "*присвистывает*"),
        (0x311, 0x420, NoAnim, "*кашляет*"),
        (-1, -1, 32, "*кланяется*"),
        (-1, -1, 33, "*отдаёт честь*"),
        (0x337, 0x449, NoAnim, "*одобрительно кивает*"),
        (0x322, 0x432, NoAnim, "*качает головой*"),
        (0x31D, 0x42D, NoAnim, "*окликает*"),
        (0x323, 0x433, NoAnim, "*охает*")
    ];

    public static int SoundFor(Mobile m, Emote emote)
    {
        var (female, male, _, _) = _table[(int)emote];
        return m.Female ? female : male;
    }

    /// <summary>Делает эмоцию. Немые тела (не люди) только показывают строку.</summary>
    public static void Do(Mobile m, Emote emote)
    {
        if (m?.Map == null || m.Deleted || !m.Alive)
        {
            return;
        }

        var (_, _, anim, text) = _table[(int)emote];
        var human = m.Body.IsHuman;

        if (human && SoundFor(m, emote) is var sound and >= 0)
        {
            m.PlaySound(sound);
        }

        // Верхом поклон и прочие движения пешего выглядят поломкой — только звук и строка.
        if (human && anim != NoAnim && m.Mount == null)
        {
            m.Animate(anim, 7, 1, true, false, 0);
        }

        m.PublicOverheadMessage(MessageType.Emote, m.EmoteHue, false, text);
    }
}
