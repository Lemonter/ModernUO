/// <summary>
///     Портировано из ServUO (Scripts/Services/New Magincia/Distillation/DistillationGump.cs).
///
///     Окно дистилляции: выбор группы, напитка, дрожжей и параметров, затем наведение на бочку. Всё
///     содержимое — cliloc-идентификаторы, поэтому переводить здесь нечего; собственные строки (сообщения и
///     подпись поля метки) локализованы на русский.
///
///     Замены:
///     <list type="bullet">
///         <item>
///             Пустой гумп не создаётся: статический <see cref="DisplayTo" /> проверяет, что для текущего
///             состояния есть описание рецепта, и только потом открывает окно. Конструктор без описания не
///             строится (см. <c>dev-docs/gump-system.md</c>).
///         </item>
///         <item>
///             <c>ObjectPropertyList</c> не используется; <c>AddHtmlLocalized</c> принимает те же аргументы.
///         </item>
///     </list>
/// </summary>
using System;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Prompts;
using Server.Targeting;

namespace Server.Engines.Distillation;

public class DistillationGump : Gump
{
    private const int LabelColor = 0x7FFF;

    private readonly DistillationContext _context;
    private readonly CraftDefinition _def;

    private DistillationGump(Mobile from, DistillationContext context, CraftDefinition def) : base(35, 35)
    {
        _context = context;
        _def = def;

        var group = context.LastGroup;
        var liquor = context.LastLiquor;

        if (liquor == Liquor.None)
        {
            liquor = DistillationSystem.GetFirstLiquor(group);
        }

        if (def.Liquor != liquor)
        {
            liquor = def.Liquor;
        }

        AddBackground(0, 0, 500, 500, 5054);
        AddImageTiled(10, 10, 480, 30, 2624);
        AddImageTiled(10, 50, 230, 120, 2624);
        AddImageTiled(10, 180, 230, 200, 2624);
        AddImageTiled(250, 50, 240, 200, 2624);
        AddImageTiled(250, 260, 240, 120, 2624);
        AddImageTiled(10, 390, 480, 60, 2624);
        AddImageTiled(10, 460, 480, 30, 2624);
        AddAlphaRegion(10, 10, 480, 480);

        AddHtmlLocalized(10, 16, 510, 20, 1150682, LabelColor, false, false); // <center>DISTILLERY MENU</center>

        AddHtmlLocalized(10, 55, 230, 20, 1150683, LabelColor, false, false); // <center>Select the group</center>

        AddButton(15, 80, 4005, 4007, 1);
        AddHtmlLocalized(
            55, 80, 200, 20, DistillationSystem.GetLabel(Group.WheatBased), LabelColor, false, false
        ); // WHEAT BASED

        AddButton(15, 106, 4005, 4007, 2);
        AddHtmlLocalized(
            55, 106, 200, 20, DistillationSystem.GetLabel(Group.WaterBased), LabelColor, false, false
        ); // WATER BASED

        AddButton(15, 132, 4005, 4007, 3);
        AddHtmlLocalized(55, 132, 200, 20, DistillationSystem.GetLabel(Group.Other), LabelColor, false, false); // OTHER

        AddHtmlLocalized(10, 184, 230, 20, 1150684, LabelColor, false, false); // <center>Select the liquor type</center>

        var y = 210;

        for (var i = 0; i < DistillationSystem.CraftDefs.Count; i++)
        {
            var craft = DistillationSystem.CraftDefs[i];

            if (craft.Group != group)
            {
                continue;
            }

            AddButton(15, y, 4005, 4007, 1000 + i);
            AddHtmlLocalized(55, y, 200, 20, DistillationSystem.GetLabel(craft.Liquor, false), LabelColor, false, false);
            y += 26;
        }

        // <center>Ingredients of ~1_NAME~</center>
        AddHtmlLocalized(250, 54, 240, 20, 1150735, $"#{DistillationSystem.GetLabel(liquor, false)}", LabelColor, false, false);

        y = 80;

        for (var i = 0; i < def.Ingredients.Length; i++)
        {
            var type = def.Ingredients[i];
            var amount = def.Amounts[i];

            if (i == 0 && type == typeof(Yeast))
            {
                for (var j = 0; j < amount; j++)
                {
                    var yeast = context.SelectedYeast[j];

                    // Yeast: ~1_VAL~
                    AddHtmlLocalized(295, y, 200, 20, yeast == null ? 1150778 : 1150779, "#1150453", LabelColor, false, false);
                    AddButton(255, y, 4005, 4007, 2000 + j);
                    y += 26;
                }

                continue;
            }

            var total = context.MakeStrong ? amount * 2 : amount;
            var owned = DistillationTarget.GetAmount(from, type, liquor);

            if (owned > total)
            {
                owned = total;
            }

            // ~1_NAME~ : ~2_NUMBER~
            AddHtmlLocalized(
                295, y, 200, 20, 1150733, $"#{def.Labels[i]}\t{owned}/{total}", LabelColor, false, false
            );

            y += 26;
        }

        AddHtmlLocalized(250, 264, 240, 20, 1150770, LabelColor, false, false); // <center>Distillery Options</center>

        AddButton(255, 290, 4005, 4007, 4);
        // Double Distillation - Standard Distillation
        AddHtmlLocalized(295, 290, 200, 20, context.MakeStrong ? 1150730 : 1150729, LabelColor, false, false);

        AddButton(255, 316, 4005, 4007, 5);
        // Mark Distiller Name - Do Not Mark
        AddHtmlLocalized(295, 320, 200, 20, context.Mark ? 1150731 : 1150732, LabelColor, false, false);

        AddButton(15, 395, 4005, 4007, 6);
        // ~1_NAME~ : ~2_NUMBER~
        AddHtmlLocalized(
            55, 395, 200, 20, 1150733, $"Метка\t{context.Label ?? "нет"}", LabelColor, false, false
        );

        AddButton(15, 465, 4005, 4007, 7);
        AddHtmlLocalized(55, 465, 200, 20, 1150771, LabelColor, false, false); // Execute Distillation

        AddButton(455, 465, 4017, 4019, 0);
        AddHtmlLocalized(400, 465, 100, 20, 1060675, LabelColor, false, false); // CLOSE
    }

    /// <summary>
    ///     Открывает окно, только если для текущего состояния игрока есть рецепт. Пустой гумп не создаётся.
    /// </summary>
    public static void DisplayTo(Mobile from)
    {
        if (from == null)
        {
            return;
        }

        var context = DistillationSystem.GetContext(from);
        var liquor = context.LastLiquor == Liquor.None
            ? DistillationSystem.GetFirstLiquor(context.LastGroup)
            : context.LastLiquor;

        var def = DistillationSystem.GetDefinition(liquor, context.LastGroup);

        if (def == null)
        {
            return;
        }

        from.CloseGump<DistillationGump>();
        from.SendGump(new DistillationGump(from, context, def));
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;

        if (from == null)
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    return;
                }
            case 1:
                {
                    _context.LastGroup = Group.WheatBased;
                    break;
                }
            case 2:
                {
                    _context.LastGroup = Group.WaterBased;
                    break;
                }
            case 3:
                {
                    _context.LastGroup = Group.Other;
                    break;
                }
            case 4:
                {
                    _context.MakeStrong = !_context.MakeStrong;
                    break;
                }
            case 5:
                {
                    _context.Mark = !_context.Mark;
                    break;
                }
            case 6:
                {
                    from.Prompt = new LabelPrompt(_context);
                    // Please enter the text with which you wish to label the liquor ...
                    from.SendLocalizedMessage(1150734);
                    return;
                }
            case 7:
                {
                    from.Target = new DistillationTarget(from, _context, _def);
                    // Target an empty liquor barrel in your backpack ...
                    from.SendLocalizedMessage(1150810);
                    return;
                }
            default:
                {
                    if (info.ButtonID < 2000)
                    {
                        var index = info.ButtonID - 1000;

                        if (index >= 0 && index < DistillationSystem.CraftDefs.Count)
                        {
                            _context.LastLiquor = DistillationSystem.CraftDefs[index].Liquor;
                        }
                    }
                    else
                    {
                        var index = info.ButtonID - 2000;

                        if (_def.Ingredients[0] == typeof(Yeast) && _def.Amounts.Length > 0 && _def.Amounts[0] > index)
                        {
                            from.Target = new YeastSelectionTarget(_context, index);
                            // Target the yeast in your backpack that you wish to toggle ...
                            from.SendLocalizedMessage(1150437);
                            return;
                        }
                    }

                    break;
                }
        }

        DisplayTo(from);
    }

    private class LabelPrompt : Prompt
    {
        private readonly DistillationContext _context;

        public LabelPrompt(DistillationContext context) => _context = context;

        public override void OnResponse(Mobile from, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _context.Label = null;
            }
            else
            {
                text = text.Trim();

                if (text.Length > 15 || !Server.Guilds.BaseGuildGump.CheckProfanity(text))
                {
                    from.SendMessage("Такая метка недопустима. Попробуйте ещё раз.");
                }
                else
                {
                    _context.Label = text;
                }
            }

            DisplayTo(from);
        }

        public override void OnCancel(Mobile from) => DisplayTo(from);
    }

    public class DistillationTarget : Target
    {
        private readonly DistillationContext _context;
        private readonly CraftDefinition _def;

        public DistillationTarget(Mobile from, DistillationContext context, CraftDefinition def)
            : base(-1, false, TargetFlags.None)
        {
            _context = context;
            _def = def;
            BeginTimeout(from, 30000);
        }

        public static int GetAmount(Mobile from, Type type, Liquor liquor)
        {
            var pack = from?.Backpack;

            if (pack == null)
            {
                return 0;
            }

            if (type == typeof(Pitcher))
            {
                var amount = 0;
                var beverage = liquor == Liquor.Brandy ? BeverageType.Wine : BeverageType.Water;

                foreach (var item in pack.FindItemsByType<Pitcher>())
                {
                    if (item.Content == beverage && item.Quantity >= item.MaxQuantity)
                    {
                        amount++;
                    }
                }

                return amount;
            }

            if (type == typeof(PewterBowlOfCorn))
            {
                return pack.GetAmount(type) + pack.GetAmount(typeof(WoodenBowlOfCorn));
            }

            return pack.GetAmount(type);
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not LiquorBarrel barrel)
            {
                from.SendMessage("Это не бочка для напитков.");
                DistillationSystem.SendDelayedGump(from);
                return;
            }

            if (!barrel.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1054107); // This item must be in your backpack.
            }
            else if (barrel.IsMature)
            {
                from.SendLocalizedMessage(1150811); // This liquor barrel already contains liquor.
            }
            else if (!barrel.IsEmpty)
            {
                // You realize that the liquor is on the process of maturation so you leave it alone.
                from.SendLocalizedMessage(1150802);
            }
            else
            {
                var percentage = _context.MakeStrong ? 2 : 1;

                if (!HasTotal(from, percentage))
                {
                    from.SendLocalizedMessage(1150747); // You don't have enough ingredients.
                }
                else
                {
                    var cooking = from.Skills[SkillName.Cooking].Value;
                    var alchemy = from.Skills[SkillName.Alchemy].Value / 2;
                    var resist = 0;

                    foreach (var yeast in _context.SelectedYeast)
                    {
                        if (yeast != null)
                        {
                            resist += yeast.BacterialResistance;
                        }
                    }

                    var chance = (int)((cooking + alchemy + resist * 12) / 2.5);

                    if (chance > Utility.Random(100))
                    {
                        ConsumeTotal(from, percentage);
                        _context.ClearYeasts();
                        from.SendLocalizedMessage(1150772); // You succeed at your distillation attempt.

                        Mobile marker = _context.Mark ? from : null;
                        from.PlaySound(0x2D6);

                        barrel.BeginDistillation(
                            _def.Liquor, _def.MaturationDuration, _context.Label, _context.MakeStrong, marker
                        );
                    }
                    else
                    {
                        from.PlaySound(0x2D6);
                        ConsumeTotal(from, percentage / 2);
                        // You have failed your distillation attempt and ingredients have been lost.
                        from.SendLocalizedMessage(1150745);
                    }
                }
            }

            DistillationSystem.SendDelayedGump(from);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (cancelType == TargetCancelType.Timeout)
            {
                // You have waited too long to make your selection, your distillation attempt has timed out.
                from.SendLocalizedMessage(1150859);
            }

            DisplayTo(from);
        }

        private bool HasTotal(Mobile from, double percentage)
        {
            for (var i = 0; i < _def.Ingredients.Length; i++)
            {
                var type = _def.Ingredients[i];

                if (i == 0 && type == typeof(Yeast))
                {
                    for (var j = 0; j < _def.Amounts[i]; j++)
                    {
                        var yeast = _context.SelectedYeast[j];

                        if (yeast == null || !yeast.IsChildOf(from.Backpack))
                        {
                            return false;
                        }
                    }

                    continue;
                }

                var toConsume = (int)(_def.Amounts[i] * percentage);

                if (GetAmount(from, type, _def.Liquor) < toConsume)
                {
                    return false;
                }
            }

            return true;
        }

        private void ConsumeTotal(Mobile from, double percentage)
        {
            var pack = from?.Backpack;

            if (pack == null)
            {
                return;
            }

            for (var i = 0; i < _def.Ingredients.Length; i++)
            {
                var type = _def.Ingredients[i];

                if (i == 0 && type == typeof(Yeast))
                {
                    for (var j = 0; j < _def.Amounts[i]; j++)
                    {
                        _context.SelectedYeast[j]?.Consume();
                    }

                    continue;
                }

                if (type == typeof(Pitcher))
                {
                    var toConsume = (int)(_def.Amounts[i] * percentage);
                    var beverage = _def.Liquor == Liquor.Brandy ? BeverageType.Wine : BeverageType.Water;

                    if (toConsume <= 0)
                    {
                        continue;
                    }

                    foreach (var pitcher in pack.FindItemsByType<Pitcher>())
                    {
                        if (toConsume <= 0)
                        {
                            break;
                        }

                        if (pitcher.Content != beverage || pitcher.Quantity < pitcher.MaxQuantity)
                        {
                            continue;
                        }

                        pitcher.Quantity = 0;
                        toConsume--;
                    }

                    continue;
                }

                if (type == typeof(PewterBowlOfCorn))
                {
                    var toConsume = (int)(_def.Amounts[i] * percentage);

                    if (toConsume <= 0)
                    {
                        continue;
                    }

                    var totalPewter = pack.GetAmount(type);

                    if (totalPewter >= toConsume)
                    {
                        pack.ConsumeTotal(type, toConsume);
                    }
                    else
                    {
                        pack.ConsumeTotal(type, totalPewter);
                        pack.ConsumeTotal(typeof(WoodenBowlOfCorn), toConsume - totalPewter);
                    }

                    continue;
                }

                pack.ConsumeTotal(type, (int)(_def.Amounts[i] * percentage));
            }
        }
    }

    private class YeastSelectionTarget : Target
    {
        private readonly DistillationContext _context;
        private readonly int _index;

        public YeastSelectionTarget(DistillationContext context, int index) : base(-1, false, TargetFlags.None)
        {
            _index = index;
            _context = context;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not Yeast yeast)
            {
                from.SendLocalizedMessage(1150741); // That is not yeast.
            }
            else if (!yeast.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1054107); // This item must be in your backpack.
            }
            else if (_context.YeastInUse(yeast))
            {
                from.SendLocalizedMessage(1150742); // You have already chosen other yeast.
            }
            else
            {
                _context.SelectedYeast[_index] = yeast;
                from.SendLocalizedMessage(1150740); // You have chosen the yeast.
            }

            DisplayTo(from);
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (_context.SelectedYeast[_index] != null)
            {
                _context.SelectedYeast[_index] = null;
                from.SendLocalizedMessage(1150743); // You no longer choose this yeast.
            }

            DisplayTo(from);
        }
    }
}
