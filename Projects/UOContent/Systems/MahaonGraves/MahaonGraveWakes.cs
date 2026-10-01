using Server.Items;
using Server.Mobiles;

namespace Server.Systems.MahaonGraves;

/// <summary>
///     Кто вылезает из потревоженной могилы.
///
///     Смысл не в опасности как таковой, а в том, чтобы у копки была цена. Иначе кладбище
///     превращается в бесплатную грядку с костями: пришёл, выкопал всё, ушёл. С риском
///     появляется выбор — идти туда одному или с кем-то, и стоит ли вообще трогать могилу,
///     если ты потрёпан после подземелья.
///
///     Кто именно поднимется, зависит от навыка копающего: новичок разбудит скелета,
///     мастер — костяного рыцаря. Это не столько баланс, сколько уместность: могилы
///     побогаче копают там, где и лежит кто-то поприличнее.
/// </summary>
public static class MahaonGraveWakes
{
    public static void Wake(Mobile digger)
    {
        if (digger?.Map == null || digger.Map == Map.Internal)
        {
            return;
        }

        var skill = digger.Skills[SkillName.Mining].Value;

        BaseCreature risen = Utility.Random(100) switch
        {
            < 45 when skill < 60 => new Skeleton(),
            < 45                 => new Zombie(),
            < 75 when skill < 80 => new Zombie(),
            < 75                 => new Ghoul(),
            < 92 when skill < 95 => new Ghoul(),
            < 92                 => new Mummy(),
            _ when skill < 95    => new Mummy(),
            _                    => new BoneKnight()
        };

        var spot = FindSpot(digger);

        risen.MoveToWorld(spot, digger.Map);
        risen.Combatant = digger;

        digger.SendMessage(0x22, "Земля вздрагивает — из могилы лезет её хозяин!");
        Effects.PlaySound(spot, digger.Map, 0x229);
        Effects.SendLocationParticles(
            EffectItem.Create(spot, digger.Map, EffectItem.DefaultDuration), 0x3789, 10, 20, 5052
        );
    }

    /// <summary>Место рядом с копающим, куда можно встать. Не нашлось — поднимаем прямо
    /// под ним: это неприятно, но честнее, чем не поднять вовсе.</summary>
    private static Point3D FindSpot(Mobile digger)
    {
        var map = digger.Map;

        for (var attempt = 0; attempt < 8; attempt++)
        {
            var x = digger.X + Utility.RandomMinMax(-2, 2);
            var y = digger.Y + Utility.RandomMinMax(-2, 2);
            var z = map.GetAverageZ(x, y);
            var candidate = new Point3D(x, y, z);

            if (map.CanSpawnMobile(candidate))
            {
                return candidate;
            }
        }

        return digger.Location;
    }
}
