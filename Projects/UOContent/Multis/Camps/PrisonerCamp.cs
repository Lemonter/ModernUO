using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Multis;

/// <summary>A prisoner locked behind an iron gate with six captors around it. Ported from ServUO
/// (Scripts/Multis/Camps/PrisonerCamp.cs). Which captors is rolled once: orcs, ratmen,
/// lizardmen or brigands.
///
/// The original's own `AddCampChests` call is answered by BaseCamp here — that is where ServUO
/// keeps it too; this codebase had it only on BrigandCamp until now.</summary>
[SerializationGenerator(0, false)]
public partial class PrisonerCamp : BaseCamp
{
    [SerializableField(0)]
    private BaseDoor _gate;

    [SerializableField(1)]
    private Mobile _prisoner;

    [Constructible]
    public PrisonerCamp() : base(0x1D4C)
    {
    }

    public override void AddComponents()
    {
        var gate = new IronGate(DoorFacing.EastCCW)
        {
            KeyValue = Key.RandomValue(),
            Locked = true
        };

        _gate = gate;
        AddItem(gate, -2, 1, 0);

        AddCampChests();

        switch (Utility.Random(4))
        {
            case 0:
                {
                    AddMobile(new Orc(), 6, 0, -2, 0);
                    AddMobile(new OrcishMage(), 6, 0, 1, 0);
                    AddMobile(new OrcishLord(), 6, 0, -2, 0);
                    AddMobile(new OrcCaptain(), 6, 0, 1, 0);
                    AddMobile(new Orc(), 6, 0, -1, 0);
                    AddMobile(new OrcChopper(), 6, 0, -2, 0);
                    break;
                }
            case 1:
                {
                    AddMobile(new Ratman(), 6, 0, -2, 0);
                    AddMobile(new Ratman(), 6, 0, 1, 0);
                    AddMobile(new RatmanMage(), 6, 0, -2, 0);
                    AddMobile(new Ratman(), 6, 0, 1, 0);
                    AddMobile(new RatmanArcher(), 6, 0, -1, 0);
                    AddMobile(new Ratman(), 6, 0, -2, 0);
                    break;
                }
            case 2:
                {
                    for (var i = 0; i < 6; i++)
                    {
                        AddMobile(new Lizardman(), 6, 0, i switch { 1 or 3 => 1, 4 => -1, _ => -2 }, 0);
                    }

                    break;
                }
            default:
                {
                    for (var i = 0; i < 6; i++)
                    {
                        AddMobile(new Brigand(), 6, 0, i switch { 1 or 3 => 1, 4 => -1, _ => -2 }, 0);
                    }

                    break;
                }
        }

        BaseCreature bc = Utility.RandomBool() ? new Noble() : new SeekerOfAdventure();

        bc.IsPrisoner = true;
        bc.CantWalk = true;
        _prisoner = bc;

        _prisoner.YellHue = Utility.RandomList(0x57, 0x67, 0x77, 0x87, 0x117);
        AddMobile(_prisoner, 2, -2, 0, 0);
    }

    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);

        if (!m.Player || _prisoner == null || _gate?.Locked != true)
        {
            return;
        }

        var number = Utility.Random(10) switch
        {
            1 => 502266,  // Aaah! Help me!
            2 => 1046000, // Help! These savages wish to end my life!
            3 => 1046003, // Quickly! Kill them for me! HELP!!
            4 => 502261,  // HELP!
            5 => 502262,  // Help me!
            6 => 502263,  // Canst thou aid me?!
            7 => 502265,  // Help! Please!
            8 => 502267,  // Go and get some help!
            9 => 502268,  // Quickly, I beg thee! Unlock my chains!
            _ => 502264   // Help a poor prisoner!
        };

        _prisoner.Yell(number);
    }
}

/// <summary>A brigand camp staffed by elves. Ported from ServUO
/// (Scripts/Multis/Camps/ElfBrigandCamp.cs) — the ordinary camp with one line changed.</summary>
[SerializationGenerator(0, false)]
public partial class ElfBrigandCamp : BrigandCamp
{
    [Constructible]
    public ElfBrigandCamp()
    {
    }

    public override Mobile Brigands => new ElfBrigand();
}
