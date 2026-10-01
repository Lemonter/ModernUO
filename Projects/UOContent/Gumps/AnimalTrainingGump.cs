using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Systems.MahaonCombat;

namespace Server.Gumps;

/// <summary>
///     Opened by double-clicking a Shepherd's Crook on your own controlled pet — see
///     ShepherdsCrook.cs. Picking a category starts background training via
///     AnimalTrainingSystem; Magic only appears if the trainer's own Magery skill > 0.
/// </summary>
public class AnimalTrainingGump : DynamicGump
{
    private readonly Mobile _trainer;
    private readonly BaseCreature _pet;
    private readonly BaseStaff _crook;

    public AnimalTrainingGump(Mobile trainer, BaseCreature pet, BaseStaff crook) : base(50, 50)
    {
        _trainer = trainer;
        _pet = pet;
        _crook = crook;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var canMagic = AnimalTrainingSystem.CanTrainMagic(_trainer);
        var height = canMagic ? 280 : 240;

        builder.AddPage();
        builder.AddBackground(0, 0, 320, height, 5054);
        builder.AddAlphaRegion(10, 10, 300, height - 20);

        builder.AddHtml(15, 15, 290, 20, $"Обучение: {_pet.Name}");

        // The "not training" message runs ~115 characters — at 290px width that's 3+
        // wrapped lines, and the old height=50 (~3 lines) clipped part of it. Both branches
        // now get the same taller box; everything below shifted down by the same +20.
        if (AnimalTrainingSystem.IsTraining(_pet))
        {
            builder.AddHtml(15, 40, 290, 60, "Питомец уже тренируется. Выберите новую область, чтобы сменить фокус.");
        }
        else
        {
            builder.AddHtml(
                15, 40, 290, 60,
                "Питомцу нужна еда рядом (в пределах нескольких шагов) на всё время тренировки. Ваше Пастушество снижает расход еды."
            );
        }

        builder.AddButton(15, 110, 4005, 4007, 1);
        builder.AddHtml(50, 110, 240, 20, "Сопротивления");

        builder.AddButton(15, 135, 4005, 4007, 2);
        builder.AddHtml(50, 135, 240, 20, "Боевые техники (на манекене)");

        builder.AddButton(15, 160, 4005, 4007, 3);
        builder.AddHtml(50, 160, 240, 20, "Физическая подготовка");

        if (canMagic)
        {
            var focusNote = AnimalTrainingSystem.IsMagicTrained(_pet)
                ? $" — {AnimalTrainingSystem.RuFocusName(AnimalTrainingSystem.GetMagicFocus(_pet))}"
                : "";

            builder.AddButton(15, 185, 4005, 4007, 4);
            builder.AddHtml(50, 185, 240, 20, $"Магия{focusNote}");
        }
        else
        {
            builder.AddHtml(15, 185, 290, 20, "Магия недоступна — вы сами не владеете магией.");
        }

        if (AnimalTrainingSystem.IsTraining(_pet))
        {
            builder.AddButton(15, 215, 4005, 4007, 9);
            builder.AddHtml(50, 215, 240, 20, "Остановить тренировку");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 1:
                AnimalTrainingSystem.StartTraining(_pet, _trainer, AnimalTrainingCategory.Resistances, _crook);
                break;
            case 2:
                AnimalTrainingSystem.StartTraining(_pet, _trainer, AnimalTrainingCategory.CombatSkills, _crook);
                break;
            case 3:
                AnimalTrainingSystem.StartTraining(_pet, _trainer, AnimalTrainingCategory.Stats, _crook);
                break;
            case 4:
                if (AnimalTrainingSystem.CanTrainMagic(_trainer))
                {
                    // Opens the group picker instead of starting training directly — the
                    // trainer always confirms (or switches) a MagicFocus there; StartTraining
                    // itself only runs once a group's actually been picked (see
                    // MagicFocusGump.OnResponse).
                    _trainer.SendGump(new MagicFocusGump(_trainer, _pet, _crook));
                }

                break;
            case 9:
                AnimalTrainingSystem.StopTraining(_pet, $"Тренировка {_pet.Name} остановлена.");
                break;
        }
    }
}
