using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Shadowguard;

public class ShadowguardGump : DynamicGump
{
    private const int Red = 0xF800;
    private const int Green = 0x07E0;

    private static readonly EncounterType[] _encounters =
    {
        EncounterType.Bar, EncounterType.Orchard, EncounterType.Armory,
        EncounterType.Fountain, EncounterType.Belfry, EncounterType.Roof
    };

    public PlayerMobile User { get; }

    public ShadowguardGump(PlayerMobile user) : base(100, 50) => User = user;

    public override bool Singleton => true;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 400, 400, 83);
        builder.AddHtmlLocalized(0, 10, 400, 16, 1154645, "#1156164", 0xFFFF); // Shadowguard
        builder.AddHtmlLocalized(0, 45, 400, 16, 1154645, "#1156181", 0xFFFF); // Select the area of Shadowguard you wish to explore...

        var controller = ShadowguardController.Instance;

        for (var i = 0; i < _encounters.Length; i++)
        {
            var encounter = _encounters[i];
            var hue = controller.HasCompletedEncounter(User, encounter) ? Green : Red;

            builder.AddHtmlLocalized(50, 78 + i * 20, 200, 16, ShadowguardController.GetLocalization(encounter), hue);
            builder.AddButton(15, 80 + i * 20, 1209, 1210, i + 1);
        }

        if (controller.IsInQueue(User))
        {
            builder.AddHtmlLocalized(50, 358, 200, 16, 1156247, 0xFFFFFF); // Exit Shadowguard Queues
            builder.AddButton(15, 360, 1209, 1210, 123);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var controller = ShadowguardController.Instance;

        if (info.ButtonID == 123)
        {
            if (controller.RemoveFromQueue(User))
            {
                User.SendLocalizedMessage(1156248); // You have been removed from all Shadowguard queues
            }

            return;
        }

        if (info.ButtonID <= 0)
        {
            return;
        }

        var id = info.ButtonID - 1;

        if (id < 0 || id >= _encounters.Length)
        {
            return;
        }

        var type = _encounters[id];

        if (!controller.CanTryEncounter(User, type))
        {
            return;
        }

        var inst = controller.GetAvailableInstance(type);

        if (inst == null)
        {
            controller.AddToQueue(User, type);
        }
        else
        {
            inst.TryBeginEncounter(User, false, type);
            controller.RemoveFromQueue(User);
        }
    }
}
