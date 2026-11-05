using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;

namespace Server.Items;

/// <summary>
///     Портировано из ServUO. Отброшено: SpellHelper.RestrictRedTravel (в репозитории нет).
/// </summary>
[SerializationGenerator(0, false)]
public partial class SerpentsJawbone : Item, ISecurable
{
    public static Dictionary<int, Point3D> Locations { get; } = new()
    {
        [1157135] = new Point3D(1156, 1143, -24), // The Village of Lakeshire
        [1157619] = new Point3D(644, 854, -56),   // The Rat Fort
        [1157620] = new Point3D(1363, 1075, -13), // Reg Volom
        [1016410] = new Point3D(1572, 1046, -8),  // Twin Oaks Tavern
        [1157621] = new Point3D(984, 622, -80),   // The Oasis
        [1078308] = new Point3D(1746, 1221, -1),  // Blood Dungeon
        [1111764] = new Point3D(912, 1362, -21),  // Cyclops Dungeon
        [1111765] = new Point3D(824, 774, -80),   // Exodus Dungeon
        [1111766] = new Point3D(349, 1434, 16),   // The Kirin Passage
        [1157622] = new Point3D(971, 303, 54),    // Pass of Karnaugh
        [1157623] = new Point3D(1033, 1154, -24), // The Rat Cave
        [1078315] = new Point3D(541, 466, -72),   // Terort Skitas
        [1111825] = new Point3D(1450, 1477, -29), // Twisted Weald
        [1113002] = new Point3D(642, 1307, -55),  // Wisp Dungeon
        [1157624] = new Point3D(753, 497, -62),   // Gwenno's Memorial
        [1157625] = new Point3D(1504, 628, -14),  // Desert Gypsy Camp
        [1113000] = new Point3D(1785, 573, 71)    // Rock Dungeon
    };

    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [Constructible]
    public SerpentsJawbone() : base(0x9F74)
    {
    }

    public override int LabelNumber => 1157654; // Serpent's Jawbone

    public override bool ForceShowProperties => true;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        SetSecureLevelEntry.AddTo(from, this, ref list);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if ((IsLockedDown || IsSecure) && from.InRange(GetWorldLocation(), 2))
        {
            if (from is PlayerMobile pm)
            {
                pm.SendGump(new InternalGump(pm, this));
            }
        }
        else if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500295); // You are too far away to do that.
        }
        else
        {
            from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.
        }
    }

    private class InternalGump : DynamicGump
    {
        private readonly Item _jawbone;
        private readonly PlayerMobile _user;

        public override bool Singleton => true;

        public InternalGump(PlayerMobile pm, Item jawbone) : base(100, 100)
        {
            _jawbone = jawbone;
            _user = pm;
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 370, 428, 0x1400);
            builder.AddHtmlLocalized(10, 10, 350, 18, 1114513, "#1156704", 0x56BA); // <DIV ALIGN=CENTER>~1_TOKEN~</DIV>

            var index = 0;

            foreach (var (label, _) in Locations)
            {
                builder.AddButton(10, 41 + index * 20, 1209, 1210, label);
                builder.AddHtmlLocalized(50, 41 + index * 20, 150, 20, label, 0x7FFF);
                index++;
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID <= 0 || !Locations.TryGetValue(info.ButtonID, out var p))
            {
                return;
            }

            if (!CheckTravel(p))
            {
                return;
            }

            BaseCreature.TeleportPets(_user, p, Map.Ilshenar);
            _user.Combatant = null;
            _user.Warmode = false;
            _user.Hidden = true;

            _user.MoveToWorld(p, Map.Ilshenar);

            Effects.PlaySound(p, Map.Ilshenar, 0x1FE);
        }

        private bool CheckTravel(Point3D p)
        {
            if (!_user.InRange(_jawbone.GetWorldLocation(), 2) || _user.Map != _jawbone.Map)
            {
                _user.SendLocalizedMessage(500295); // You are too far away to do that.
            }
            else if (Factions.Sigil.ExistsOn(_user))
            {
                _user.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            }
            else if (_user.Criminal)
            {
                _user.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            }
            else if (SpellHelper.CheckCombat(_user))
            {
                _user.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            }
            else if (_user.Spell != null)
            {
                _user.SendLocalizedMessage(1049616); // You are too busy to do that at the moment.
            }
            else if (_user.Map == Map.Ilshenar && _user.InRange(p, 1))
            {
                _user.SendLocalizedMessage(1019003); // You are already there.
            }
            else
            {
                return true;
            }

            return false;
        }
    }
}
