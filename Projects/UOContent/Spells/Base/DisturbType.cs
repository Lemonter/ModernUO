namespace Server.Spells
{
    public enum DisturbType
    {
        Unspecified,
        EquipRequest,
        UseRequest,
        Hurt,
        Kill,
        NewCast,

        // Mahaon: paralyze/stun is now the only "you got interrupted" cause left besides a
        // called-shot hit on the caster's chosen casting channel (CastingChannelSystem) —
        // plain melee/enemy-spell/poison damage no longer disturbs at all (see
        // Mobile.Damage/Spell.OnCasterHurt). Kept as its own DisturbType so OnDisturb can
        // give it a distinct message instead of the generic "concentration disturbed" one.
        Paralyzed
    }
}
