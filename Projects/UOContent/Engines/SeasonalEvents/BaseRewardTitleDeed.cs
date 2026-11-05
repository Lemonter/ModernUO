using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     ServUO's reward-title bookkeeping (<c>BaseRewardTitleDeed</c> → <c>PlayerMobile.AddRewardTitle</c>) does not
///     exist in this codebase, so the deed applies the title straight to <see cref="Mobile.Title" /> when the title
///     is a plain string. Cliloc-based deeds (the Treasures of Doom set) only announce the localized title, since a
///     cliloc id cannot be turned into the paperdoll title string on the server.
/// </summary>
[SerializationGenerator(0, false)]
public abstract partial class BaseRewardTitleDeed : Item
{
    protected BaseRewardTitleDeed() : base(0x14F0)
    {
        Hue = 0x481;
        LootType = LootType.Blessed;
    }

    public abstract TextDefinition Title { get; }

    public override string DefaultName => "грамота на титул";

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (Title.String != null)
        {
            from.Title = Title.String;
            from.SendMessage($"Ваш титул изменён на «{Title.String}».");
            return;
        }

        if (Title.Number > 0)
        {
            from.SendMessage("Вы получаете титул:");
            from.SendLocalizedMessage(Title.Number);
        }
    }
}
