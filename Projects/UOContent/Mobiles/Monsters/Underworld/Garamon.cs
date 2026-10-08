using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>The sage standing at the mouth of the Underworld. Ported from ServUO
/// (Scripts/Mobiles/NPCs/Garamon.cs). He is not a quest giver: he answers keywords, and the
/// answers are the walkthrough for the Sacred Quest — the vines, the hidden teleporters, the
/// two key fragments in the secret rooms, and the third that Tyball's Shadow carries.
///
/// The original is a bare Mobile rather than a BaseCreature, blessed and unable to walk; kept
/// that way. His lines are plain text in the original rather than clilocs, so they are
/// translated here like the rest of this shard's free text; the English keywords are kept
/// alongside the Russian ones so either works.
///
/// One real crash fixed: the original casts the speaker to PlayerMobile and then dereferences
/// it without a null check, so any non-player speech in range throws.</summary>
[SerializationGenerator(0, false)]
public partial class Garamon : Mobile
{
    [Constructible]
    public Garamon()
    {
        Str = 100;
        Int = 100;
        Dex = 100;

        Name = "Гарамон";
        Body = 0x190;
        Hue = 33821;

        HairItemID = 0x2044;
        HairHue = 0x44E;
        FacialHairItemID = 0x204B;

        CantWalk = true;
        Direction = Direction.South;

        AddItem(new Shoes(1810));
        AddItem(new Robe(946));

        Blessed = true;
    }

    public virtual bool IsInvulnerable => true;

    public override bool HandlesOnSpeech(Mobile from) => from.InRange(Location, 8) || base.HandlesOnSpeech(from);

    public override void OnSpeech(SpeechEventArgs e)
    {
        if (e.Handled || !e.Mobile.InRange(Location, 2))
        {
            return;
        }

        if (e.Mobile is PlayerMobile { AbyssEntry: true })
        {
            SayTo(e.Mobile, "Ты уже прошёл Священный Поиск.");
            base.OnSpeech(e);
            return;
        }

        switch (e.Speech.ToLowerInvariant())
        {
            case "hello":
            case "привет":
                {
                    Say("Приветствую, искатель! Если ты ищешь путь в Бездну, я могу помочь.");
                    break;
                }
            case "secret":
            case "тайна":
                {
                    Say("Тот, кто внимателен к стенам, заметит нечто необычное.");
                    break;
                }
            case "teleporter":
            case "телепорт":
                {
                    Say("Их здесь немало. Они облегчат тебе путь.");
                    break;
                }
            case "vines":
            case "лозы":
                {
                    Say("Ах, лозы! Коварная вещь. Поищи то, чем их можно выжечь.");
                    break;
                }
            case "burn":
            case "выжечь":
                {
                    Say("Скажу сразу: огонь тут ни при чём. Нужное найдётся в самом подземелье.");
                    break;
                }
            case "abyss":
            case "бездна":
                {
                    Say("Вход стерегут каменные стражи. Они пропустят лишь того, у кого есть Тройной ключ!");
                    break;
                }
            case "stone guardian":
            case "стражи":
                {
                    Say("Они не пустят тебя в Бездну, пока ты не предъявишь Тройной ключ.");
                    break;
                }
            case "key":
            case "ключ":
                {
                    Say("Он из трёх частей — их нужно найти и соединить в одно!");
                    break;
                }
            case "parts":
            case "части":
                {
                    Say("Две спрятаны в тайных комнатах Подземья. Третью придётся отнять у тени зла.");
                    break;
                }
            case "shadow of evil":
            case "тень зла":
                {
                    Say("Гнуснейший из предателей. Добудь первые две части и вызови его на бой за третью! Он обитает за пустотой, в Святилище.");
                    break;
                }
            case "shrine":
            case "святилище":
                {
                    Say("Ищи дорогу через подземелье. Добраться туда можно только телепортом.");
                    break;
                }
        }

        base.OnSpeech(e);
    }
}
