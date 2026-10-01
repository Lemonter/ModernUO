using Server.Factions;
using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.ConPVP
{
    public class SafeZone : GuardedRegion
    {
        public static readonly int SafeZonePriority = HouseRegion.HousePriority + 1;

        /*public override bool AllowReds => true;*/

        public SafeZone(Rectangle2D area, Point3D goloc, Map map, bool isGuarded) : base(null, map, SafeZonePriority, area)
        {
            GoLocation = goloc;

            GuardsDisabled = !isGuarded;

            Register();
        }

        public override bool AllowHousing(Mobile from, Point3D p) =>
            from.AccessLevel >= AccessLevel.GameMaster && base.AllowHousing(from, p);

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (m.Player && Sigil.ExistsOn(m))
            {
                m.SendMessage(0x22, "У тебя в руках сигил — сюда нельзя.");
                return false;
            }

            var pm = m as PlayerMobile ??
                     (m is BaseCreature bc && bc.Summoned ? bc.SummonMaster as PlayerMobile : null);

            if (pm?.DuelContext?.StartedBeginCountdown == true)
            {
                return true;
            }

            if (DuelContext.CheckCombat(m))
            {
                m.SendMessage(0x22, "Ты недавно был в бою — сюда пока нельзя.");
                return false;
            }

            return base.OnMoveInto(m, d, newLocation, oldLocation);
        }

        public override void OnEnter(Mobile m)
        {
            m.SendMessage("Ты вошёл в дуэльную безопасную зону. Здесь разрешены только дуэли.");
        }

        public override void OnExit(Mobile m)
        {
            m.SendMessage("Ты покинул дуэльную безопасную зону. Бой снова без ограничений.");
        }

        public override bool CanUseStuckMenu(Mobile m) => false;
    }
}
