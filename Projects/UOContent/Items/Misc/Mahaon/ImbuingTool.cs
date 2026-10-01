using ModernUO.Serialization;
using Server.Gumps;
using Server.Systems.MahaonImbuing;
using Server.Targeting;

namespace Server.Items;

/// <summary>Ступка чародея — инструмент наложения чар. Двойной клик, затем таргет на
/// предмет с реагентами (не сам инструмент несёт материалы — они лежат в рюкзаке игрока и
/// списываются автоматически при подтверждении в гампе).</summary>
[SerializationGenerator(0, false)]
public partial class ImbuingTool : Item
{
    [Constructible]
    public ImbuingTool() : base(0xE9B)
    {
        Weight = 3.0;
        Name = "ступка чародея";
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendMessage("Это должно быть у тебя в рюкзаке, чтобы использовать.");
            return;
        }

        if (from.Skills[SkillName.Imbuing].Value <= 0)
        {
            from.SendMessage(0x22, "У тебя нет навыка Наложения чар.");
            return;
        }

        from.SendMessage("Выбери предмет для наложения чар.");
        from.Target = new ImbuingTarget(this);
    }

    private class ImbuingTarget : Target
    {
        private readonly ImbuingTool _tool;

        public ImbuingTarget(ImbuingTool tool) : base(2, false, TargetFlags.None)
        {
            _tool = tool;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Item item)
            {
                from.SendMessage(0x22, "Наложить чары можно только на предмет.");
                return;
            }

            if (!ImbuingSystem.CanImbue(item, out var reason))
            {
                from.SendMessage(0x22, reason);
                return;
            }

            from.CloseGump<ImbuingGump>();
            from.SendGump(new ImbuingGump(from, item));
        }
    }
}
