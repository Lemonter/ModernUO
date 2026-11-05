using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Spells;

namespace Server.Mobiles;

/// <summary>
///     Портировано из ServUO (Covetous Void Spawn/Creatures/Cora.cs) — Кора, хозяйка Бездонного бассейна:
///     босс Коветуса с 35 000 хитов, который раз в 15–120 секунд «раскрывает трещины» и вытягивает ману у
///     всех, кто на них наступит.
///
///     Отличия от оригинала: в этом репозитории нет <c>IElementalCreature</c>, телепортационных свойств
///     (<c>TeleportsTo</c> и компания), типа анимации <c>AnimationType</c> и режима осады (<c>SiegeShard</c>) —
///     они отброшены. Способности восстановлены: <c>SetSpecialAbility(DragonBreath)</c> — это
///     <see cref="MonsterAbilities.FireBreath" />, а <c>SetAreaEffect(AreaEffect.AuraDamage)</c> вместе с
///     <c>IAuraCreature</c> — встроенная аура <see cref="BaseCreature" /> (радиус 3, урона нет — только
///     вытягивание маны в <see cref="AuraEffect" />). Трещины не строятся по
///     <c>MovementPath</c>, а ставятся в четырёх направлениях на расстоянии до 10 клеток: поведение то же,
///     но без расчёта пути. Урон от маны, сообщения и дроп артефактов сохранены.
/// </summary>
[SerializationGenerator(0, false)]
public partial class CoraTheSorceress : BaseCreature
{
    private DateTime _nextManaDrain;

    [Constructible]
    public CoraTheSorceress() : base(AIType.AI_Mage, FightMode.Closest, 10, 1)
    {
        Body = 0x191;
        Name = "Кора";
        Title = "колдунья";

        HairItemID = 0x2045;
        HairHue = 452;

        SetStr(909, 949);
        SetDex(125);
        SetInt(903, 947);

        SetHits(35000);

        SetDamage(17, 25);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Energy, 50);

        SetResistance(ResistanceType.Physical, 52, 67);
        SetResistance(ResistanceType.Fire, 51, 68);
        SetResistance(ResistanceType.Cold, 51, 69);
        SetResistance(ResistanceType.Poison, 51, 70);
        SetResistance(ResistanceType.Energy, 50, 68);

        SetSkill(SkillName.Wrestling, 100.1, 119.7);
        SetSkill(SkillName.Tactics, 102.3, 118.5);
        SetSkill(SkillName.MagicResist, 101.2, 119.6);
        SetSkill(SkillName.Anatomy, 100.1, 117.5);
        SetSkill(SkillName.Magery, 100.1, 117.5);
        SetSkill(SkillName.EvalInt, 100.1, 117.5);

        Fame = 32000;
        Karma = -32000;

        AddAndEquip(new WildStaff(), 1971);
        AddAndEquip(new ThighBoots(), 1910);
        AddAndEquip(new ChainLegs(), 1936);
        AddAndEquip(new LeatherGloves(), 1910);
        AddAndEquip(new LeatherBustierArms(), 1947);

        _nextManaDrain = Core.Now + TimeSpan.FromSeconds(15);
    }

    public override bool AlwaysMurderer => true;
    public override bool ClickTitle => false;
    public override bool ShowFameTitle => false;

    private static readonly MonsterAbility[] _abilities = [MonsterAbilities.FireBreath];

    public override MonsterAbility[] GetMonsterAbilities() => _abilities;

    public override bool HasAura => true;
    public override int AuraBaseDamage => 0;
    public override int AuraRange => 3;
    public override int AuraFireDamage => 0;

    public override void AuraEffect(Mobile m)
    {
        if (m.Mana <= 0)
        {
            return;
        }

        var mana = Utility.Random(1, m.Mana);

        m.Mana -= mana;
        // Cora drains ~1_VAL~ points of your mana!
        m.SendLocalizedMessage(1153114, mana.ToString());
    }

    public override int GetDeathSound() => 0x316;

    private void AddAndEquip(Item item, int hue = 0)
    {
        item.Movable = false;
        item.Hue = hue;
        AddItem(item);
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 3);
        AddLoot(LootPack.SuperBoss, 3);
    }

    public override void OnThink()
    {
        base.OnThink();

        if (_nextManaDrain <= Core.Now)
        {
            DoManaDrain();
        }
    }

    /// <summary>Раскрывает «трещины» в четырёх направлениях: каждый осколок вытягивает ману у того, кто на него встал.</summary>
    public void DoManaDrain()
    {
        DoCrack(Direction.North);
        DoCrack(Direction.West);
        DoCrack(Direction.South);
        DoCrack(Direction.East);

        _nextManaDrain = Core.Now + TimeSpan.FromSeconds(Utility.RandomMinMax(15, 120));
    }

    private void DoCrack(Direction direction)
    {
        var map = Map;

        if (map == null)
        {
            return;
        }

        const int range = 10;
        const int offset = 8;

        var x = X;
        var y = Y;

        switch (direction)
        {
            case Direction.North:
                x += Utility.RandomMinMax(-offset, offset);
                y -= range;
                break;
            case Direction.West:
                x -= range;
                y += Utility.RandomMinMax(-offset, offset);
                break;
            case Direction.South:
                x += Utility.RandomMinMax(-offset, offset);
                y += range;
                break;
            case Direction.East:
                x += range;
                y += Utility.RandomMinMax(-offset, offset);
                break;
        }

        var point = new Point3D(x, y, map.GetAverageZ(x, y));

        if (!SpellHelper.AdjustField(ref point, map, 12, false))
        {
            return;
        }

        var crack = new ManaDrainCrack(Utility.RandomList(6913, 6915, 6917, 6919), this);
        crack.MoveToWorld(point, map);
    }

    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (0.30 > Utility.RandomDouble())
        {
            var player = DemonKnight.FindRandomPlayer(this);

            if (player != null)
            {
                var artifact = Server.Engines.VoidPool.VoidPoolRewards.DropRandomArtifact();

                if (artifact != null)
                {
                    var pack = player.Backpack;

                    if (pack == null || !pack.TryDropItem(player, artifact, false))
                    {
                        player.BankBox.DropItem(artifact);
                    }

                    // За доблесть в бою с павшим чудовищем вам дарован особый артефакт.
                    player.SendLocalizedMessage(1062317);
                }
            }
        }
    }
}

/// <summary>Осколок трещины Коры: гасит ману наступившему, через полсекунды гаснет и удаляется.</summary>
[SerializationGenerator(0, false)]
public partial class ManaDrainCrack : Item
{
    [SerializableField(0)]
    private Mobile _owner;

    private TimerExecutionToken _fadeToken;
    private Item _static;

    public ManaDrainCrack(int itemID, Mobile owner) : base(itemID)
    {
        _owner = owner;

        Movable = false;
        Hue = 1152;

        Timer.StartTimer(TimeSpan.FromSeconds(5), Fade, out _fadeToken);
    }

    public override bool OnMoveOver(Mobile m)
    {
        if ((m is PlayerMobile || m is BaseCreature { IsMonster: false }) && m.CanBeHarmful(_owner, false))
        {
            m.FixedParticles(0x3779, 10, 25, 5002, EffectLayer.Head);
            m.Mana = 0;
        }

        return true;
    }

    public override void OnLocationChange(Point3D oldLocation)
    {
        base.OnLocationChange(oldLocation);

        _static?.Delete();

        _static = new Static(ItemID + 1);
        _static.MoveToWorld(Location, Map);

        foreach (var m in Map.GetMobilesInRange(Location, 0))
        {
            OnMoveOver(m);
        }
    }

    private void Fade()
    {
        Hue = 1153;

        if (_static != null)
        {
            _static.Hue = 1153;
        }

        Timer.DelayCall(TimeSpan.FromSeconds(0.5), Delete);
    }

    public override void OnAfterDelete()
    {
        base.OnAfterDelete();

        _fadeToken.Cancel();
        _static?.Delete();
        _static = null;
    }
}
