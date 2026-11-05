using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;

namespace Server.Mobiles;

/// <summary>
///     ServUO derives the New Haven escortables from BaseEscort (a MondainQuester quest giver), which does
///     not exist in ModernUO, and pairs each of them with a NewHaven*EscortQuest ML quest class. They are
///     ported here as inert, fully dressed town NPCs without the escort/quest machinery.
/// </summary>
[SerializationGenerator(0, false)]
public partial class NewHavenEscortable : BaseVendor
{
    private readonly List<SBInfo> _sbInfos = new();

    [Constructible]
    public NewHavenEscortable() : base(null)
    {
        AI = AIType.AI_Melee;
        FightMode = FightMode.Aggressor;
        RangePerception = 22;
        RangeFight = 1;

        SetSpeed(0.2, 1.0);
    }

    public override bool IsActiveVendor => false;
    public override bool IsInvulnerable => false;
    public override bool Commandable => false;
    public override bool InitialInnocent => true;

    protected override List<SBInfo> SBInfos => _sbInfos;

    public override void InitSBInfo()
    {
    }

    public override void InitBody()
    {
        SetStr(90, 100);
        SetDex(90, 100);
        SetInt(15, 25);

        Hue = Race.Human.RandomSkinHue();
        Female = Utility.RandomBool();
        Name = NameList.RandomName(Female ? "female" : "male");
        Race = Race.Human;

        Utility.AssignRandomHair(this);
        Utility.AssignRandomFacialHair(this);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenMerchant : NewHavenEscortable
{
    [Constructible]
    public NewHavenMerchant()
    {
        Title = "торговец";

        SetSkill(SkillName.ItemID, 55.0, 78.0);
        SetSkill(SkillName.ArmsLore, 55, 78);
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false;

    public override void InitOutfit()
    {
        if (Female)
        {
            AddItem(new PlainDress());
        }
        else
        {
            AddItem(new Shirt(GetRandomHue()));
        }

        var lowHue = GetRandomHue();

        AddItem(new ThighBoots());

        if (Female)
        {
            AddItem(new FancyDress(lowHue));
        }
        else
        {
            AddItem(new FancyShirt(lowHue));
        }

        AddItem(new LongPants(lowHue));

        if (!Female)
        {
            AddItem(new BodySash(lowHue));
        }

        PackGold(200, 250);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenMage : NewHavenEscortable
{
    [Constructible]
    public NewHavenMage()
    {
        Title = "маг";

        SetSkill(SkillName.EvalInt, 80.0, 100.0);
        SetSkill(SkillName.Inscribe, 80.0, 100.0);
        SetSkill(SkillName.Magery, 80.0, 100.0);
        SetSkill(SkillName.Meditation, 80.0, 100.0);
        SetSkill(SkillName.MagicResist, 80.0, 100.0);
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false;

    public override void InitOutfit()
    {
        AddItem(new Robe(GetRandomHue()));

        var lowHue = GetRandomHue();

        AddItem(new ShortPants(lowHue));

        if (Female)
        {
            AddItem(new ThighBoots(lowHue));
        }
        else
        {
            AddItem(new Boots(lowHue));
        }

        PackGold(200, 250);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenMessenger : NewHavenEscortable
{
    [Constructible]
    public NewHavenMessenger() => Title = "посыльный";

    public override bool ClickTitle => false;

    public override void InitOutfit()
    {
        if (Female)
        {
            AddItem(new PlainDress());
        }
        else
        {
            AddItem(new Shirt(GetRandomHue()));
        }

        var lowHue = GetRandomHue();

        AddItem(new ShortPants(lowHue));

        if (Female)
        {
            AddItem(new Boots(lowHue));
        }
        else
        {
            AddItem(new Shoes(lowHue));
        }

        // ServUO picks one of the ShortHair/TwoPigTails/ReceedingHair/KrisnaHair hair items here; those
        // item classes do not exist in ModernUO, so the random hair helper is used instead.
        Utility.AssignRandomHair(this);

        PackGold(200, 250);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenSeekerOfAdventure : NewHavenEscortable
{
    [Constructible]
    public NewHavenSeekerOfAdventure() => Title = "искатель приключений";

    public override bool ClickTitle => false;

    public override void InitOutfit()
    {
        if (Female)
        {
            AddItem(new FancyDress(GetRandomHue()));
        }
        else
        {
            AddItem(new FancyShirt(GetRandomHue()));
        }

        var lowHue = GetRandomHue();

        AddItem(new ShortPants(lowHue));

        if (Female)
        {
            AddItem(new ThighBoots(lowHue));
        }
        else
        {
            AddItem(new Boots(lowHue));
        }

        if (!Female)
        {
            AddItem(new BodySash(lowHue));
        }

        AddItem(new Cloak(GetRandomHue()));
        AddItem(new Longsword());

        PackGold(100, 150);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenNoble : NewHavenEscortable
{
    [Constructible]
    public NewHavenNoble()
    {
        Title = "аристократ";

        SetSkill(SkillName.Parry, 80.0, 100.0);
        SetSkill(SkillName.Swords, 80.0, 100.0);
        SetSkill(SkillName.Tactics, 80.0, 100.0);
    }

    public override bool CanTeach => true;
    public override bool ClickTitle => false;

    public override void InitOutfit()
    {
        if (Female)
        {
            AddItem(new FancyDress());
        }
        else
        {
            AddItem(new FancyShirt(GetRandomHue()));
        }

        var lowHue = GetRandomHue();

        AddItem(new ShortPants(lowHue));

        if (Female)
        {
            AddItem(new ThighBoots(lowHue));
        }
        else
        {
            AddItem(new Boots(lowHue));
        }

        if (!Female)
        {
            AddItem(new BodySash(lowHue));
        }

        AddItem(new Cloak(GetRandomHue()));

        if (!Female)
        {
            AddItem(new Longsword());
        }

        PackGold(200, 250);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenBrideGroom : NewHavenEscortable
{
    [Constructible]
    public NewHavenBrideGroom() => Title = Female ? "невеста" : "жених";

    public override bool ClickTitle => false;

    public override void InitOutfit()
    {
        if (Female)
        {
            AddItem(new FancyDress());
        }
        else
        {
            AddItem(new FancyShirt());
        }

        var lowHue = GetRandomHue();

        AddItem(new LongPants(lowHue));

        if (Female)
        {
            AddItem(new Shoes(lowHue));
        }
        else
        {
            AddItem(new Boots(lowHue));
        }

        if (Utility.RandomBool())
        {
            HairItemID = 0x203B;
        }
        else
        {
            HairItemID = 0x203C;
        }

        HairHue = Race.RandomHairHue();

        PackGold(200, 250);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenPeasant : NewHavenEscortable
{
    [Constructible]
    public NewHavenPeasant() => Title = "крестьянин";

    public override bool ClickTitle => false;

    public override void InitOutfit()
    {
        if (Female)
        {
            AddItem(new PlainDress());
        }
        else
        {
            AddItem(new Shirt(GetRandomHue()));
        }

        var lowHue = GetRandomHue();

        AddItem(new ShortPants(lowHue));

        if (Female)
        {
            AddItem(new Boots(lowHue));
        }
        else
        {
            AddItem(new Shoes(lowHue));
        }

        Utility.AssignRandomHair(this);

        PackGold(200, 250);
    }
}

[SerializationGenerator(0, false)]
public partial class NewHavenHealer : NewHavenEscortable
{
    private static readonly TimeSpan ResurrectDelay = TimeSpan.FromSeconds(2.0);

    private DateTime _nextResurrect;

    [Constructible]
    public NewHavenHealer()
    {
        Title = "странствующий целитель";

        AI = AIType.AI_Mage;
        SetSpeed(0.2, 0.8);
        RangePerception = DefaultRangePerception;
        FightMode = FightMode.Aggressor;

        SpeechHue = 0;

        SetStr(304, 400);
        SetDex(102, 150);
        SetInt(204, 300);

        SetDamage(10, 23);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 40, 50);
        SetResistance(ResistanceType.Fire, 40, 50);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 40, 50);

        SetSkill(SkillName.Anatomy, 75.0, 97.5);
        SetSkill(SkillName.EvalInt, 82.0, 100.0);
        SetSkill(SkillName.Healing, 75.0, 97.5);
        SetSkill(SkillName.Magery, 82.0, 100.0);
        SetSkill(SkillName.MagicResist, 82.0, 100.0);
        SetSkill(SkillName.Tactics, 82.0, 100.0);
        SetSkill(SkillName.Camping, 80.0, 100.0);
        SetSkill(SkillName.Forensics, 80.0, 100.0);
        SetSkill(SkillName.SpiritSpeak, 80.0, 100.0);

        Fame = 1000;
        Karma = 10000;

        PackItem(new Bandage(Utility.RandomMinMax(5, 10)));
        PackItem(new HealPotion());
        PackItem(new CurePotion());
    }

    public override bool ClickTitle => false;
    public override bool CanTeach => true;

    public virtual bool HealsYoungPlayers => true;

    public override bool CheckTeach(SkillName skill, Mobile from)
    {
        if (!base.CheckTeach(skill, from))
        {
            return false;
        }

        return skill is SkillName.Anatomy or SkillName.Camping or SkillName.Forensics or SkillName.Healing
            or SkillName.SpiritSpeak;
    }

    public override void InitOutfit()
    {
        AddItem(new Sandals(GetShoeHue()));
        AddItem(new Robe(Utility.RandomYellowHue()));
        AddItem(new GnarledStaff());
    }

    public virtual bool CheckResurrect(Mobile m)
    {
        if (m.Criminal)
        {
            Say(501222); // Thou art a criminal.  I shall not resurrect thee.
            return false;
        }

        if (m.Murderer)
        {
            Say(501223); // Thou'rt not a decent and good person. I shall not resurrect thee.
            return false;
        }

        return true;
    }

    public virtual void OfferResurrection(Mobile m)
    {
        Direction = GetDirectionTo(m);
        // Thou hast strayed from the path of virtue, but thou still deservest a second chance.
        Say(501224);

        m.PlaySound(0x214);
        m.FixedEffect(0x376A, 10, 16);

        m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
    }

    public virtual void OfferHeal(PlayerMobile m)
    {
        Direction = GetDirectionTo(m);

        if (m.CheckYoungHealTime())
        {
            Say(501229); // You look like you need some healing my child.

            m.PlaySound(0x1F2);
            m.FixedEffect(0x376A, 9, 32);

            m.Hits = m.HitsMax;
        }
        else
        {
            Say(501228); // I can do no more for you at this time.
        }
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
        if (!m.Frozen && Core.Now >= _nextResurrect && InRange(m, 4) && !InRange(oldLocation, 4) && InLOS(m))
        {
            if (!m.Alive)
            {
                _nextResurrect = Core.Now + ResurrectDelay;

                if (m.Map == null || !m.Map.CanFit(m.Location, 16, false, false))
                {
                    m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                }
                else if (CheckResurrect(m))
                {
                    OfferResurrection(m);
                }
            }
            else if (HealsYoungPlayers && m.Hits < m.HitsMax && m is PlayerMobile pm && pm.Young)
            {
                OfferHeal(pm);
            }
        }
    }
}
