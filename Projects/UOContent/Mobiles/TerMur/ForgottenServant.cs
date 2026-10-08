using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.Mobiles;

/// <summary>Ported from ServUO (Scripts/Mobiles/Custom/ForgottenServant.cs). SetWearable
/// (both the 3-arg item/hue/dropChance and 2-arg item/dropChance forms) doesn't exist here —
/// converted to AddItem(new X { Hue = ... }).</summary>
[SerializationGenerator(0, false)]
public partial class ForgottenServant : BaseCreature
{
    private static readonly Type[] _weaponsList =
    {
        typeof(Longsword), typeof(Cutlass), typeof(Broadsword), typeof(Axe), typeof(Club), typeof(Dagger), typeof(Spear)
    };

    [Constructible]
    public ForgottenServant() : base(AIType.AI_Melee, FightMode.Closest, 10, 1)
    {
        SpeechHue = Utility.RandomDyedHue();
        Title = "Forgotten Servant";
        Hue = 768;

        Female = Utility.RandomBool();

        if (Female)
        {
            Body = 0x191;
            Name = NameList.RandomName("female");
            AddItem(new Skirt { Hue = Utility.RandomNeutralHue() });
        }
        else
        {
            Body = 0x190;
            Name = NameList.RandomName("male");
            AddItem(new ShortPants { Hue = Utility.RandomNeutralHue() });
        }

        SetStr(147, 215);
        SetDex(91, 115);
        SetInt(61, 85);

        SetHits(95, 123);

        SetDamage(4, 14);

        SetDamageType(ResistanceType.Physical, 100);

        SetResistance(ResistanceType.Physical, 25, 35);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);

        SetSkill(SkillName.MagicResist, 70.1, 85.0);
        SetSkill(SkillName.Swords, 60.1, 85.0);
        SetSkill(SkillName.Tactics, 75.1, 90.0);
        SetSkill(SkillName.Wrestling, 60.1, 85.0);

        Fame = 2500;
        Karma = -2500;

        AddItem(new Boots { Hue = Utility.RandomNeutralHue() });
        AddItem(new FancyShirt());
        AddItem(new Bandana());
        AddItem((Item)Activator.CreateInstance(Utility.RandomList(_weaponsList)));

        Utility.AssignRandomHair(this);
    }

    public override bool ClickTitle => false;
    public override bool AlwaysMurderer => true;

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
    }
}
