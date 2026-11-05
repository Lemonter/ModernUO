namespace Server.Mobiles;

/// <summary>Mirror of ServUO's ElementType (Scripts/Mobiles/MobileInterfaces.cs). The
/// IElementalCreature interface it was declared next to is not ported — the drakes expose the
/// element as a plain property instead.</summary>
public enum ElementType
{
    Physical,
    Fire,
    Cold,
    Poison,
    Energy,
    Chaos,
    Direct
}
