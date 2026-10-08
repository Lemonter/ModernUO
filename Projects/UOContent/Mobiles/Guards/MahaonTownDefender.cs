using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Systems.MahaonRaids;

namespace Server.Mobiles
{
    /// <summary>
    ///     A normal-combat town guard for raid events — unlike WarriorGuard/BaseGuard
    ///     (the vanilla anti-PK "summon and instant-kill" mechanic, not suited for an
    ///     extended fight), this one just fights like any other creature, except damage
    ///     against/from raid mobs is capped to 1-2 (see MahaonRaidCombat) so a raid can run
    ///     as a long, guard-points-farming spectacle instead of guards one-shotting
    ///     everything or getting steamrolled by "hundreds of orcs".
    /// </summary>
    [SerializationGenerator(0, false)]
    public partial class MahaonTownDefender : BaseCreature, IMahaonTownDefender
    {
        [Constructible]
        public MahaonTownDefender() : base(AIType.AI_Melee)
        {
            Body = 0x190;
            Name = "защитник города";
            Hue = Race.Human.RandomSkinHue();

            SetStr(100, 120);
            SetDex(80, 100);
            SetInt(40, 60);

            SetHits(80, 100);

            SetDamage(8, 14);

            SetSkill(SkillName.Anatomy, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 80.0, 100.0);
            SetSkill(SkillName.Swords, 80.0, 100.0);
            SetSkill(SkillName.MagicResist, 80.0, 100.0);

            Fame = 0;
            Karma = 0;

            VirtualArmor = 40;

            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateLegs());
            AddItem(new PlateHelm());

            var weapon = new Halberd
            {
                Movable = false
            };

            AddItem(weapon);
        }

        public override bool AlwaysMurderer => false;
        public override bool ClickTitle => false;
        public override bool ShowFameTitle => false;

        // Раньше защитник дрался только реактивно (обычный AI_Melee — атакует, только
        // если атаковали его самого). Теперь, раз ванильная стража отключена
        // (DisableVanillaGuards), это должно стать её честной заменой — реально
        // проактивно атаковать ПК и красных существ, кто бы это ни был.
        //
        // AcquireOnApproachDelay = Zero включает уже существующий в BaseCreature.OnMovement
        // механизм "заметил кого-то рядом — напал", не пишем свой OnMovement с нуля.
        // Но IsEnemy по умолчанию (см. BaseCreature.IsEnemy) считает врагом ЛЮБОГО
        // игрока, кроме особых исключений — это полностью переопределяем, а не
        // дополняем, иначе страж бросался бы вообще на всех подряд.
        public override TimeSpan AcquireOnApproachDelay => TimeSpan.Zero;
        public override int AcquireOnApproachRange => 10;

        public override bool IsEnemy(Mobile m) =>
            m.Alive && m.Murderer && (m as BaseCreature)?.IsInvulnerable != true;

        // Real fighting stats above still matter for who "wins" the visual duel/animations
        // — this only caps the actual Hits damage exchanged specifically against raid
        // mobs (and anything they've summoned), so guards can't one-shot a whole raid nor
        // get instantly overwhelmed by sheer numbers.
        public override void Damage(int amount, Mobile from = null, bool informMount = true, bool ignoreEvilOmen = false)
        {
            base.Damage(MahaonRaidCombat.CapIfSpectacleFight(from, this, amount), from, informMount, ignoreEvilOmen);
        }
    }
}
