using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonRaids;

namespace Server.Mobiles;

/// <summary>Магический вариант MahaonTownDefender — тот же принцип обнаружения цели
/// (см. комментарии там), но AI_Mage вместо AI_Melee: реальный навык Magery, обычная
/// движковая MageAI сама решает, какие заклинания кастовать по назначенному Combatant —
/// ничего своего для самого каста не пишем, это уже встроенный механизм.</summary>
[SerializationGenerator(0, false)]
public partial class MahaonTownDefenderMage : BaseCreature, IMahaonTownDefender
{
    [Constructible]
    public MahaonTownDefenderMage() : base(AIType.AI_Mage)
    {
        Body = 0x190;
        Name = "страж-чародей";
        Hue = Race.Human.RandomSkinHue();

        SetStr(60, 80);
        SetDex(60, 80);
        SetInt(100, 130);

        SetHits(60, 80);
        SetMana(100, 120);

        SetDamage(4, 8);

        SetSkill(SkillName.Magery, 80.0, 100.0);
        SetSkill(SkillName.EvalInt, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 80.0, 100.0);
        SetSkill(SkillName.Meditation, 60.0, 80.0);
        SetSkill(SkillName.Tactics, 50.0, 70.0);
        SetSkill(SkillName.Wrestling, 50.0, 70.0);

        Fame = 0;
        Karma = 0;

        VirtualArmor = 20;

        AddItem(new Robe { Hue = 0x455, Movable = false });
        AddItem(new WizardsHat { Hue = 0x455, Movable = false });
        AddItem(new GnarledStaff { Movable = false });
    }

    public override bool AlwaysMurderer => false;
    public override bool ClickTitle => false;
    public override bool ShowFameTitle => false;

    // Та же логика обнаружения, что у обычного MahaonTownDefender — строго ПК и красные
    // существа, не любой игрок подряд (см. подробный комментарий в
    // MahaonTownDefender.cs про то, почему стандартный IsEnemy тут не годится как есть).
    public override bool AcquireOnApproach => true;
    public override int AcquireOnApproachRange => 10;

    public override bool IsEnemy(Mobile m) =>
        m.Alive && m.Murderer && (m as BaseCreature)?.IsInvulnerable != true;

    // Та же логика "спектакля" боя с рейд-мобами, что и у обычного защитника — иначе
    // маг либо мгновенно убивал бы рейдовых мобов заклинаниями, либо не наносил бы им
    // урона вообще из-за их же защит.
    public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
    {
        base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
    }
}
