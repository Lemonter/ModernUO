using ModernUO.Serialization;
using System;

namespace Server.Items;

/// <summary>
///     Ported from ServUO (Scripts/Items/Artifacts/Equipment/Weapons/MuseumWeapons.cs).
/// </summary>
[SerializationGenerator(0, false)]
public partial class BlackthornsKryss : Kryss
{
    [Constructible]
    public BlackthornsKryss()
        : base()
    {
        Hue = 0x5E5;
        Slayer = BaseRunicTool.GetRandomSlayer();
        Attributes.WeaponSpeed = 25;
        Attributes.WeaponDamage = 50;
        WeaponAttributes.UseBestSkill = 1;
        WeaponAttributes.HitLeechHits = 22;
    }

    public override int LabelNumber => 1073260; // Blackthorn's Kryss - Museum of Vesper Replica

    public override int InitMinHits => 80;

    public override int InitMaxHits => 80;
}

[SerializationGenerator(0, false)]
public partial class SwordOfJustice : VikingSword
{
    [Constructible]
    public SwordOfJustice()
        : base()
    {
        Hue = 0x47E;
        Slayer = BaseRunicTool.GetRandomSlayer();
        Attributes.SpellChanneling = 1;
        Attributes.CastSpeed = -1;
        Attributes.WeaponDamage = 50;
        Attributes.Luck = 100;
        WeaponAttributes.UseBestSkill = 1;
        WeaponAttributes.HitLowerAttack = 60;
    }

    public override int LabelNumber => 1073261; // Sword of Justice - Museum of Vesper Replica

    public override int InitMinHits => 80;

    public override int InitMaxHits => 80;
}

[SerializationGenerator(0, false)]
public partial class GeoffreysAxe : ExecutionersAxe
{
    [Constructible]
    public GeoffreysAxe()
        : base()
    {
        Hue = 0x21;
        Slayer = BaseRunicTool.GetRandomSlayer();
        Attributes.BonusStr = 10;
        Attributes.AttackChance = 15;
        Attributes.WeaponDamage = 40;
        Attributes.Luck = 150;
        WeaponAttributes.ResistFireBonus = 10;
        WeaponAttributes.UseBestSkill = 1;
        WeaponAttributes.HitLowerAttack = 60;
    }

    public override int LabelNumber => 1073263; // Geoffrey's Axe - Museum of Vesper Replica

    public override int InitMinHits => 80;

    public override int InitMaxHits => 80;
}
