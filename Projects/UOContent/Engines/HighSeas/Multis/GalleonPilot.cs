/// <summary>
///     Портировано из ServUO (Scripts/Services/Expansions/High Seas/Multis/GalleonPilot.cs).
///
///     NPC-кормчий галеры: стоит на палубе, слушает приказы «вперёд/назад/влево/вправо/стоп» и
///     повороты, по двойному клику переименовывает корабль или ставит его в сухой док, а через
///     контекстное меню открывает настройки доступа.
///
///     Ключевое отличие от ServUO: там кормчий ПОДМЕНЯЛ собой <c>TillerMan</c> и был ещё и
///     «рулём» для mouse-pilot пакета 0x33. В этом движке <c>TillerMan</c> — предмет
///     (<c>Server.Items.TillerMan</c>), мышиного пилота нет, поэтому кормчий — отдельный мобил
///     на палубе, а штатный рычаг у галеры скрыт (<see cref="BaseGalleon" />). Корабль ходит через
///     <c>BaseBoat.StartMove(dir, fast)</c> и <c>BaseBoat.StartTurn</c>/<c>Facing</c> — точно как
///     классические лодки. Раз рулевой один, кормчий сам озвучивает и «слушаюсь», и отказы
///     (якорь, нет прав, некуда идти).
///
///     Команды принимаются двумя путями:
///     <list type="bullet">
///         <item>
///             keyword-ы клиента (0x45 «forward» … 0x4F «stop», 0x65/0x66 — повороты) — тот же набор,
///             что разбирает <c>BaseBoat.OnSpeech</c> у классических лодок;
///         </item>
///         <item>
///             русские слова в речи («вперёд», «назад», «влево», «вправо», «стоп», «налево»,
///             «направо»): клиент кодирует keyword-ы только для английских команд, поэтому без этого
///             на русском клиенте галера не слушалась бы вовсе.
///         </item>
///     </list>
///
///     Не портировано (этапы 3–4): <c>MoveTillermanEntry</c> (в этом движке позиция кормчего
///     пересчитывается движком корабля) и «сброс списка доступа» через <c>BasicConfirmGump</c>
///     (в ModernUO такого окна нет — записи удаляются по одной в <c>GrantAccessGump</c>).
///     Ремонт корпуса (этап 2) портирован: <see cref="EmergencyRepairEntry" /> и
///     <see cref="ShipRepairEntry" /> вызывают <see cref="BaseGalleon.TryEmergencyRepair" /> и
///     <see cref="BaseGalleon.TryRepairs" />.
/// </summary>
using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.Engines.HighSeas;

[SerializationGenerator(0, false)]
public partial class GalleonPilot : BaseCreature
{
    private const Direction ForwardDirection = Direction.North;
    private const Direction BackwardDirection = Direction.South;
    private const Direction LeftDirection = Direction.West;
    private const Direction RightDirection = Direction.East;

    [SerializableField(0, setter: "private")]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BaseGalleon _galleon;

    /// <summary>
    ///     Пустой конструктор — только ради <c>[add</c> (правило «у Item/Mobile есть
    ///     <c>[Constructible]</c> без параметров»). Такой кормчий ничем не управляет и удаляется при
    ///     первой загрузке мира: настоящий кормчий всегда создаётся кораблём.
    /// </summary>
    [Constructible]
    public GalleonPilot() : this(null)
    {
    }

    public GalleonPilot(BaseGalleon galleon) : base(AIType.AI_Animal, FightMode.None)
    {
        _galleon = galleon;

        CantWalk = true;
        Blessed = true;

        InitBody();
        InitOutfit();

        SetSkill(SkillName.Cartography, 100.0);

        // В ServUO скорость задавалась аргументами base(...); здесь она берётся из Data/npc-speeds.json,
        // а для этого типа записи нет — ставим те же значения, что были в исходнике.
        SetSpeed(0.5, 2.0);
    }

    private void InitBody()
    {
        InitStats(100, 100, 25);

        SpeechHue = Utility.RandomDyedHue();
        Hue = Race.Human.RandomSkinHue();
        Title = "кормчий";

        if (Female = Utility.RandomBool())
        {
            Body = 0x191;
            Name = RussianNames.RandomFemale();
        }
        else
        {
            Body = 0x190;
            Name = RussianNames.RandomMale();
        }
    }

    /// <summary>
    ///     Одежда кормчего. В ServUO был ещё «токунский» набор по <c>Map == Map.Tokuno</c>, но кормчий
    ///     создаётся до постановки корабля в мир (карта — <c>Map.Internal</c>), так что та ветка не
    ///     срабатывала никогда; оставлен только обычный набор.
    /// </summary>
    private void InitOutfit()
    {
        switch (Utility.Random(3))
        {
            case 0:
                AddItem(new FancyShirt(GetRandomHue()));
                break;
            case 1:
                AddItem(new Doublet(GetRandomHue()));
                break;
            default:
                AddItem(new Shirt(GetRandomHue()));
                break;
        }

        switch (Utility.Random(4))
        {
            case 0:
                AddItem(new Shoes(GetShoeHue()));
                break;
            case 1:
                AddItem(new Boots(GetShoeHue()));
                break;
            case 2:
                AddItem(new Sandals(GetShoeHue()));
                break;
            default:
                AddItem(new ThighBoots(GetShoeHue()));
                break;
        }

        if (Female)
        {
            switch (Utility.Random(6))
            {
                case 0:
                    AddItem(new ShortPants(GetRandomHue()));
                    break;
                case 1:
                case 2:
                    AddItem(new Kilt(GetRandomHue()));
                    break;
                default:
                    AddItem(new Skirt(GetRandomHue()));
                    break;
            }
        }
        else
        {
            switch (Utility.Random(2))
            {
                case 0:
                    AddItem(new LongPants(GetRandomHue()));
                    break;
                default:
                    AddItem(new ShortPants(GetRandomHue()));
                    break;
            }
        }

        var hairHue = Race.Human.RandomHairHue();

        Utility.AssignRandomHair(this, hairHue);
        Utility.AssignRandomFacialHair(this, hairHue);
    }

    private static int GetRandomHue() => Utility.Random(5) switch
    {
        0 => Utility.RandomBlueHue(),
        1 => Utility.RandomGreenHue(),
        2 => Utility.RandomRedHue(),
        3 => Utility.RandomYellowHue(),
        _ => Utility.RandomNeutralHue()
    };

    private static int GetShoeHue() => Utility.RandomDouble() < 0.1 ? 0 : Utility.RandomNeutralHue();

    public void SetFacing(Direction dir) => Direction = dir;

    public override void AddNameProperties(IPropertyList list)
    {
        var galleon = _galleon;

        if (galleon == null)
        {
            base.AddNameProperties(list);
            return;
        }

        list.Add(1050045, $"{" "}\t{Name ?? " "}\t{GetShipSuffix(galleon)}"); // ~1_PREFIX~~2_NAME~~3_SUFFIX~
        list.Add(galleon.Status); // состояние корпуса (как у TillerMan)
        list.Add(1116580 + (int)galleon.DamageTaken); // State: Pristine
    }

    private static string GetShipSuffix(BaseGalleon galleon)
    {
        var shipName = galleon.ShipName;

        return string.IsNullOrEmpty(shipName)
            ? " — кормчий безымянного корабля"
            : $" — кормчий корабля «{shipName}»";
    }

    public override void OnDoubleClick(Mobile from)
    {
        var galleon = _galleon;

        if (from == null)
        {
            return;
        }

        if (galleon == null)
        {
            base.OnDoubleClick(from);
            return;
        }

        if (!galleon.IsOwner(from) && from.AccessLevel <= AccessLevel.Player)
        {
            return;
        }

        if (galleon.Contains(from))
        {
            // Штатный TillerMan у галеры спрятан, поэтому вопрос о названии задаёт кормчий.
            Say("Каким именем назвать корабль, капитан?");
            galleon.BeginRename(from);
        }
        else
        {
            galleon.BeginDryDock(from);
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        var galleon = _galleon;

        if (from == null)
        {
            return false;
        }

        if (dropped is MapItem map && galleon?.Deleted == false && galleon.Contains(from) && galleon.HasCommandRights(from))
        {
            galleon.AssociateMap(map);
            return true;
        }

        return base.OnDragDrop(from, dropped);
    }

    public override bool CheckNonlocalLift(Mobile from, Item item)
    {
        var galleon = _galleon;

        if (galleon == null || from == null)
        {
            return false;
        }

        return galleon.GetSecurityLevel(from) >= SecurityLevel.Captain || base.CheckNonlocalLift(from, item);
    }

    public override bool AllowEquipFrom(Mobile from)
    {
        var galleon = _galleon;

        if (galleon == null || from == null)
        {
            return false;
        }

        return galleon.GetSecurityLevel(from) >= SecurityLevel.Captain || base.AllowEquipFrom(from);
    }

    public override bool HandlesOnSpeech(Mobile from) =>
        _galleon?.Deleted == false && from?.InRange(Location, 12) == true;

    public override void OnSpeech(SpeechEventArgs e)
    {
        var from = e.Mobile;
        var galleon = _galleon;

        if (from == null || galleon == null)
        {
            return;
        }

        var command = ResolveCommand(e);

        if (command == ShipCommand.None)
        {
            return;
        }

        if (!galleon.Contains(from))
        {
            from.SendMessage(0x22, "Поднимитесь на палубу, чтобы отдать приказ кормчему.");
            return;
        }

        if (!galleon.HasCommandRights(from))
        {
            from.SendMessage(0x22, "Вы не вправе командовать этим кораблём.");
            return;
        }

        Execute(command, galleon);
    }

    private void Execute(ShipCommand command, BaseGalleon galleon)
    {
        // Пробитый корпус не слушается: сначала аварийные заделки, иначе корабль идёт ко дну.
        if (galleon.Scuttled)
        {
            Say("Корпус не держит, капитан! Нужны аварийные заделки!");
            return;
        }

        switch (command)
        {
            case ShipCommand.RaiseAnchor:
                if (galleon.RaiseAnchor(false))
                {
                    Say("Якорь поднят, капитан!");
                }
                else
                {
                    Say("Якорь и так поднят.");
                }

                return;
            case ShipCommand.LowerAnchor:
                if (galleon.LowerAnchor(false))
                {
                    Say("Якорь опущен, капитан!");
                }
                else
                {
                    Say("Якорь уже опущен.");
                }

                return;
            case ShipCommand.Stop:
                if (galleon.StopMove(false))
                {
                    Say("Стоп машина!");
                }
                else
                {
                    Say("Корабль и так стоит на месте, капитан.");
                }

                return;
        }

        if (galleon.Anchored)
        {
            Say("Якорь опущен, капитан — прикажите поднять его.");
            return;
        }

        var moved = command switch
        {
            ShipCommand.Forward   => galleon.TrySail(ForwardDirection, true),
            ShipCommand.Backward  => galleon.TrySail(BackwardDirection, true),
            ShipCommand.Left      => galleon.TrySail(LeftDirection, true),
            ShipCommand.Right     => galleon.TrySail(RightDirection, true),
            ShipCommand.TurnLeft  => galleon.StartTurn(-2, true),
            ShipCommand.TurnRight => galleon.StartTurn(2, true),
            _                     => false
        };

        if (moved)
        {
            Say("Слушаюсь, капитан!");
        }
        else
        {
            Say("Не выходит, капитан: место занято или корабль не может идти.");
        }
    }

    private static ShipCommand ResolveCommand(SpeechEventArgs e)
    {
        var speech = e.Speech;

        // Якорь: клиент кодирует keyword-и только для английских команд (0x6A/0x6B), поэтому
        // «поднять/опустить якорь» разбираем по тексту.
        if (!string.IsNullOrEmpty(speech))
        {
            if (speech.Contains("поднять якорь", StringComparison.OrdinalIgnoreCase) ||
                speech.Contains("подними якорь", StringComparison.OrdinalIgnoreCase) ||
                speech.Contains("отдать якорь", StringComparison.OrdinalIgnoreCase))
            {
                return ShipCommand.RaiseAnchor;
            }

            if (speech.Contains("опустить якорь", StringComparison.OrdinalIgnoreCase) ||
                speech.Contains("бросить якорь", StringComparison.OrdinalIgnoreCase))
            {
                return ShipCommand.LowerAnchor;
            }
        }

        if (e.HasKeyword(0x6B))
        {
            return ShipCommand.RaiseAnchor;
        }

        if (e.HasKeyword(0x6A))
        {
            return ShipCommand.LowerAnchor;
        }

        if (e.HasKeyword(0x45) || e.HasKeyword(0x52))
        {
            return ShipCommand.Forward;
        }

        if (e.HasKeyword(0x46) || e.HasKeyword(0x53))
        {
            return ShipCommand.Backward;
        }

        if (e.HasKeyword(0x47) || e.HasKeyword(0x50))
        {
            return ShipCommand.Left;
        }

        if (e.HasKeyword(0x48) || e.HasKeyword(0x51))
        {
            return ShipCommand.Right;
        }

        if (e.HasKeyword(0x49) || e.HasKeyword(0x65))
        {
            return ShipCommand.TurnRight;
        }

        if (e.HasKeyword(0x4A) || e.HasKeyword(0x66))
        {
            return ShipCommand.TurnLeft;
        }

        if (e.HasKeyword(0x4F) || e.HasKeyword(0x69))
        {
            return ShipCommand.Stop;
        }

        if (string.IsNullOrEmpty(speech))
        {
            return ShipCommand.None;
        }

        if (speech.Contains("налево", StringComparison.OrdinalIgnoreCase))
        {
            return ShipCommand.TurnLeft;
        }

        if (speech.Contains("направо", StringComparison.OrdinalIgnoreCase))
        {
            return ShipCommand.TurnRight;
        }

        if (speech.Contains("вперёд", StringComparison.OrdinalIgnoreCase) ||
            speech.Contains("вперед", StringComparison.OrdinalIgnoreCase))
        {
            return ShipCommand.Forward;
        }

        if (speech.Contains("назад", StringComparison.OrdinalIgnoreCase))
        {
            return ShipCommand.Backward;
        }

        if (speech.Contains("влево", StringComparison.OrdinalIgnoreCase))
        {
            return ShipCommand.Left;
        }

        if (speech.Contains("вправо", StringComparison.OrdinalIgnoreCase))
        {
            return ShipCommand.Right;
        }

        if (speech.Contains("стоп", StringComparison.OrdinalIgnoreCase) ||
            speech.Contains("стой", StringComparison.OrdinalIgnoreCase))
        {
            return ShipCommand.Stop;
        }

        return ShipCommand.None;
    }

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        var galleon = _galleon;

        if (galleon == null)
        {
            return;
        }

        if (galleon.Contains(from))
        {
            var level = galleon.GetSecurityLevel(from);

            // Ремонт корпуса — матросам и выше (в ServUO — level >= Crewman).
            if (level >= SecurityLevel.Crewman)
            {
                list.Add(new EmergencyRepairEntry(galleon, from));
                list.Add(new ShipRepairEntry(galleon, from));
            }

            if (level == SecurityLevel.Captain)
            {
                list.Add(new RenameShipEntry(galleon, from));
                list.Add(new SecuritySettingsEntry(galleon, from));
            }
        }
        else if (galleon.IsOwner(from))
        {
            list.Add(new DryDockEntry(galleon, from));
        }
    }

    public override void OnAfterDelete()
    {
        var galleon = _galleon;
        _galleon = null;

        if (galleon?.Deleted == false)
        {
            galleon.Delete();
        }

        base.OnAfterDelete();
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        if (_galleon?.Deleted != false)
        {
            Timer.DelayCall(Delete);
        }
    }

    private enum ShipCommand
    {
        None,
        Forward,
        Backward,
        Left,
        Right,
        TurnLeft,
        TurnRight,
        Stop,
        RaiseAnchor,
        LowerAnchor
    }

    private class RenameShipEntry : ContextMenuEntry
    {
        private readonly BaseGalleon _galleon;
        private readonly Mobile _from;

        public RenameShipEntry(BaseGalleon galleon, Mobile from) : base(1111680, 3) // Rename Ship
        {
            _galleon = galleon;
            _from = from;
            Enabled = galleon.IsOwner(from);
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_galleon?.Deleted == false && _galleon.IsOwner(_from))
            {
                _galleon.BeginRename(_from);
            }
        }
    }

    private class DryDockEntry : ContextMenuEntry
    {
        private readonly BaseGalleon _galleon;
        private readonly Mobile _from;

        public DryDockEntry(BaseGalleon galleon, Mobile from) : base(1116520, 12) // Dry Dock
        {
            _galleon = galleon;
            _from = from;
            Enabled = galleon.IsOwner(from);
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_galleon?.Deleted == false && !_galleon.Contains(_from) && _galleon.IsOwner(_from))
            {
                _galleon.BeginDryDock(_from);
            }
        }
    }

    private class SecuritySettingsEntry : ContextMenuEntry
    {
        private readonly BaseGalleon _galleon;
        private readonly Mobile _from;

        public SecuritySettingsEntry(BaseGalleon galleon, Mobile from) : base(1149786, 5) // Security Settings
        {
            _galleon = galleon;
            _from = from;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            if (_galleon?.Deleted == false)
            {
                ShipSecurityGump.DisplayTo(_from, _galleon);
            }
        }
    }

    /// <summary>Аварийные заделки: 25 досок и 55 ткани за шесть минут хода с пробитым корпусом.</summary>
    private class EmergencyRepairEntry : ContextMenuEntry
    {
        private readonly BaseGalleon _galleon;
        private readonly Mobile _from;

        public EmergencyRepairEntry(BaseGalleon galleon, Mobile from) : base(1116589, 5) // Emergency Repairs
        {
            _galleon = galleon;
            _from = from;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            var galleon = _galleon;

            if (galleon?.Deleted != false)
            {
                return;
            }

            if (!galleon.Scuttled)
            {
                _from.SendLocalizedMessage(1116595); // Your ship is not in need of emergency repairs in order to sail.
                return;
            }

            if (galleon.IsUnderEmergencyRepairs())
            {
                _from.SendLocalizedMessage(1116592, $"{(int)galleon.GetEndEmergencyRepairs().TotalMinutes}");
                return;
            }

            // О нехватке материалов TryEmergencyRepair сообщает сам (cliloc 1116591).
            galleon.TryEmergencyRepair(_from);
        }
    }

    /// <summary>Постоянный ремонт: только у берега или причала, за доски и ткань.</summary>
    private class ShipRepairEntry : ContextMenuEntry
    {
        private readonly BaseGalleon _galleon;
        private readonly Mobile _from;

        public ShipRepairEntry(BaseGalleon galleon, Mobile from) : base(1116590, 5) // Permanent Repairs
        {
            _galleon = galleon;
            _from = from;
        }

        public override void OnClick(Mobile from, IEntity target)
        {
            var galleon = _galleon;

            if (galleon?.Deleted != false)
            {
                return;
            }

            if (!BaseGalleon.IsNearLandOrDocks(galleon))
            {
                _from.SendLocalizedMessage(1116594); // Your ship must be near shore or a sea market in order to effect permanent repairs.
            }
            else if (galleon.DamageTaken == DamageLevel.Pristine)
            {
                _from.SendLocalizedMessage(1116596); // Your ship is in pristine condition and does not need repairs.
            }
            else
            {
                galleon.TryRepairs(_from);
            }
        }
    }
}
