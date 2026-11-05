/// <summary>
///     Ported from ServUO (Scripts/Services/City Loyalty System/Items/CityMessageBoard.cs).
///
///     Substitutions:
///     <list type="bullet">
///         <item>
///             ServUO derived this from <c>BasePlayerBB</c> and overrode <c>Public</c>/<c>CanPostGreeting</c>. This
///             codebase's <see cref="BasePlayerBB" /> refuses to open unless the board is locked down inside a house,
///             so a town board could never be used; the board is instead a standalone readable item that renders the
///             city's governor-authored headline and body in its own gump. The governor-only "set headline / set body /
///             set announcement" context entries (commented out in ServUO) are restored here.
///         </item>
///         <item>
///             Headline, body and post date live on <see cref="CityLoyaltySystem" /> so they are saved once with the
///             city, not duplicated onto the item.
///         </item>
///         <item>
///             Gump helpers (<c>BaseGump</c>, <c>BaseQuestGump.C32216</c>) replaced by a plain <see cref="Gump" />.
///         </item>
///     </list>
/// </summary>
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.CityLoyalty;

[SerializationGenerator(0, false)]
public partial class CityMessageBoard : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private City _city;

    [Constructible]
    public CityMessageBoard() : this(City.Britain, 0xA0C5)
    {
    }

    [Constructible]
    public CityMessageBoard(City city, int itemId) : base(itemId)
    {
        Movable = false;
        _city = city;
    }

    public CityLoyaltySystem CitySystem => CityLoyaltySystem.GetCityInstance(_city);

    public override int LabelNumber => 1027774; // bulletin board
    public override bool ForceShowProperties => true;

    public override void OnDoubleClick(Mobile from)
    {
        var sys = CitySystem;

        if (!CityLoyaltySystem.Enabled || sys == null)
        {
            return;
        }

        if (!sys.IsCitizen(from, true))
        {
            // Only Citizens of this City may use this.
            from.SendLocalizedMessage(1154275);
            return;
        }

        if (!from.InRange(Location, 3))
        {
            from.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1019045, from.NetState); // I can't reach that.
            return;
        }

        from.SendGump(new CityNewsGump(sys));
    }

    private void AfterDeserialization()
    {
        if (CitySystem != null)
        {
            CitySystem.Board = this;
        }
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        var sys = CitySystem;

        if (!CityLoyaltySystem.Enabled || sys == null || !sys.IsGovernor(from))
        {
            return;
        }

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154912, // Set New Announcement
                m =>
                {
                    if (m.Prompt != null)
                    {
                        // You already have a text entry request pending.
                        m.SendLocalizedMessage(1079166);
                        return;
                    }

                    m.SendMessage("Введите текст объявления глашатая:");
                    m.BeginPrompt(
                        (mob, text) =>
                        {
                            if (!Server.Guilds.BaseGuildGump.CheckProfanity(text, 150))
                            {
                                mob.SendLocalizedMessage(1112587); // Invalid entry.
                                return;
                            }

                            if (sys.Herald == null)
                            {
                                return;
                            }

                            sys.Herald.Announcement = text;
                            mob.SendMessage("Объявление глашатая обновлено.");
                        },
                        (mob, _) => mob.SendMessage("Объявление глашатая не изменено.")
                    );
                },
                enabled: sys.Herald != null
            )
        );

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154913, // Set Headline
                m =>
                {
                    if (m.Prompt != null)
                    {
                        m.SendLocalizedMessage(1079166);
                        return;
                    }

                    m.SendMessage("Введите заголовок доски объявлений:");
                    m.BeginPrompt(
                        (mob, text) =>
                        {
                            if (!Server.Guilds.BaseGuildGump.CheckProfanity(text, 150))
                            {
                                mob.SendLocalizedMessage(1112587);
                                return;
                            }

                            if (string.IsNullOrEmpty(text))
                            {
                                sys.Headline = null;
                                sys.Body = null;
                                sys.PostedOn = Core.Now;
                                mob.SendMessage($"Заголовок доски объявлений города {sys.Definition.Name} удалён!");
                                return;
                            }

                            sys.Headline = text;
                            sys.PostedOn = Core.Now;
                            mob.SendMessage($"Заголовок доски объявлений города {sys.Definition.Name} изменён!");
                        },
                        (mob, _) => mob.SendMessage("Заголовок не изменён.")
                    );
                }
            )
        );

        list.Add(
            new SimpleContextMenuEntry(
                from,
                1154914, // Set Body
                m =>
                {
                    if (m.Prompt != null)
                    {
                        m.SendLocalizedMessage(1079166);
                        return;
                    }

                    m.SendMessage("Введите текст доски объявлений:");
                    m.BeginPrompt(
                        (mob, text) =>
                        {
                            if (!Server.Guilds.BaseGuildGump.CheckProfanity(text, 150))
                            {
                                mob.SendLocalizedMessage(1112587);
                                return;
                            }

                            if (string.IsNullOrEmpty(text))
                            {
                                sys.Body = null;
                                sys.PostedOn = Core.Now;
                                mob.SendMessage($"Текст доски объявлений города {sys.Definition.Name} удалён!");
                                return;
                            }

                            sys.Body = text;
                            sys.PostedOn = Core.Now;
                            mob.SendMessage($"Текст доски объявлений города {sys.Definition.Name} изменён!");
                        },
                        (mob, _) => mob.SendMessage("Текст не изменён.")
                    );
                }
            )
        );
    }
}

public class CityNewsGump : Gump
{
    public CityNewsGump(CityLoyaltySystem city) : base(100, 100)
    {
        AddBackground(0, 0, 554, 350, 9380);

        // The Latest News from the City of ~1_CITY~
        AddHtmlLocalized(25, 55, 500, 20, 1154915, city.Definition.Name, 0, false, false);

        if (city.PostedOn != System.DateTime.MinValue)
        {
            // Posted by ~1_NAME~ on ~2_date~
            AddHtmlLocalized(
                25,
                85,
                500,
                20,
                1154916,
                $"{city.Governor?.Name ?? "неизвестно"}\t{city.PostedOn.ToShortDateString()}",
                0,
                false,
                false
            );
        }

        AddHtml(25, 115, 500, 20, city.Headline ?? string.Empty, false, false);
        AddHtml(25, 195, 500, 150, city.Body ?? string.Empty, false, false);
    }
}
