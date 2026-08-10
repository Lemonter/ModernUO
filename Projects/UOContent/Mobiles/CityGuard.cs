using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;
using Server.Systems.MahaonCities;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class CityGuard : BaseCreature
{
    [SerializableField(0)]
    private string _city;

    [SerializableField(1)]
    private Guild _controllingGuild;

    // Patrol bookkeeping — deliberately not serialized, resets fine on world load.
    private Point3D _lastLocation;
    private int _stepsSinceHome;
    private int _stepThreshold = Utility.RandomMinMax(20, 30);

    private const int GuardScanRange = 15;

    public CityGuard(string city, Guild controllingGuild) : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        _city = city;
        _controllingGuild = controllingGuild;

        Name = $"{controllingGuild.Name} стражник";
        Body = 0x190;
        Hue = Race.Human.RandomSkinHue();

        SetStr(100, 120);
        SetDex(80, 100);
        SetInt(50, 60);
        SetHits(80, 100);

        SetSkill(SkillName.Swords, 80.0, 100.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 60.0, 80.0);

        AddItem(new Longsword());
        AddItem(new PlateChest());
        AddItem(new PlateArms());
        AddItem(new PlateLegs());
        AddItem(new PlateGorget());
    }

    public override bool AlwaysMurderer => false;

    public override bool IsEnemy(Mobile m) =>
        (_controllingGuild != null && CityControlSystem.IsHostileToCity(_city, m)) ||
        IsSuppressablePK(m) ||
        base.IsEnemy(m);

    private static bool IsSuppressablePK(Mobile m) => m is BotMobile { IsPk: true };

    public override void OnThink()
    {
        base.OnThink();

        // Mostly just stand there. Only bother with the leash-and-hunt logic below on an
        // occasional think tick, not every single one — cheap and still looks natural.
        if (Utility.RandomDouble() > 0.15)
        {
            return;
        }

        // Actively hunt down PK bots causing trouble in the city, rather than waiting to
        // be attacked first (FightMode.Aggressor alone wouldn't trigger on sight).
        if ((Combatant?.Deleted != false || !Combatant.Alive) && Map != null)
        {
            foreach (var mobile in Map.GetMobilesInRange<Mobile>(Location, GuardScanRange))
            {
                if (mobile.Alive && !mobile.Deleted && IsSuppressablePK(mobile))
                {
                    Combatant = mobile;
                    Warmode = true;
                    break;
                }
            }
        }

        // Patrol leash: only matters while not in combat — mostly standing still, with an
        // occasional step already coming from the base wander AI. After enough steps away
        // from the post, walk back and reset the counter.
        if (Combatant?.Deleted == false && Combatant.Alive)
        {
            return;
        }

        if (Location != _lastLocation)
        {
            _stepsSinceHome++;
            _lastLocation = Location;
        }

        if (_stepsSinceHome >= _stepThreshold && Home != Point3D.Zero)
        {
            MoveToWorld(Home, Map);
            _stepsSinceHome = 0;
            _stepThreshold = Utility.RandomMinMax(20, 30);
        }
    }
}
